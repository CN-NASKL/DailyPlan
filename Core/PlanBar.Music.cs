using System;
using System.Threading.Tasks;

namespace DailyPlan.Core;

// ===== 音乐服务生命周期 + 歌词滚动刷新 =====
partial class PlanBar
{
    async Task InitMusicAsync()
    {
        await _music.InitAsync();
        // 初始化完成后，若已有活动会话，触发一次渲染与歌词抓取
        OnMusicChangedInternal();
    }

    // SMTC 事件在后台线程触发，marshal 到 UI 线程
    void OnMusicChanged()
    {
        if (IsDisposed || !IsHandleCreated) return;
        BeginInvoke((Action)OnMusicChangedInternal);
    }

    void OnMusicChangedInternal()
    {
        _lastLyricIndex = -1;
        // 歌词暂不在主栏显示（安排信息靠左、歌名靠右），不再联网抓取，lyrics 恒空
        _lyrics.Clear();
        Render();
    }

    void OnLyricsChanged()
    {
        if (IsDisposed || !IsHandleCreated) return;
        BeginInvoke((Action)(() => { _lastLyricIndex = -1; Render(); }));
    }

    // 250ms 心跳：仅在「有同步歌词且播放中」时按位置刷新当前歌词行
    void MusicFrameTick()
    {
        var info = _music.Current;
        var lyrics = _lyrics.Current;
        if (!info.HasSession || !info.IsPlaying) return;
        if (lyrics == null || !lyrics.IsSynced || lyrics.Lines.Count == 0) return;

        int idx = lyrics.IndexAt(_music.EstimatedPosition);
        if (idx != _lastLyricIndex)
        {
            _lastLyricIndex = idx;
            Render();
        }
    }
}
