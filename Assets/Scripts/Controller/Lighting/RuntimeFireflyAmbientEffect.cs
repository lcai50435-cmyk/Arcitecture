using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RuntimeFireflyAmbientEffect : MonoBehaviour
{
    private const string EffectObjectName = "RuntimeFireflyAmbientEffect";
    private const string BaseSceneName = "NewBase";
    private const string LegacyBaseSceneName = "BaseScene";
    private const string FirstStageId = "stage_01";
    private const string SecondStageId = "stage_02";
    private const int FireflySortingOrder = 28150;
    private const float ShapeWidthMultiplier = 1.15f;
    private const float ShapeHeightMultiplier = 1.05f;

    private static readonly FireflyAmbientProfile BaseProfile = new FireflyAmbientProfile(
        72,
        10f,
        4.8f,
        7.6f,
        0.035f,
        0.085f,
        0.05f,
        0.18f,
        0.035f,
        0.13f,
        0.42f,
        0.18f,
        new Color(1f, 0.86f, 0.42f, 0.72f),
        new Color(0.66f, 0.96f, 1f, 0.52f));

    private static readonly FireflyAmbientProfile FirstStageGameplayProfile = new FireflyAmbientProfile(
        20,
        3f,
        4.8f,
        7.2f,
        0.045f,
        0.14f,
        0.06f,
        0.18f,
        0.032f,
        0.11f,
        0.28f,
        0.18f,
        new Color(1f, 0.92f, 0.62f, 0.38f),
        new Color(0.82f, 0.99f, 1f, 0.28f));

    private static readonly FireflyAmbientProfile GameplayProfile = new FireflyAmbientProfile(
        240,
        36f,
        5.2f,
        8.0f,
        0.12f,
        0.32f,
        0.07f,
        0.22f,
        0.045f,
        0.16f,
        0.36f,
        0.26f,
        new Color(1f, 0.84f, 0.34f, 1f),
        new Color(0.64f, 0.96f, 1f, 0.82f));

    private static readonly FireflyAmbientProfile SecondStageGameplayProfile = new FireflyAmbientProfile(
        72,
        9f,
        5.2f,
        8.0f,
        0.065f,
        0.17f,
        0.06f,
        0.18f,
        0.04f,
        0.14f,
        0.28f,
        0.18f,
        new Color(1f, 0.86f, 0.42f, 0.62f),
        new Color(0.70f, 0.96f, 1f, 0.46f));

    private static RuntimeFireflyAmbientEffect instance;
    private static bool sceneHookRegistered;

    private ParticleSystem fireflySystem;
    private ParticleSystemRenderer fireflyRenderer;
    private Material fireflyMaterial;
    private Camera activeCamera;
    private FireflyAmbientProfile activeProfile;
    private string activeSceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (sceneHookRegistered)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneHookRegistered = false;
        }

        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        RuntimeFireflyAmbientEffect effect = EnsureInstance();
        effect.ApplyScene(SceneManager.GetActiveScene().name);
    }

    private static RuntimeFireflyAmbientEffect EnsureInstance()
    {
        if (instance != null)
        {
            instance.EnsureSceneHook();
            return instance;
        }

        RuntimeFireflyAmbientEffect existing = FindObjectOfType<RuntimeFireflyAmbientEffect>(true);
        if (existing != null)
        {
            instance = existing;
            existing.EnsureSceneHook();
            return existing;
        }

        GameObject effectObject = new GameObject(EffectObjectName);
        instance = effectObject.AddComponent<RuntimeFireflyAmbientEffect>();
        instance.EnsureSceneHook();
        return instance;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RuntimeFireflyAmbientEffect effect = EnsureInstance();
        string sceneName = ResolveAmbientSceneName(scene.name, mode, SceneManager.GetActiveScene().name);
        effect.ApplyScene(sceneName);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = EffectObjectName;
        gameObject.layer = NightLightingLayers.VisualLayer;
        DontDestroyOnLoad(gameObject);
        EnsureSceneHook();
        EnsureParticleSystem();
        ApplyScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (fireflyMaterial != null)
        {
            Destroy(fireflyMaterial);
            fireflyMaterial = null;
        }
    }

    private void LateUpdate()
    {
        if (activeProfile == null)
        {
            StopAndClear();
            return;
        }

        ResolveCameraIfNeeded();
        if (activeCamera == null)
        {
            StopAndClear();
            return;
        }

        EnsureParticleSystem();
        EnsureCameraIncludesEffectLayer(activeCamera);
        UpdateEmitterBounds();

        if (!fireflySystem.isPlaying)
        {
            fireflySystem.Play(true);
        }
    }

    private void EnsureSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private void ApplyScene(string sceneName)
    {
        if (string.Equals(activeSceneName, sceneName, StringComparison.Ordinal) && activeProfile != null)
        {
            EnsureParticleSystem();
            ConfigureParticleSystem(activeProfile);
            return;
        }

        activeSceneName = sceneName;
        activeCamera = null;
        activeProfile = ResolveProfile(sceneName);

        EnsureParticleSystem();
        StopAndClear();

        if (activeProfile == null)
        {
            return;
        }

        ConfigureParticleSystem(activeProfile);
    }

    private static string ResolveAmbientSceneName(string loadedSceneName, LoadSceneMode mode, string activeSceneName)
    {
        if (mode == LoadSceneMode.Additive && ResolveProfile(activeSceneName) != null)
        {
            return activeSceneName;
        }

        return loadedSceneName;
    }

    private static FireflyAmbientProfile ResolveProfile(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        if (string.Equals(sceneName, BaseSceneName, StringComparison.Ordinal) ||
            string.Equals(sceneName, LegacyBaseSceneName, StringComparison.Ordinal))
        {
            return BaseProfile;
        }

        GameplayStageDefinition stage = GameplayStageCatalog.GetStageByScene(sceneName);
        if (stage == null)
        {
            return null;
        }

        if (string.Equals(stage.stageId, FirstStageId, StringComparison.Ordinal))
        {
            return FirstStageGameplayProfile;
        }

        return string.Equals(stage.stageId, SecondStageId, StringComparison.Ordinal)
            ? SecondStageGameplayProfile
            : GameplayProfile;
    }

    private void EnsureParticleSystem()
    {
        if (fireflySystem == null)
        {
            fireflySystem = GetComponent<ParticleSystem>();
            if (fireflySystem == null)
            {
                fireflySystem = gameObject.AddComponent<ParticleSystem>();
            }
        }

        if (fireflyRenderer == null)
        {
            fireflyRenderer = GetComponent<ParticleSystemRenderer>();
            if (fireflyRenderer == null)
            {
                fireflyRenderer = gameObject.AddComponent<ParticleSystemRenderer>();
            }
        }
    }

    private void ConfigureParticleSystem(FireflyAmbientProfile profile)
    {
        ParticleSystem.MainModule main = fireflySystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.prewarm = true;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = profile.maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(profile.minLifetime, profile.maxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(profile.minSpeed, profile.maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(profile.minSize, profile.maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(profile.warmColor, profile.coolColor);

        ParticleSystem.EmissionModule emission = fireflySystem.emission;
        emission.enabled = true;
        emission.rateOverTime = profile.emissionRate;

        ParticleSystem.ShapeModule shape = fireflySystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.randomDirectionAmount = 0.18f;

        ParticleSystem.VelocityOverLifetimeModule velocity = fireflySystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-profile.horizontalDrift, profile.horizontalDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(profile.minVerticalDrift, profile.maxVerticalDrift);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule noise = fireflySystem.noise;
        noise.enabled = true;
        noise.strength = profile.noiseStrength;
        noise.frequency = profile.noiseFrequency;
        noise.scrollSpeed = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = fireflySystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            CreateLifetimeGradient(profile.warmColor),
            CreateLifetimeGradient(profile.coolColor));

        ParticleSystem.LightsModule lights = fireflySystem.lights;
        lights.enabled = false;

        ParticleSystem.CollisionModule collision = fireflySystem.collision;
        collision.enabled = false;

        ParticleSystem.TrailModule trails = fireflySystem.trails;
        trails.enabled = false;

        fireflyRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        fireflyRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        fireflyRenderer.sortingOrder = FireflySortingOrder;
        fireflyRenderer.sharedMaterial = GetFireflyMaterial();
    }

    private Material GetFireflyMaterial()
    {
        if (fireflyMaterial != null)
        {
            return fireflyMaterial;
        }

        Material baseMaterial = NightLightingVisualFactory.GetAdditiveGlowMaterial();
        if (baseMaterial != null)
        {
            fireflyMaterial = new Material(baseMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            fireflyMaterial = shader != null ? new Material(shader) : null;
        }

        if (fireflyMaterial == null)
        {
            return null;
        }

        Sprite glowSprite = NightLightingVisualFactory.GetRadialGlowSprite();
        if (glowSprite != null)
        {
            fireflyMaterial.mainTexture = glowSprite.texture;
        }

        fireflyMaterial.name = "RuntimeFireflyAmbientMaterial";
        fireflyMaterial.hideFlags = HideFlags.HideAndDontSave;
        return fireflyMaterial;
    }

    private static Gradient CreateLifetimeGradient(Color color)
    {
        return new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(color.a, 0.18f),
                new GradientAlphaKey(color.a * 0.78f, 0.74f),
                new GradientAlphaKey(0f, 1f)
            }
        };
    }

    private void ResolveCameraIfNeeded()
    {
        if (activeCamera != null &&
            activeCamera.isActiveAndEnabled &&
            string.Equals(activeCamera.gameObject.scene.name, activeSceneName, StringComparison.Ordinal))
        {
            return;
        }

        activeCamera = Camera.main;
        if (activeCamera != null && activeCamera.isActiveAndEnabled)
        {
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate != null && candidate.isActiveAndEnabled)
            {
                activeCamera = candidate;
                return;
            }
        }
    }

    private void UpdateEmitterBounds()
    {
        float worldHeight = activeCamera.orthographic ? activeCamera.orthographicSize * 2f : 10f;
        float worldWidth = worldHeight * Mathf.Max(0.01f, activeCamera.aspect);

        transform.position = new Vector3(activeCamera.transform.position.x, activeCamera.transform.position.y, 0f);

        ParticleSystem.ShapeModule shape = fireflySystem.shape;
        shape.scale = new Vector3(worldWidth * ShapeWidthMultiplier, worldHeight * ShapeHeightMultiplier, 0.12f);
    }

    private void StopAndClear()
    {
        if (fireflySystem == null)
        {
            return;
        }

        fireflySystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void EnsureCameraIncludesEffectLayer(Camera camera)
    {
        int requiredMask = camera.cullingMask | NightLightingLayers.VisualLayerMask;
        if (camera.cullingMask != requiredMask)
        {
            camera.cullingMask = requiredMask;
        }
    }

    private sealed class FireflyAmbientProfile
    {
        public readonly int maxParticles;
        public readonly float emissionRate;
        public readonly float minLifetime;
        public readonly float maxLifetime;
        public readonly float minSize;
        public readonly float maxSize;
        public readonly float minSpeed;
        public readonly float maxSpeed;
        public readonly float minVerticalDrift;
        public readonly float maxVerticalDrift;
        public readonly float horizontalDrift;
        public readonly float noiseStrength;
        public readonly Color warmColor;
        public readonly Color coolColor;
        public readonly float noiseFrequency = 0.42f;

        public FireflyAmbientProfile(
            int maxParticles,
            float emissionRate,
            float minLifetime,
            float maxLifetime,
            float minSize,
            float maxSize,
            float minSpeed,
            float maxSpeed,
            float minVerticalDrift,
            float maxVerticalDrift,
            float horizontalDrift,
            float noiseStrength,
            Color warmColor,
            Color coolColor)
        {
            this.maxParticles = Mathf.Max(1, maxParticles);
            this.emissionRate = Mathf.Max(0f, emissionRate);
            this.minLifetime = Mathf.Max(0.1f, minLifetime);
            this.maxLifetime = Mathf.Max(this.minLifetime, maxLifetime);
            this.minSize = Mathf.Max(0.001f, minSize);
            this.maxSize = Mathf.Max(this.minSize, maxSize);
            this.minSpeed = Mathf.Max(0f, minSpeed);
            this.maxSpeed = Mathf.Max(this.minSpeed, maxSpeed);
            this.minVerticalDrift = minVerticalDrift;
            this.maxVerticalDrift = Mathf.Max(this.minVerticalDrift, maxVerticalDrift);
            this.horizontalDrift = Mathf.Max(0f, horizontalDrift);
            this.noiseStrength = Mathf.Max(0f, noiseStrength);
            this.warmColor = warmColor;
            this.coolColor = coolColor;
        }
    }
}
