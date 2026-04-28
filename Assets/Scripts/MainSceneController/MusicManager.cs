using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private const string AudioCatalogResourcePath = "Audio/ArcitectureAudioCatalog";
    private const string SecondaryBgmSourceName = "SecondaryBgmSource";
    private const string SfxSourceName = "SfxSource";
    private const float SceneCrossfadeDuration = 0.8f;
    private const float CombatEnterFadeDuration = 0.45f;
    private const float CombatExitFadeDuration = 1.2f;
    private const float CombatScanInterval = 0.2f;
    private const float CombatReleaseDelay = 3f;
    private const float SfxMinRetriggerInterval = 0.08f;
    private const float SfxPitchMin = 0.96f;
    private const float SfxPitchMax = 1.04f;

    public static MusicManager Instance;

    private readonly AudioSource[] bgmSources = new AudioSource[2];
    private readonly MusicCueId[] bgmSourceCues = new MusicCueId[2];
    private readonly float[] bgmSourceBlendWeights = new float[2];
    private readonly Dictionary<SfxCueId, float> lastSfxPlayTimes = new Dictionary<SfxCueId, float>();

    private ArcitectureAudioCatalog audioCatalog;
    private AudioSource sfxSource;
    private Coroutine bgmTransitionCoroutine;
    private MusicCueId currentSceneCue = MusicCueId.None;
    private MusicCueId currentMusicCue = MusicCueId.None;
    private int activeBgmSourceIndex;
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private float nextCombatScanAt;
    private float lastCombatDetectedAt = float.NegativeInfinity;
    private bool isCombatMusicActive;
    private bool gameplayMusicPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static MusicManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        MusicManager existing = FindObjectOfType<MusicManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new GameObject(nameof(MusicManager));
        return managerObject.AddComponent<MusicManager>();
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

        LoadAudioCatalog();
        EnsureAudioSources();
        ApplyVolumeSettings(masterVolume, musicVolume, sfxVolume);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        SyncSceneCue(SceneManager.GetActiveScene().name, true, 0f);
    }

    private void Update()
    {
        if (gameplayMusicPaused)
        {
            return;
        }

        UpdateCombatMusicState();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public void ApplyVolumeSettings(float nextMasterVolume, float nextMusicVolume, float nextSfxVolume)
    {
        masterVolume = Mathf.Clamp01(nextMasterVolume);
        musicVolume = Mathf.Clamp01(nextMusicVolume);
        sfxVolume = Mathf.Clamp01(nextSfxVolume);

        AudioListener.volume = masterVolume;
        UpdateAllBgmVolumes();

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllBgmVolumes();
    }

    public static void PlaySfx(SfxCueId cueId)
    {
        MusicManager manager = EnsureInstance();
        if (manager == null)
        {
            return;
        }

        manager.PlaySfxInternal(cueId);
    }

    public static void PlayButtonClickSfx()
    {
        PlaySfx(SfxCueId.ButtonClick);
    }

    public static void SetGameplayMusicPaused(bool paused)
    {
        MusicManager manager = Instance != null ? Instance : EnsureInstance();
        if (manager == null)
        {
            return;
        }

        manager.SetGameplayMusicPausedInternal(paused);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SyncSceneCue(scene.name, false, SceneCrossfadeDuration);
    }

    private void SyncSceneCue(string sceneName, bool immediate, float fadeDuration)
    {
        MusicCueId nextSceneCue = ResolveSceneCue(sceneName);
        currentSceneCue = nextSceneCue;

        if (!GameplayStageCatalog.IsGameplayScene(sceneName))
        {
            isCombatMusicActive = false;
            lastCombatDetectedAt = float.NegativeInfinity;
        }

        if (nextSceneCue == MusicCueId.None)
        {
            return;
        }

        PlayMusicCue(ResolveDesiredCue(), immediate ? 0f : fadeDuration);
    }

    private void UpdateCombatMusicState()
    {
        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (Time.unscaledTime < nextCombatScanAt)
        {
            return;
        }

        nextCombatScanAt = Time.unscaledTime + CombatScanInterval;
        bool hasCombatThreat = HasCombatThreat();

        if (hasCombatThreat)
        {
            lastCombatDetectedAt = Time.unscaledTime;
            if (!isCombatMusicActive)
            {
                isCombatMusicActive = true;
                PlayMusicCue(ResolveDesiredCue(), CombatEnterFadeDuration);
            }

            return;
        }

        if (!isCombatMusicActive)
        {
            return;
        }

        if (Time.unscaledTime - lastCombatDetectedAt < CombatReleaseDelay)
        {
            return;
        }

        isCombatMusicActive = false;
        PlayMusicCue(ResolveDesiredCue(), CombatExitFadeDuration);
    }

    private bool HasCombatThreat()
    {
        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStatsManager enemy = enemies[i];
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            if (enemy.CurrentState == EnemyState.Chase ||
                enemy.CurrentState == EnemyState.Attack ||
                enemy.HasPlayerInRange)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayMusicCue(MusicCueId targetCue, float fadeDuration)
    {
        AudioClip targetClip = GetMusicClip(targetCue);
        if (targetCue == MusicCueId.None || targetClip == null)
        {
            return;
        }

        EnsureAudioSources();
        if (TryAdoptExistingPlayback(targetCue, targetClip))
        {
            return;
        }

        if (bgmTransitionCoroutine != null)
        {
            StopCoroutine(bgmTransitionCoroutine);
            bgmTransitionCoroutine = null;
        }

        if (currentMusicCue == targetCue && bgmSourceCues[activeBgmSourceIndex] == targetCue)
        {
            UpdateAllBgmVolumes();
            return;
        }

        if (fadeDuration <= 0f || !bgmSources[activeBgmSourceIndex].isPlaying)
        {
            PlayCueImmediate(targetCue, targetClip);
            return;
        }

        bgmTransitionCoroutine = StartCoroutine(CrossfadeRoutine(targetCue, targetClip, fadeDuration));
    }

    private bool TryAdoptExistingPlayback(MusicCueId targetCue, AudioClip targetClip)
    {
        if (currentMusicCue != MusicCueId.None)
        {
            return false;
        }

        AudioSource primarySource = bgmSources[0];
        if (primarySource == null || !primarySource.isPlaying || primarySource.clip != targetClip)
        {
            return false;
        }

        bgmSourceCues[0] = targetCue;
        bgmSourceBlendWeights[0] = 1f;
        bgmSourceCues[1] = MusicCueId.None;
        bgmSourceBlendWeights[1] = 0f;
        activeBgmSourceIndex = 0;
        currentMusicCue = targetCue;
        UpdateAllBgmVolumes();
        return true;
    }

    private void PlayCueImmediate(MusicCueId targetCue, AudioClip targetClip)
    {
        int inactiveIndex = 1 - activeBgmSourceIndex;
        AudioSource activeSource = bgmSources[activeBgmSourceIndex];
        AudioSource inactiveSource = bgmSources[inactiveIndex];

        if (inactiveSource != null)
        {
            inactiveSource.Stop();
            inactiveSource.clip = null;
            bgmSourceCues[inactiveIndex] = MusicCueId.None;
            bgmSourceBlendWeights[inactiveIndex] = 0f;
            UpdateBgmVolume(inactiveIndex);
        }

        if (activeSource != null)
        {
            if (activeSource.isPlaying && activeSource.clip != targetClip)
            {
                activeSource.Stop();
            }

            activeSource.clip = targetClip;
            bgmSourceCues[activeBgmSourceIndex] = targetCue;
            bgmSourceBlendWeights[activeBgmSourceIndex] = 1f;

            if (!activeSource.isPlaying)
            {
                activeSource.Play();
            }

            currentMusicCue = targetCue;
            UpdateBgmVolume(activeBgmSourceIndex);
        }
    }

    private IEnumerator CrossfadeRoutine(MusicCueId targetCue, AudioClip targetClip, float duration)
    {
        int fromIndex = activeBgmSourceIndex;
        int toIndex = 1 - activeBgmSourceIndex;
        AudioSource fromSource = bgmSources[fromIndex];
        AudioSource toSource = bgmSources[toIndex];

        if (toSource == null)
        {
            PlayCueImmediate(targetCue, targetClip);
            yield break;
        }

        toSource.clip = targetClip;
        toSource.loop = true;
        bgmSourceCues[toIndex] = targetCue;
        bgmSourceBlendWeights[toIndex] = 0f;
        UpdateBgmVolume(toIndex);

        if (!toSource.isPlaying)
        {
            toSource.Play();
        }

        MusicCueId fromCue = bgmSourceCues[fromIndex];
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            bgmSourceBlendWeights[fromIndex] = 1f - t;
            bgmSourceBlendWeights[toIndex] = t;
            UpdateBgmVolume(fromIndex);
            UpdateBgmVolume(toIndex);
            yield return null;
        }

        bgmSourceBlendWeights[fromIndex] = 0f;
        bgmSourceBlendWeights[toIndex] = 1f;
        UpdateBgmVolume(fromIndex);
        UpdateBgmVolume(toIndex);

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.clip = null;
        }

        bgmSourceCues[fromIndex] = MusicCueId.None;
        bgmSourceBlendWeights[fromIndex] = 0f;
        UpdateBgmVolume(fromIndex);

        activeBgmSourceIndex = toIndex;
        currentMusicCue = targetCue;
        bgmTransitionCoroutine = null;

        if (fromCue == targetCue)
        {
            UpdateAllBgmVolumes();
        }
    }

    private void PlaySfxInternal(SfxCueId cueId)
    {
        EnsureAudioSources();
        LoadAudioCatalog();

        if (sfxSource == null || audioCatalog == null)
        {
            return;
        }

        AudioClip clip = audioCatalog.GetSfxClip(cueId);
        if (clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (lastSfxPlayTimes.TryGetValue(cueId, out float lastPlayedAt) &&
            now - lastPlayedAt < SfxMinRetriggerInterval)
        {
            return;
        }

        lastSfxPlayTimes[cueId] = now;
        sfxSource.pitch = Random.Range(SfxPitchMin, SfxPitchMax);
        sfxSource.PlayOneShot(clip, audioCatalog.GetSfxGain(cueId));
    }

    private void SetGameplayMusicPausedInternal(bool paused)
    {
        EnsureAudioSources();
        if (gameplayMusicPaused == paused)
        {
            return;
        }

        gameplayMusicPaused = paused;
        for (int i = 0; i < bgmSources.Length; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null)
            {
                continue;
            }

            if (paused)
            {
                if (source.isPlaying)
                {
                    source.Pause();
                }
            }
            else if (source.clip != null && bgmSourceCues[i] != MusicCueId.None)
            {
                source.UnPause();
            }
        }
    }

    private void LoadAudioCatalog()
    {
        if (audioCatalog != null)
        {
            return;
        }

        audioCatalog = Resources.Load<ArcitectureAudioCatalog>(AudioCatalogResourcePath);
        if (audioCatalog == null)
        {
            Debug.LogWarning($"MusicManager 未能加载音频目录资源：Resources/{AudioCatalogResourcePath}");
        }
    }

    private void EnsureAudioSources()
    {
        AudioSource primarySource = GetComponent<AudioSource>();
        if (primarySource == null)
        {
            primarySource = gameObject.AddComponent<AudioSource>();
        }

        bgmSources[0] = primarySource;
        ConfigureBgmSource(bgmSources[0]);
        bgmSources[1] = EnsureChildSource(SecondaryBgmSourceName);
        ConfigureBgmSource(bgmSources[1]);
        sfxSource = EnsureChildSource(SfxSourceName);
        ConfigureSfxSource(sfxSource);
    }

    private AudioSource EnsureChildSource(string childName)
    {
        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName);
        childObject.transform.SetParent(transform, false);

        AudioSource childSource = childObject.GetComponent<AudioSource>();
        if (childSource == null)
        {
            childSource = childObject.AddComponent<AudioSource>();
        }

        return childSource;
    }

    private static void ConfigureBgmSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(source.volume);
    }

    private void ConfigureSfxSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = sfxVolume;
    }

    private void UpdateAllBgmVolumes()
    {
        UpdateBgmVolume(0);
        UpdateBgmVolume(1);
    }

    private void UpdateBgmVolume(int sourceIndex)
    {
        AudioSource source = bgmSources[sourceIndex];
        if (source == null)
        {
            return;
        }

        MusicCueId cueId = bgmSourceCues[sourceIndex];
        if (cueId == MusicCueId.None)
        {
            source.volume = 0f;
            return;
        }

        source.volume = bgmSourceBlendWeights[sourceIndex] * GetMusicGain(cueId) * musicVolume;
    }

    private MusicCueId ResolveDesiredCue()
    {
        if (currentSceneCue == MusicCueId.Stage && isCombatMusicActive)
        {
            return MusicCueId.Fight;
        }

        return currentSceneCue;
    }

    private MusicCueId ResolveSceneCue(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return MusicCueId.None;
        }

        if (sceneName == "MainScene")
        {
            return MusicCueId.StartScreen;
        }

        if (sceneName == "BaseScene" || sceneName == "NewBase")
        {
            return MusicCueId.SafeHouse;
        }

        if (GameplayStageCatalog.IsGameplayScene(sceneName) || sceneName == "DeadScene")
        {
            return MusicCueId.Stage;
        }

        return MusicCueId.None;
    }

    private AudioClip GetMusicClip(MusicCueId cueId)
    {
        return audioCatalog != null ? audioCatalog.GetMusicClip(cueId) : null;
    }

    private float GetMusicGain(MusicCueId cueId)
    {
        return audioCatalog != null ? audioCatalog.GetMusicGain(cueId) : 1f;
    }
}
