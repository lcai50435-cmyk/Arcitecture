using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ScreenAdaptationManager : MonoBehaviour
{
    private const float DefaultReferenceWidth = 1920f;
    private const float DefaultReferenceHeight = 1080f;
    private const float DesignAspect = DefaultReferenceWidth / DefaultReferenceHeight;
    private const float MinReferenceAxis = 64f;
    private const float DefaultCanvasMatch = 0.5f;

    private static ScreenAdaptationManager instance;

    private readonly Dictionary<int, float> baseOrthographicSizes = new Dictionary<int, float>();
    private Vector2Int lastScreenSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScreenAdaptationManager manager = EnsureInstance();
        manager.StartCoroutine(manager.RefreshAfterSceneLoad());
    }

    private static ScreenAdaptationManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        ScreenAdaptationManager existing = FindObjectOfType<ScreenAdaptationManager>();
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("ScreenAdaptationManager");
        instance = managerObject.AddComponent<ScreenAdaptationManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        lastScreenSize = GetCurrentScreenSize();
        RefreshAdaptation();
    }

    private void LateUpdate()
    {
        Vector2Int currentScreenSize = GetCurrentScreenSize();
        if (currentScreenSize == lastScreenSize)
        {
            return;
        }

        lastScreenSize = currentScreenSize;
        RefreshAdaptation();
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        RefreshAdaptation();
        yield return null;
        RefreshAdaptation();
    }

    public static void RefreshNow()
    {
        ScreenAdaptationManager manager = EnsureInstance();
        manager.lastScreenSize = GetCurrentScreenSize();
        manager.RefreshAdaptation();
    }

    public static string GetCurrentAppliedViewZoomLabel()
    {
        ScreenAdaptationManager manager = EnsureInstance();
        if (manager.TryGetCurrentAppliedViewZoomMultiplier(out float multiplier))
        {
            return $"{Mathf.RoundToInt(multiplier * 100f)}%";
        }

        return null;
    }

    public static bool TryGetAdaptedOrthographicSize(Camera camera, out float size)
    {
        ScreenAdaptationManager manager = EnsureInstance();
        return manager.TryResolveAdaptedOrthographicSize(camera, out size);
    }

    public static void RegisterBaseOrthographicSize(Camera camera, float baseSize, bool applyCurrentScale = false)
    {
        ScreenAdaptationManager manager = EnsureInstance();
        manager.RegisterCameraBaseOrthographicSize(camera, baseSize, applyCurrentScale);
    }

    private void RefreshAdaptation()
    {
        ApplyCameraAdaptation();
        ApplyCanvasAdaptation();
    }

    private void ApplyCameraAdaptation()
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        float currentAspect = GetCurrentAspect();

        foreach (Camera camera in cameras)
        {
            if (camera == null || !camera.orthographic || !camera.CompareTag("MainCamera"))
            {
                continue;
            }

            int cameraId = camera.GetInstanceID();
            if (!baseOrthographicSizes.ContainsKey(cameraId))
            {
                baseOrthographicSizes[cameraId] = camera.orthographicSize;
            }

            float targetSize = ResolveAdaptedOrthographicSize(baseOrthographicSizes[cameraId], currentAspect);

            if (!ShouldDeferCameraSizeWrite(camera) && !Mathf.Approximately(camera.orthographicSize, targetSize))
            {
                camera.orthographicSize = targetSize;
            }

            camera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void ApplyCanvasAdaptation()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            Vector2 referenceResolution = scaler.referenceResolution;
            if (referenceResolution.x < MinReferenceAxis || referenceResolution.y < MinReferenceAxis)
            {
                referenceResolution = new Vector2(DefaultReferenceWidth, DefaultReferenceHeight);
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = DefaultCanvasMatch;
        }
    }

    private static Vector2Int GetCurrentScreenSize()
    {
        return new Vector2Int(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1));
    }

    private static float GetCurrentAspect()
    {
        return Mathf.Max(Screen.width, 1) / (float)Mathf.Max(Screen.height, 1);
    }

    private bool TryGetCurrentAppliedViewZoomMultiplier(out float multiplier)
    {
        Camera camera = ResolvePrimaryOrthographicCamera();
        if (camera == null)
        {
            multiplier = 0f;
            return false;
        }

        if (!baseOrthographicSizes.TryGetValue(camera.GetInstanceID(), out float baseSize) || baseSize <= 0.01f)
        {
            multiplier = 0f;
            return false;
        }

        float aspectCompensation = 1f;
        float currentAspect = GetCurrentAspect();
        if (currentAspect < DesignAspect)
        {
            aspectCompensation = DesignAspect / Mathf.Max(currentAspect, 0.01f);
        }

        multiplier = camera.orthographicSize / Mathf.Max(baseSize * aspectCompensation, 0.01f);
        return true;
    }

    private bool TryResolveAdaptedOrthographicSize(Camera camera, out float size)
    {
        if (camera == null || !camera.orthographic)
        {
            size = 0f;
            return false;
        }

        int cameraId = camera.GetInstanceID();
        if (!baseOrthographicSizes.TryGetValue(cameraId, out float baseSize) || baseSize <= 0.01f)
        {
            baseSize = camera.orthographicSize;
            if (baseSize <= 0.01f)
            {
                size = 0f;
                return false;
            }

            baseOrthographicSizes[cameraId] = baseSize;
        }

        size = ResolveAdaptedOrthographicSize(baseSize, GetCurrentAspect());
        return true;
    }

    private void RegisterCameraBaseOrthographicSize(Camera camera, float baseSize, bool applyCurrentScale)
    {
        if (camera == null || !camera.orthographic || baseSize <= 0.01f)
        {
            return;
        }

        int cameraId = camera.GetInstanceID();
        baseOrthographicSizes[cameraId] = baseSize;

        if (!applyCurrentScale || ShouldDeferCameraSizeWrite(camera))
        {
            return;
        }

        float targetSize = ResolveAdaptedOrthographicSize(baseSize, GetCurrentAspect());
        if (!Mathf.Approximately(camera.orthographicSize, targetSize))
        {
            camera.orthographicSize = targetSize;
        }
    }

    private static float ResolveAdaptedOrthographicSize(float baseSize, float currentAspect)
    {
        float targetSize = baseSize * GameSettingsStore.GetViewZoomMultiplier(GameSettingsStore.GetViewZoomIndex());
        if (currentAspect < DesignAspect)
        {
            targetSize *= DesignAspect / Mathf.Max(currentAspect, 0.01f);
        }

        return targetSize;
    }

    private static bool ShouldDeferCameraSizeWrite(Camera camera)
    {
        return camera != null
            && camera.CompareTag("MainCamera")
            && (GameplayStageIntroDirector.IsIntroActive || GameplayFailureController.IsFailureActive);
    }

    private static Camera ResolvePrimaryOrthographicCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled && mainCamera.orthographic)
        {
            return mainCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        Camera fallback = null;
        float highestDepth = float.MinValue;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.orthographic)
            {
                continue;
            }

            if (candidate.depth < highestDepth)
            {
                continue;
            }

            highestDepth = candidate.depth;
            fallback = candidate;
        }

        return fallback;
    }
}
