using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace DailyPlan.Services;

// ===== 当前媒体信息快照（不可变，供渲染线程读取） =====
internal sealed class MediaInfo
{
    public bool HasSession;      // 是否有活动媒体会话
    public string Title = "";    // 歌名
    public string Artist = "";   // 歌手
    public bool IsPlaying;       // 是否正在播放
    public TimeSpan Duration;    // 总时长（可能为 0，表示未知）
}

// ===== SMTC 音乐服务：读取系统当前媒体会话 + 发送控制命令 =====
internal sealed class MusicService
{
    GlobalSystemMediaTransportControlsSessionManager? _manager;
    GlobalSystemMediaTransportControlsSession? _session;

    // 位置估算：SMTC 事件不保证高频触发，用本地时钟推算当前进度
    TimeSpan _basePosition;
    DateTime _baseAt;
    bool _isPlaying;
    TimeSpan _duration;

    public MediaInfo Current { get; private set; } = new();

    // 任何媒体变化（切换会话/换歌/播放暂停/时间线变化）都会触发，回调在后台线程
    public event Action? Changed;

    public async Task InitAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            await RefreshSessionAsync();
        }
        catch
        {
            // SMTC 不可用（老系统）时静默降级，不显示音乐区
            _manager = null;
        }
    }

    // 估算当前播放位置（暂停时固定，播放时随时间前进）
    public TimeSpan EstimatedPosition
    {
        get
        {
            if (!_isPlaying) return _basePosition;
            var est = _basePosition + (DateTime.UtcNow - _baseAt);
            if (_duration > TimeSpan.Zero && est > _duration) est = _duration;
            return est;
        }
    }

    void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        => _ = RefreshSessionAsync();

    async Task RefreshSessionAsync()
    {
        if (_manager == null) return;
        Unsubscribe();
        _session = _manager.GetCurrentSession();
        if (_session != null)
        {
            _session.MediaPropertiesChanged += OnSessionChanged;
            _session.PlaybackInfoChanged += OnSessionChanged;
            _session.TimelinePropertiesChanged += OnSessionChanged;
        }
        await ReadCurrentAsync();
    }

    void OnSessionChanged(GlobalSystemMediaTransportControlsSession sender, object args)
        => _ = ReadCurrentAsync();

    async Task ReadCurrentAsync()
    {
        var info = new MediaInfo();
        if (_session != null)
        {
            info.HasSession = true;
            try
            {
                var props = await _session.TryGetMediaPropertiesAsync();
                info.Title = props.Title ?? "";
                info.Artist = props.Artist ?? "";
            }
            catch { }

            try
            {
                info.IsPlaying = _session.GetPlaybackInfo().PlaybackStatus ==
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            }
            catch { }

            try
            {
                var tl = _session.GetTimelineProperties();
                info.Duration = tl.EndTime - tl.StartTime;
                _basePosition = tl.Position;
                _duration = info.Duration;
            }
            catch { }
        }

        _isPlaying = info.IsPlaying;
        _baseAt = DateTime.UtcNow;
        Current = info;
        Changed?.Invoke();
    }

    void Unsubscribe()
    {
        if (_session == null) return;
        _session.MediaPropertiesChanged -= OnSessionChanged;
        _session.PlaybackInfoChanged -= OnSessionChanged;
        _session.TimelinePropertiesChanged -= OnSessionChanged;
        _session = null;
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_session == null) return;
        try
        {
            var st = _session.GetPlaybackInfo().PlaybackStatus;
            if (st == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                await _session.TryPauseAsync();
            else
                await _session.TryPlayAsync();
        }
        catch { }
    }

    public async Task NextAsync()
    {
        try { if (_session != null) await _session.TrySkipNextAsync(); } catch { }
    }

    public async Task PreviousAsync()
    {
        try { if (_session != null) await _session.TrySkipPreviousAsync(); } catch { }
    }
}
