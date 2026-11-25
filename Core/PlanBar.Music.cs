using System;
using System.Threading.Tasks;

namespace DailyPlan.Core;

// ===== 音乐服务生命周期 =====
partial class PlanBar
{
    async Task InitMusicAsync()
    {
        await _music.InitAsync();
        // 初始化完成后，若已有活动会话，触发一次渲染
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
        Render();
    }
}
