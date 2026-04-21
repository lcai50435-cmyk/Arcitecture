using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    private AudioSource bgmSource;

    void Awake()
    {
        // 防止重复创建
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动获取 Audio Source
        bgmSource = GetComponent<AudioSource>();

        // 确保音乐播放
        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void SetVolume(float volume)
    {
        if (bgmSource != null)
            bgmSource.volume = volume;
    }
}