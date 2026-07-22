using System;
using UnityEngine;

public enum GameInputAction
{
    Attack = 0,
    Interact = 1,
    OpenMap = 2,
    Pause = 3,
    PhotoCapture = 4
}

public enum GameDisplayMode
{
    Windowed = 0,
    Fullscreen = 1
}

public enum GameAudioToggle
{
    MuteMode = 0,
    MusicCrossfade = 1,
    SfxDynamicRange = 2,
    SpatialAudio = 3
}

public sealed class GameSettingsDraft
{
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;
    public bool muteMode;
    public bool musicCrossfade;
    public bool sfxDynamicRange;
    public bool spatialAudio;
    public int resolutionIndex;
    public GameDisplayMode displayMode;
    public int viewZoomIndex;
    public KeyCode attackKey;
    public KeyCode interactKey;
    public KeyCode openMapKey;
    public KeyCode pauseKey;
    public KeyCode photoCaptureKey;

    public GameSettingsDraft Clone()
    {
        return new GameSettingsDraft
        {
            masterVolume = masterVolume,
            musicVolume = musicVolume,
            sfxVolume = sfxVolume,
            muteMode = muteMode,
            musicCrossfade = musicCrossfade,
            sfxDynamicRange = sfxDynamicRange,
            spatialAudio = spatialAudio,
            resolutionIndex = resolutionIndex,
            displayMode = displayMode,
            viewZoomIndex = viewZoomIndex,
            attackKey = attackKey,
            interactKey = interactKey,
            openMapKey = openMapKey,
            pauseKey = pauseKey,
            photoCaptureKey = photoCaptureKey
        };
    }

    public bool GetAudioToggle(GameAudioToggle toggle)
    {
        switch (toggle)
        {
            case GameAudioToggle.MuteMode:
                return muteMode;
            case GameAudioToggle.MusicCrossfade:
                return musicCrossfade;
            case GameAudioToggle.SfxDynamicRange:
                return sfxDynamicRange;
            case GameAudioToggle.SpatialAudio:
                return spatialAudio;
            default:
                return false;
        }
    }

    public void SetAudioToggle(GameAudioToggle toggle, bool enabled)
    {
        switch (toggle)
        {
            case GameAudioToggle.MuteMode:
                muteMode = enabled;
                break;
            case GameAudioToggle.MusicCrossfade:
                musicCrossfade = enabled;
                break;
            case GameAudioToggle.SfxDynamicRange:
                sfxDynamicRange = enabled;
                break;
            case GameAudioToggle.SpatialAudio:
                spatialAudio = enabled;
                break;
        }
    }

    public KeyCode GetBinding(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.Attack:
                return attackKey;
            case GameInputAction.Interact:
                return interactKey;
            case GameInputAction.OpenMap:
                return openMapKey;
            case GameInputAction.Pause:
                return pauseKey;
            case GameInputAction.PhotoCapture:
                return photoCaptureKey;
            default:
                return KeyCode.None;
        }
    }

    public void SetBinding(GameInputAction action, KeyCode keyCode)
    {
        switch (action)
        {
            case GameInputAction.Attack:
                attackKey = keyCode;
                break;
            case GameInputAction.Interact:
                interactKey = keyCode;
                break;
            case GameInputAction.OpenMap:
                openMapKey = keyCode;
                break;
            case GameInputAction.Pause:
                pauseKey = keyCode;
                break;
            case GameInputAction.PhotoCapture:
                photoCaptureKey = keyCode;
                break;
        }
    }
}

public static class GameSettingsStore
{
    private const string LegacyMasterVolumeKey = "GameVolume";
    private const string MasterVolumeKey = "GameSettings.MasterVolume";
    private const string MusicVolumeKey = "GameSettings.MusicVolume";
    private const string SfxVolumeKey = "GameSettings.SfxVolume";
    private const string MuteModeKey = "GameSettings.Audio.MuteMode";
    private const string MusicCrossfadeKey = "GameSettings.Audio.MusicCrossfade";
    private const string SfxDynamicRangeKey = "GameSettings.Audio.SfxDynamicRange";
    private const string SpatialAudioKey = "GameSettings.Audio.SpatialAudio";
    private const string ResolutionIndexKey = "ResolutionIndex";
    private const string DisplayModeKey = "GameSettings.DisplayMode";
    private const string ViewZoomIndexKey = "GameSettings.ViewZoomIndex";
    private const float DefaultMasterVolume = 1f;
    private const float DefaultMusicVolume = 0.85f;
    private const float DefaultSfxVolume = 1f;
    private const bool DefaultMuteMode = false;
    private const bool DefaultMusicCrossfade = true;
    private const bool DefaultSfxDynamicRange = false;
    private const bool DefaultSpatialAudio = false;
    private const int DefaultResolutionIndex = 3;
    private const int DefaultViewZoomIndex = 1;

    private static readonly Vector2Int[] ResolutionOptions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(1280, 800),
        new Vector2Int(1440, 900),
        new Vector2Int(1680, 1050)
    };

    private static readonly float[] ViewZoomMultipliers =
    {
        0.9f,
        1f,
        1.1f,
        1.2f
    };

    public static int ResolutionOptionCount => ResolutionOptions.Length;
    public static int ViewZoomOptionCount => ViewZoomMultipliers.Length;

    public static GameSettingsDraft LoadSavedSettings()
    {
        return new GameSettingsDraft
        {
            masterVolume = LoadSavedMasterVolume(),
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume)),
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume)),
            muteMode = LoadBool(MuteModeKey, DefaultMuteMode),
            musicCrossfade = LoadBool(MusicCrossfadeKey, DefaultMusicCrossfade),
            sfxDynamicRange = LoadBool(SfxDynamicRangeKey, DefaultSfxDynamicRange),
            spatialAudio = LoadBool(SpatialAudioKey, DefaultSpatialAudio),
            resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionIndexKey, DefaultResolutionIndex), 0, ResolutionOptions.Length - 1),
            displayMode = LoadSavedDisplayMode(),
            viewZoomIndex = Mathf.Clamp(PlayerPrefs.GetInt(ViewZoomIndexKey, DefaultViewZoomIndex), 0, ViewZoomMultipliers.Length - 1),
            attackKey = LoadSavedKeyBinding(GameInputAction.Attack),
            interactKey = LoadSavedKeyBinding(GameInputAction.Interact),
            openMapKey = LoadSavedKeyBinding(GameInputAction.OpenMap),
            pauseKey = LoadSavedKeyBinding(GameInputAction.Pause),
            photoCaptureKey = LoadSavedKeyBinding(GameInputAction.PhotoCapture)
        };
    }

    public static GameSettingsDraft CreateDraftFromSaved()
    {
        return LoadSavedSettings();
    }

    public static GameSettingsDraft CreateDefaultDraft()
    {
        return new GameSettingsDraft
        {
            masterVolume = DefaultMasterVolume,
            musicVolume = DefaultMusicVolume,
            sfxVolume = DefaultSfxVolume,
            muteMode = DefaultMuteMode,
            musicCrossfade = DefaultMusicCrossfade,
            sfxDynamicRange = DefaultSfxDynamicRange,
            spatialAudio = DefaultSpatialAudio,
            resolutionIndex = DefaultResolutionIndex,
            displayMode = GameDisplayMode.Windowed,
            viewZoomIndex = DefaultViewZoomIndex,
            attackKey = GetDefaultKey(GameInputAction.Attack),
            interactKey = GetDefaultKey(GameInputAction.Interact),
            openMapKey = GetDefaultKey(GameInputAction.OpenMap),
            pauseKey = GetDefaultKey(GameInputAction.Pause),
            photoCaptureKey = GetDefaultKey(GameInputAction.PhotoCapture)
        };
    }

    public static GameSettingsDraft DiscardDraft(GameSettingsDraft savedSettings)
    {
        return savedSettings != null ? savedSettings.Clone() : CreateDraftFromSaved();
    }

    public static void PreviewDraftAudio(GameSettingsDraft draft)
    {
        GameSettingsDraft source = draft ?? LoadSavedSettings();
        float masterVolume = Mathf.Clamp01(source.masterVolume);
        float musicVolume = Mathf.Clamp01(source.musicVolume);
        float sfxVolume = Mathf.Clamp01(source.sfxVolume);
        float effectiveMasterVolume = source.muteMode ? 0f : masterVolume;

        AudioListener.volume = effectiveMasterVolume;

        MusicManager manager = MusicManager.Instance != null
            ? MusicManager.Instance
            : MusicManager.EnsureInstance();
        if (manager != null)
        {
            manager.ApplyVolumeSettings(effectiveMasterVolume, musicVolume, sfxVolume);
        }
    }

    public static void ApplyDraft(GameSettingsDraft draft)
    {
        GameSettingsDraft source = draft ?? CreateDefaultDraft();

        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(source.masterVolume));
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(source.musicVolume));
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(source.sfxVolume));
        SaveBool(MuteModeKey, source.muteMode);
        SaveBool(MusicCrossfadeKey, source.musicCrossfade);
        SaveBool(SfxDynamicRangeKey, source.sfxDynamicRange);
        SaveBool(SpatialAudioKey, source.spatialAudio);
        PlayerPrefs.SetInt(ResolutionIndexKey, Mathf.Clamp(source.resolutionIndex, 0, ResolutionOptions.Length - 1));
        PlayerPrefs.SetInt(DisplayModeKey, (int)source.displayMode);
        PlayerPrefs.SetInt(ViewZoomIndexKey, Mathf.Clamp(source.viewZoomIndex, 0, ViewZoomMultipliers.Length - 1));
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Attack), (int)source.attackKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Interact), (int)source.interactKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.OpenMap), (int)source.openMapKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Pause), (int)source.pauseKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.PhotoCapture), (int)source.photoCaptureKey);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        ApplyDisplaySettings();
    }

    public static bool IsDirty(GameSettingsDraft savedSettings, GameSettingsDraft draftSettings)
    {
        if (savedSettings == null || draftSettings == null)
        {
            return false;
        }

        return !Mathf.Approximately(savedSettings.masterVolume, draftSettings.masterVolume) ||
               !Mathf.Approximately(savedSettings.musicVolume, draftSettings.musicVolume) ||
               !Mathf.Approximately(savedSettings.sfxVolume, draftSettings.sfxVolume) ||
               savedSettings.muteMode != draftSettings.muteMode ||
               savedSettings.musicCrossfade != draftSettings.musicCrossfade ||
               savedSettings.sfxDynamicRange != draftSettings.sfxDynamicRange ||
               savedSettings.spatialAudio != draftSettings.spatialAudio ||
               savedSettings.resolutionIndex != draftSettings.resolutionIndex ||
               savedSettings.displayMode != draftSettings.displayMode ||
               savedSettings.viewZoomIndex != draftSettings.viewZoomIndex ||
               savedSettings.attackKey != draftSettings.attackKey ||
               savedSettings.interactKey != draftSettings.interactKey ||
               savedSettings.openMapKey != draftSettings.openMapKey ||
               savedSettings.pauseKey != draftSettings.pauseKey ||
               savedSettings.photoCaptureKey != draftSettings.photoCaptureKey;
    }

    public static Vector2Int GetResolutionOption(int index)
    {
        return ResolutionOptions[Mathf.Clamp(index, 0, ResolutionOptions.Length - 1)];
    }

    public static int GetResolutionIndex()
    {
        return LoadSavedSettings().resolutionIndex;
    }

    public static int GetViewZoomIndex()
    {
        return LoadSavedSettings().viewZoomIndex;
    }

    public static float GetViewZoomMultiplier(int index)
    {
        return ViewZoomMultipliers[Mathf.Clamp(index, 0, ViewZoomMultipliers.Length - 1)];
    }

    public static string GetViewZoomLabel(int index)
    {
        return $"{Mathf.RoundToInt(GetViewZoomMultiplier(index) * 100f)}%";
    }

    public static GameDisplayMode GetDisplayMode()
    {
        return LoadSavedDisplayMode();
    }

    public static float GetMasterVolume()
    {
        return LoadSavedSettings().masterVolume;
    }

    public static float GetMusicVolume()
    {
        return LoadSavedSettings().musicVolume;
    }

    public static float GetSfxVolume()
    {
        return LoadSavedSettings().sfxVolume;
    }

    public static bool GetAudioToggle(GameAudioToggle toggle)
    {
        return LoadSavedSettings().GetAudioToggle(toggle);
    }

    public static void ApplyDisplaySettings()
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        if (ShouldApplyExplicitResolutionForCurrentPlatform())
        {
            Vector2Int resolution = GetResolutionOption(savedSettings.resolutionIndex);
            bool fullscreen = savedSettings.displayMode == GameDisplayMode.Fullscreen;
            Screen.SetResolution(resolution.x, resolution.y, fullscreen);
        }

        ScreenAdaptationManager.RefreshNow();
    }

    public static bool ShouldApplyExplicitResolutionForPlatform(bool isWebGlPlayer)
    {
        return !isWebGlPlayer;
    }

    private static bool ShouldApplyExplicitResolutionForCurrentPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return ShouldApplyExplicitResolutionForPlatform(true);
#else
        return ShouldApplyExplicitResolutionForPlatform(false);
#endif
    }

    public static void ApplyAudioSettings()
    {
        PreviewDraftAudio(LoadSavedSettings());
    }

    public static KeyCode GetKeyBinding(GameInputAction action)
    {
        return LoadSavedSettings().GetBinding(action);
    }

    public static void SetKeyBinding(GameInputAction action, KeyCode keyCode)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.SetBinding(action, keyCode);
        ApplyDraft(savedSettings);
    }

    public static void SetMasterVolume(float value, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.masterVolume = Mathf.Clamp01(value);
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void SetMusicVolume(float value, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.musicVolume = Mathf.Clamp01(value);
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void SetSfxVolume(float value, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.sfxVolume = Mathf.Clamp01(value);
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void SetAudioToggle(GameAudioToggle toggle, bool enabled, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.SetAudioToggle(toggle, enabled);
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void SetResolutionIndex(int index, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.resolutionIndex = Mathf.Clamp(index, 0, ResolutionOptions.Length - 1);
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void SetDisplayMode(GameDisplayMode mode, bool applyImmediately = true)
    {
        GameSettingsDraft savedSettings = LoadSavedSettings();
        savedSettings.displayMode = mode;
        SaveCompatChange(savedSettings, applyImmediately);
    }

    public static void ResetAll()
    {
        ApplyDraft(CreateDefaultDraft());
    }

    public static KeyCode GetDefaultKey(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.Attack:
                return KeyCode.Mouse0;
            case GameInputAction.Interact:
                return KeyCode.F;
            case GameInputAction.OpenMap:
                return KeyCode.M;
            case GameInputAction.Pause:
                return KeyCode.Escape;
            case GameInputAction.PhotoCapture:
                return KeyCode.P;
            default:
                return KeyCode.None;
        }
    }

    public static string GetActionDisplayName(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.Attack:
                return "攻击";
            case GameInputAction.Interact:
                return "交互";
            case GameInputAction.OpenMap:
                return "地图";
            case GameInputAction.Pause:
                return "暂停";
            case GameInputAction.PhotoCapture:
                return "拍照";
            default:
                return action.ToString();
        }
    }

    public static string GetResolutionLabel(int index)
    {
        Vector2Int resolution = GetResolutionOption(index);
        return $"{resolution.x} x {resolution.y}";
    }

    public static string GetAspectLabel(int index)
    {
        Vector2Int resolution = GetResolutionOption(index);
        return FormatAspectLabel(resolution.x, resolution.y);
    }

    public static string GetCurrentRuntimeResolutionLabel()
    {
        return $"{Mathf.Max(Screen.width, 1)} x {Mathf.Max(Screen.height, 1)}";
    }

    public static string GetCurrentRuntimeAspectLabel()
    {
        return FormatAspectLabel(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1));
    }

    public static string GetCurrentRuntimeDisplayModeLabel()
    {
        return Screen.fullScreen ? "全屏" : "窗口";
    }

    public static string GetCurrentRuntimeViewZoomLabel()
    {
        string runtimeLabel = ScreenAdaptationManager.GetCurrentAppliedViewZoomLabel();
        return string.IsNullOrEmpty(runtimeLabel) ? GetViewZoomLabel(GetViewZoomIndex()) : runtimeLabel;
    }

    public static string GetDisplayModeLabel(GameDisplayMode mode)
    {
        return mode == GameDisplayMode.Fullscreen ? "全屏" : "窗口";
    }

    public static string GetKeyDisplayName(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0:
                return "鼠标左键";
            case KeyCode.Mouse1:
                return "鼠标右键";
            case KeyCode.Mouse2:
                return "鼠标中键";
            case KeyCode.LeftShift:
                return "左 Shift";
            case KeyCode.RightShift:
                return "右 Shift";
            case KeyCode.LeftControl:
                return "左 Ctrl";
            case KeyCode.RightControl:
                return "右 Ctrl";
            case KeyCode.LeftAlt:
                return "左 Alt";
            case KeyCode.RightAlt:
                return "右 Alt";
            case KeyCode.Space:
                return "空格";
            case KeyCode.Return:
                return "回车";
            case KeyCode.Escape:
                return "Esc";
            case KeyCode.Tab:
                return "Tab";
            case KeyCode.UpArrow:
                return "上方向键";
            case KeyCode.DownArrow:
                return "下方向键";
            case KeyCode.LeftArrow:
                return "左方向键";
            case KeyCode.RightArrow:
                return "右方向键";
        }

        string rawName = keyCode.ToString();
        if (rawName.StartsWith("Alpha"))
        {
            return rawName.Substring("Alpha".Length);
        }

        if (rawName.StartsWith("Keypad"))
        {
            return rawName.Replace("Keypad", "小键盘 ");
        }

        return rawName;
    }

    private static void SaveCompatChange(GameSettingsDraft draft, bool applyImmediately)
    {
        if (applyImmediately)
        {
            ApplyDraft(draft);
            return;
        }

        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(draft.masterVolume));
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(draft.musicVolume));
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(draft.sfxVolume));
        SaveBool(MuteModeKey, draft.muteMode);
        SaveBool(MusicCrossfadeKey, draft.musicCrossfade);
        SaveBool(SfxDynamicRangeKey, draft.sfxDynamicRange);
        SaveBool(SpatialAudioKey, draft.spatialAudio);
        PlayerPrefs.SetInt(ResolutionIndexKey, Mathf.Clamp(draft.resolutionIndex, 0, ResolutionOptions.Length - 1));
        PlayerPrefs.SetInt(DisplayModeKey, (int)draft.displayMode);
        PlayerPrefs.SetInt(ViewZoomIndexKey, Mathf.Clamp(draft.viewZoomIndex, 0, ViewZoomMultipliers.Length - 1));
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Attack), (int)draft.attackKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Interact), (int)draft.interactKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.OpenMap), (int)draft.openMapKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.Pause), (int)draft.pauseKey);
        PlayerPrefs.SetInt(GetInputActionPrefKey(GameInputAction.PhotoCapture), (int)draft.photoCaptureKey);
        PlayerPrefs.Save();
    }

    private static GameDisplayMode LoadSavedDisplayMode()
    {
        int rawValue = PlayerPrefs.GetInt(DisplayModeKey, (int)GameDisplayMode.Windowed);
        return rawValue == (int)GameDisplayMode.Fullscreen
            ? GameDisplayMode.Fullscreen
            : GameDisplayMode.Windowed;
    }

    private static float LoadSavedMasterVolume()
    {
        if (PlayerPrefs.HasKey(MasterVolumeKey))
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
        }

        return Mathf.Clamp01(PlayerPrefs.GetFloat(LegacyMasterVolumeKey, DefaultMasterVolume));
    }

    private static bool LoadBool(string key, bool defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
    }

    private static void SaveBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    private static KeyCode LoadSavedKeyBinding(GameInputAction action)
    {
        KeyCode defaultKey = GetDefaultKey(action);
        int rawValue = PlayerPrefs.GetInt(GetInputActionPrefKey(action), (int)defaultKey);
        return Enum.IsDefined(typeof(KeyCode), rawValue) ? (KeyCode)rawValue : defaultKey;
    }

    private static string FormatAspectLabel(int width, int height)
    {
        int safeWidth = Mathf.Max(1, width);
        int safeHeight = Mathf.Max(1, height);
        float aspect = safeWidth / (float)safeHeight;

        if (ApproximatelyAspect(aspect, 16f / 9f))
        {
            return "16:9";
        }

        if (ApproximatelyAspect(aspect, 16f / 10f))
        {
            return "16:10";
        }

        if (ApproximatelyAspect(aspect, 21f / 9f))
        {
            return "21:9";
        }

        if (ApproximatelyAspect(aspect, 4f / 3f))
        {
            return "4:3";
        }

        int divisor = GreatestCommonDivisor(safeWidth, safeHeight);
        return $"{safeWidth / divisor}:{safeHeight / divisor}";
    }

    private static bool ApproximatelyAspect(float aspect, float target)
    {
        return Mathf.Abs(aspect - target) <= 0.02f;
    }

    private static string GetInputActionPrefKey(GameInputAction action)
    {
        return $"GameSettings.Key.{action}";
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            int remainder = a % b;
            a = b;
            b = remainder;
        }

        return Mathf.Max(1, Mathf.Abs(a));
    }
}
