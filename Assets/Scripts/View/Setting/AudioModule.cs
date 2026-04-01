using UnityEngine;

/// <summary>
/// 音频模块 - 挂载到音频源对象上
/// </summary>
public class AudioModule : MonoBehaviour
{
    [Header("音频源")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private float currentMasterVolume = 0.8f;
    private float currentMusicVolume = 0.7f;
    private float currentSFXVolume = 0.8f;

    private void OnEnable()
    {
        // 订阅事件
        SettingsEvents.OnMasterVolumeChanged += OnMasterVolumeChanged;
        SettingsEvents.OnMusicVolumeChanged += OnMusicVolumeChanged;
        SettingsEvents.OnSFXVolumeChanged += OnSFXVolumeChanged;
        SettingsEvents.OnMuteChanged += OnMuteChanged;
    }

    private void OnDisable()
    {
        // 取消订阅
        SettingsEvents.OnMasterVolumeChanged -= OnMasterVolumeChanged;
        SettingsEvents.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        SettingsEvents.OnSFXVolumeChanged -= OnSFXVolumeChanged;
        SettingsEvents.OnMuteChanged -= OnMuteChanged;
    }

    private void OnMasterVolumeChanged(float volume)
    {
        currentMasterVolume = volume;
        ApplyVolumes();
    }

    private void OnMusicVolumeChanged(float volume)
    {
        currentMusicVolume = volume;
        ApplyVolumes();
    }

    private void OnSFXVolumeChanged(float volume)
    {
        currentSFXVolume = volume;
        ApplyVolumes();
    }

    private void OnMuteChanged(bool muted)
    {
        // 静音已经在SettingsManager中处理了主音量
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = currentMasterVolume * currentMusicVolume;

        if (sfxSource != null)
            sfxSource.volume = currentMasterVolume * currentSFXVolume;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null && currentMasterVolume > 0)
        {
            sfxSource.PlayOneShot(clip, currentSFXVolume);
        }
    }

    /// <summary>
    /// 播放音乐
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource != null && clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }
}