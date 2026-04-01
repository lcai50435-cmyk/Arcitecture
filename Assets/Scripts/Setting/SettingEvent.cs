using System;
using UnityEngine;

/// <summary>
/// 全局设置事件 - 用于各UI组件间通信
/// </summary>
public static class SettingsEvents
{
    // 音频事件
    public static Action<float> OnMasterVolumeChanged;
    public static Action<float> OnMusicVolumeChanged;
    public static Action<float> OnSFXVolumeChanged;
    public static Action<bool> OnMuteChanged;

    // 画面事件
    public static Action<int> OnResolutionIndexChanged;
    public static Action<int> OnRefreshRateIndexChanged;
    public static Action<bool> OnFullscreenChanged;

    // 分辨率数据事件
    public static Action<Resolution[]> OnResolutionsLoaded;
    public static Action<int, int> OnRefreshRatesChanged;

    // UI控制事件
    public static Action OnSettingsSaved;
    public static Action OnSettingsLoaded;
    public static Action OnSettingsReset;
    public static Action OnExitGame;
    public static Action OnApplyVideo;

    // 提示事件
    public static Action<string> OnShowMessage;
}