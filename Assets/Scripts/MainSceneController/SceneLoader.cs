using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private int mainMenuIndex = 0;
    [SerializeField] private int baseSceneIndex = 1;
    [SerializeField] private int gameSceneIndex = 2;
    [SerializeField] private string mainMenuSceneName = "MainScene";
    [SerializeField] private string baseSceneName = "BaseScene";
    [SerializeField] private string gameSceneName = "GameScene";

    public static SceneLoader Instance;

    private Canvas overlayCanvas;
    private CanvasGroup overlayGroup;
    private Image overlayImage;
    private bool isBusy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static SceneLoader EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SceneLoader existing = FindObjectOfType<SceneLoader>();
        if (existing != null)
        {
            return existing;
        }

        GameObject loaderObject = new GameObject("SceneLoader");
        return loaderObject.AddComponent<SceneLoader>();
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
        EnsureOverlay();
        SetOverlayAlpha(0f, false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ToBase() => SwitchScene(baseSceneName, baseSceneIndex);
    public void ToGame() => SwitchScene(gameSceneName, gameSceneIndex);
    public void ToMenu() => SwitchScene(mainMenuSceneName, mainMenuIndex);
    public void ToScene(string sceneName) => SwitchScene(sceneName, -1);

    private void SwitchScene(string sceneName, int fallbackIndex)
    {
        if (isBusy)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(sceneName) && activeScene.name == sceneName)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) && fallbackIndex >= 0 && activeScene.buildIndex == fallbackIndex)
        {
            return;
        }

        StartCoroutine(DoTransition(sceneName, fallbackIndex));
    }

    private IEnumerator DoTransition(string sceneName, int fallbackIndex)
    {
        isBusy = true;
        EnsureOverlay();

        yield return FadeOverlay(1f);

        AsyncOperation loadOperation = CreateLoadOperation(sceneName, fallbackIndex);
        if (loadOperation == null)
        {
            isBusy = false;
            yield return FadeOverlay(0f);
            yield break;
        }

        yield return loadOperation;
        yield return null;

        EnsureOverlay();
        yield return FadeOverlay(0f);

        isBusy = false;
    }

    private AsyncOperation CreateLoadOperation(string sceneName, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            return SceneManager.LoadSceneAsync(sceneName);
        }

        if (fallbackIndex >= 0)
        {
            return SceneManager.LoadSceneAsync(fallbackIndex);
        }

        Debug.LogError("SceneLoader 缺少可用的场景目标。");
        return null;
    }

    private void EnsureOverlay()
    {
        if (overlayCanvas != null && overlayGroup != null && overlayImage != null)
        {
            return;
        }

        Transform existingCanvas = transform.Find("FadeOverlayCanvas");
        if (existingCanvas != null)
        {
            overlayCanvas = existingCanvas.GetComponent<Canvas>();
            overlayGroup = existingCanvas.GetComponent<CanvasGroup>();
            overlayImage = existingCanvas.GetComponentInChildren<Image>(true);
            if (overlayCanvas != null && overlayGroup != null && overlayImage != null)
            {
                return;
            }
        }

        GameObject canvasObject = existingCanvas != null ? existingCanvas.gameObject : new GameObject("FadeOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = canvasObject.AddComponent<Canvas>();
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        }

        overlayGroup = canvasObject.GetComponent<CanvasGroup>();
        if (overlayGroup == null)
        {
            overlayGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        Transform imageTransform = canvasObject.transform.Find("FadeOverlay");
        GameObject imageObject = imageTransform != null ? imageTransform.gameObject : new GameObject("FadeOverlay");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        if (imageRect == null)
        {
            imageRect = imageObject.AddComponent<RectTransform>();
        }

        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.localScale = Vector3.one;

        overlayImage = imageObject.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = imageObject.AddComponent<Image>();
        }

        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;
    }

    private IEnumerator FadeOverlay(float targetAlpha)
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float startAlpha = overlayGroup != null ? overlayGroup.alpha : 0f;
        float elapsed = 0f;

        SetOverlayAlpha(startAlpha, true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetOverlayAlpha(alpha, true);
            yield return null;
        }

        SetOverlayAlpha(targetAlpha, targetAlpha > 0.001f);
    }

    private void SetOverlayAlpha(float alpha, bool blockRaycasts)
    {
        if (overlayGroup == null)
        {
            return;
        }

        overlayGroup.alpha = Mathf.Clamp01(alpha);
        overlayGroup.interactable = blockRaycasts;
        overlayGroup.blocksRaycasts = blockRaycasts;
    }
}
