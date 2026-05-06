using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public sealed class WebGLCanvasResizeBridge : MonoBehaviour
{
    private const int RefreshFrameBudget = 8;
    private const string GameObjectName = "WebGLCanvasResizeBridge";

    private static WebGLCanvasResizeBridge instance;

    private Coroutine refreshCoroutine;
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance();
#endif
    }

    public static void RequestCanvasSync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance().RequestSync();
#endif
    }

    public static void SyncCanvasSizeNow()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance().SyncNativeCanvasSize();
#endif
    }

    public static void RestoreCanvasViewportNow()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureInstance().RestoreNativeCanvasViewport();
#endif
    }

    private static WebGLCanvasResizeBridge EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        WebGLCanvasResizeBridge existing = FindObjectOfType<WebGLCanvasResizeBridge>();
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject bridgeObject = new GameObject(GameObjectName);
        instance = bridgeObject.AddComponent<WebGLCanvasResizeBridge>();
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
        name = GameObjectName;
        DontDestroyOnLoad(gameObject);
        InstallNativeBridge();
    }

    private void Start()
    {
        RequestSync();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            RequestSync();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            RequestSync();
        }
    }

    public void HandleWebGLCanvasResize()
    {
        RequestSync();
    }

    private void RequestSync()
    {
        SyncNativeCanvasSize();

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        refreshCoroutine = StartCoroutine(RefreshAfterCanvasResize());
    }

    private IEnumerator RefreshAfterCanvasResize()
    {
        for (int frame = 0; frame < RefreshFrameBudget; frame++)
        {
            yield return null;
            SyncNativeCanvasSize();
            ScreenAdaptationManager.RefreshNow();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
        }

        ScreenAdaptationManager.RefreshNow();
        Canvas.ForceUpdateCanvases();
        refreshCoroutine = null;
    }

    private void InstallNativeBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (installed)
        {
            return;
        }

        installed = true;
        ArcitectureInstallCanvasResizeBridge(GameObjectName);
#endif
    }

    private void SyncNativeCanvasSize()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InstallNativeBridge();
        ArcitectureSyncCanvasSize();
#endif
    }

    private void RestoreNativeCanvasViewport()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InstallNativeBridge();
        ArcitectureRestoreCanvasViewport();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ArcitectureInstallCanvasResizeBridge(string gameObjectName);

    [DllImport("__Internal")]
    private static extern int ArcitectureSyncCanvasSize();

    [DllImport("__Internal")]
    private static extern int ArcitectureRestoreCanvasViewport();
#endif
}
