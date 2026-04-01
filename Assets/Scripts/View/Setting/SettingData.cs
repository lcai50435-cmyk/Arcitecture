using System;

[Serializable]
public class SettingsData
{
    // “Ù∆µ
    public float masterVolume = 0.8f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public bool isMuted = false;
    public float previousMasterVolume = 0.8f;

    // ª≠√Ê
    public bool fullscreen = true;
    public int resolutionIndex = 0;
    public int refreshRateIndex = 0;

    public void SetDefault()
    {
        masterVolume = 0.8f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        isMuted = false;
        previousMasterVolume = 0.8f;
        fullscreen = true;
        resolutionIndex = 0;
        refreshRateIndex = 0;
    }
}