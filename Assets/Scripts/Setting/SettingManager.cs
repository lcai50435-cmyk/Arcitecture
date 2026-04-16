using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 设置核心管理器 - 单例，负责数据存储和分发
/// </summary>
public class SettingsManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private string settingsFileName = "gamesettings.json";

    private SettingsData currentData;
    private string settingsFilePath;

    // 分辨率数据
    private Resolution[] allResolutions;
    private Resolution[] uniqueResolutions;
    private Dictionary<int, List<int>> resolutionRefreshRates;

    // 单例
    public static SettingsManager Instance { get; private set; }

    // 公共属性
    public SettingsData Data => currentData;
    public Resolution[] AvailableResolutions => uniqueResolutions;
    public List<int> GetRefreshRates(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < uniqueResolutions.Length)
        {
            var res = uniqueResolutions[resolutionIndex];
            if (resolutionRefreshRates.ContainsKey(res.width * 10000 + res.height))
            {
                return resolutionRefreshRates[res.width * 10000 + res.height];
            }
        }
        return new List<int> { 60 };
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        settingsFilePath = Path.Combine(Application.persistentDataPath, settingsFileName);
        currentData = new SettingsData();

        LoadResolutions();
        LoadSettings();
    }

    private void Start()
    {
        // 应用设置
        ApplyAudioSettings();
        ApplyVideoSettings();

        // 触发加载完成事件
        SettingsEvents.OnSettingsLoaded?.Invoke();
    }

    /// <summary>
    /// 加载所有可用分辨率
    /// </summary>
    private void LoadResolutions()
    {
        allResolutions = Screen.resolutions;
        resolutionRefreshRates = new Dictionary<int, List<int>>();

        // 去重分辨率
        var uniqueDict = new Dictionary<string, Resolution>();
        foreach (var res in allResolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!uniqueDict.ContainsKey(key))
            {
                uniqueDict.Add(key, res);
            }

            // 存储刷新率
            int hash = res.width * 10000 + res.height;
            if (!resolutionRefreshRates.ContainsKey(hash))
            {
                resolutionRefreshRates[hash] = new List<int>();
            }
            if (!resolutionRefreshRates[hash].Contains(res.refreshRate))
            {
                resolutionRefreshRates[hash].Add(res.refreshRate);
            }
        }

        uniqueResolutions = uniqueDict.Values.ToArray();

        // 排序分辨率
        System.Array.Sort(uniqueResolutions, (a, b) =>
        {
            if (a.width != b.width) return a.width.CompareTo(b.width);
            return a.height.CompareTo(b.height);
        });

        // 排序刷新率
        foreach (var key in resolutionRefreshRates.Keys.ToList())
        {
            resolutionRefreshRates[key].Sort();
        }

        // 触发事件
        SettingsEvents.OnResolutionsLoaded?.Invoke(uniqueResolutions);
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                JsonUtility.FromJsonOverwrite(json, currentData);
                Debug.Log("设置加载成功");
            }
            else
            {
                currentData.SetDefault();
                Debug.Log("使用默认设置");
            }

            // 验证索引有效性
            if (currentData.resolutionIndex < 0 || currentData.resolutionIndex >= uniqueResolutions.Length)
            {
                currentData.resolutionIndex = 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载设置失败: {e.Message}");
            currentData.SetDefault();
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(currentData, true);
            File.WriteAllText(settingsFilePath, json);
            Debug.Log("设置已保存");
            SettingsEvents.OnSettingsSaved?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存设置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 应用音频设置
    /// </summary>
    private void ApplyAudioSettings()
    {
        SettingsEvents.OnMasterVolumeChanged?.Invoke(currentData.masterVolume);
        SettingsEvents.OnMusicVolumeChanged?.Invoke(currentData.musicVolume);
        SettingsEvents.OnSFXVolumeChanged?.Invoke(currentData.sfxVolume);
        SettingsEvents.OnMuteChanged?.Invoke(currentData.isMuted);
    }

    /// <summary>
    /// 应用画面设置
    /// </summary>
    private void ApplyVideoSettings()
    {
        SettingsEvents.OnResolutionIndexChanged?.Invoke(currentData.resolutionIndex);
        SettingsEvents.OnRefreshRateIndexChanged?.Invoke(currentData.refreshRateIndex);
        SettingsEvents.OnFullscreenChanged?.Invoke(currentData.fullscreen);
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    public void SetMasterVolume(float value)
    {
        currentData.masterVolume = Mathf.Clamp01(value);
        SettingsEvents.OnMasterVolumeChanged?.Invoke(currentData.masterVolume);
        SaveSettings();
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public void SetMusicVolume(float value)
    {
        currentData.musicVolume = Mathf.Clamp01(value);
        SettingsEvents.OnMusicVolumeChanged?.Invoke(currentData.musicVolume);
        SaveSettings();
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float value)
    {
        currentData.sfxVolume = Mathf.Clamp01(value);
        SettingsEvents.OnSFXVolumeChanged?.Invoke(currentData.sfxVolume);
        SaveSettings();
    }

    /// <summary>
    /// 设置静音
    /// </summary>
    public void SetMute(bool muted)
    {
        if (muted)
        {
            currentData.previousMasterVolume = currentData.masterVolume;
            currentData.masterVolume = 0f;
        }
        else
        {
            currentData.masterVolume = currentData.previousMasterVolume;
        }
        currentData.isMuted = muted;

        SettingsEvents.OnMuteChanged?.Invoke(muted);
        SettingsEvents.OnMasterVolumeChanged?.Invoke(currentData.masterVolume);
        SaveSettings();
    }

    /// <summary>
    /// 设置分辨率索引
    /// </summary>
    public void SetResolutionIndex(int index)
    {
        if (index < 0 || index >= uniqueResolutions.Length) return;
        currentData.resolutionIndex = index;
        currentData.refreshRateIndex = 0;

        SettingsEvents.OnResolutionIndexChanged?.Invoke(index);

        // 触发刷新率更新
        var rates = GetRefreshRates(index);
        SettingsEvents.OnRefreshRatesChanged?.Invoke(index, rates.Count > 0 ? rates[0] : 60);
        SettingsEvents.OnRefreshRateIndexChanged?.Invoke(0);

        SaveSettings();
    }

    /// <summary>
    /// 设置刷新率索引
    /// </summary>
    public void SetRefreshRateIndex(int index)
    {
        currentData.refreshRateIndex = index;
        SettingsEvents.OnRefreshRateIndexChanged?.Invoke(index);
        SaveSettings();
    }

    /// <summary>
    /// 设置全屏
    /// </summary>
    public void SetFullscreen(bool fullscreen)
    {
        currentData.fullscreen = fullscreen;
        SettingsEvents.OnFullscreenChanged?.Invoke(fullscreen);
        SaveSettings();
    }

    /// <summary>
    /// 应用分辨率
    /// </summary>
    public void ApplyResolution()
    {
        if (currentData.resolutionIndex < 0 || currentData.resolutionIndex >= uniqueResolutions.Length) return;

        var res = uniqueResolutions[currentData.resolutionIndex];
        var rates = GetRefreshRates(currentData.resolutionIndex);
        int refreshRate = currentData.refreshRateIndex < rates.Count ? rates[currentData.refreshRateIndex] : 60;

        Screen.SetResolution(res.width, res.height, currentData.fullscreen, refreshRate);
        SettingsEvents.OnShowMessage?.Invoke($"分辨率已更改为 {res.width}×{res.height}");
    }

    /// <summary>
    /// 重置为默认
    /// </summary>
    public void ResetToDefault()
    {
        currentData.SetDefault();
        ApplyAudioSettings();
        ApplyVideoSettings();
        SaveSettings();
        SettingsEvents.OnSettingsReset?.Invoke();
        SettingsEvents.OnShowMessage?.Invoke("已重置为默认设置");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void ExitGame()
    {
        SaveSettings();
        SettingsEvents.OnExitGame?.Invoke();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
