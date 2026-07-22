using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game Over screen button controls
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private const string BaseSceneName = "NewBase";

    [Header("按钮")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("场景名")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainScene";

    [Header("淡入设置")]
    [SerializeField] private float pageFadeDelay = 0.42f;
    [SerializeField] private float pageFadeDuration = 1.1f;
    [SerializeField] private float blackoutHoldDuration = 0.24f;
    [SerializeField] private float blackoutFadeDuration = 1.05f;

    private CanvasGroup rootCanvasGroup;
    private Canvas overlayCanvas;
    private CanvasGroup overlayCanvasGroup;
    private RuntimeSettingsPanel settingsPanel;

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        Time.timeScale = 1f;
        PreparePageFade();
        PrepareBlackOverlay();
        EnsureSettingsPanel();
        StartCoroutine(PlayEntranceSequence());
    }

    private void Update()
    {
        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        if (pauseKey == KeyCode.None || !Input.GetKeyDown(pauseKey))
        {
            return;
        }

        EnsureSettingsPanel();
        if (settingsPanel == null)
        {
            return;
        }

        if (settingsPanel.IsShown)
        {
            if (!settingsPanel.IsCapturingBinding)
            {
                settingsPanel.RequestContinueGame();
            }

            return;
        }

        settingsPanel.Show(SettingsPanelContext.BaseHub);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
            overlayCanvasGroup = null;
        }
    }

    private void EnsureSettingsPanel()
    {
        settingsPanel = RuntimeSettingsPanel.EnsureInstance();
    }

    public void RestartGame()
    {
        ResetRuntimeState();
        string targetSceneName = ResolveRestartSceneName();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(string.IsNullOrWhiteSpace(targetSceneName) ? gameSceneName : targetSceneName);
            return;
        }

        SceneManager.LoadScene(string.IsNullOrWhiteSpace(targetSceneName) ? gameSceneName : targetSceneName);
    }

    private static string ResolveRestartSceneName()
    {
        return BaseSceneName;
    }

    public void GoToMainMenu()
    {
        ResetRuntimeState();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(mainMenuSceneName);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void ResetRuntimeState()
    {
        RuntimeSessionResetService.ResetGameplayTransientState();
    }

    private void PreparePageFade()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
    }

    private void PrepareBlackOverlay()
    {
        GameObject canvasObject = new GameObject(
            "DeathPageOverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 11000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        overlayCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        GameObject imageObject = new GameObject(
            "DeathPageOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        imageRect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }

    private IEnumerator PlayEntranceSequence()
    {
        if (rootCanvasGroup == null)
        {
            yield break;
        }

        float pageDelay = Mathf.Max(0f, pageFadeDelay);
        float pageDuration = Mathf.Max(0.01f, pageFadeDuration);
        float blackoutHold = Mathf.Max(0f, blackoutHoldDuration);
        float blackoutFade = Mathf.Max(0.01f, blackoutFadeDuration);
        float totalDuration = Mathf.Max(pageDelay + pageDuration, blackoutHold + blackoutFade);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float pageT = Mathf.Clamp01((elapsed - pageDelay) / pageDuration);
            rootCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, pageT);

            if (overlayCanvasGroup != null)
            {
                float overlayT = Mathf.Clamp01((elapsed - blackoutHold) / blackoutFade);
                overlayCanvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, overlayT);
            }

            yield return null;
        }

        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
            overlayCanvasGroup = null;
        }
    }
}
