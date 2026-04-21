using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimePhotoCaptureManager : MonoBehaviour
{
    private const string CanvasName = "RuntimePhotoCaptureCanvas";
    private const string PauseReason = "RuntimePhotoCapture";
    private const int SortingOrder = 290;
    private const float ShutterDuration = 0.18f;
    private const float ShutterMaxHeight = 132f;
    private const float FlashPeakAlpha = 0.92f;
    private const float ConfirmIntroDuration = 0.16f;
    private const float ConfirmOutroDuration = 0.12f;
    private const float ToastDuration = 0.48f;
    private const float ToastFadeInDuration = 0.08f;
    private const float ToastFadeOutStart = 0.26f;

    private static readonly Color ShutterColor = new Color(0.02f, 0.02f, 0.02f, 0.96f);
    private static readonly Color ConfirmBackdropColor = new Color(0.02f, 0.03f, 0.04f, 0.82f);
    private static readonly Color ConfirmPanelColor = new Color(0.11f, 0.10f, 0.09f, 0.98f);
    private static readonly Color ConfirmPanelBorderColor = new Color(0.98f, 0.94f, 0.86f, 0.92f);
    private static readonly Color ConfirmPreviewFrameColor = new Color(0.06f, 0.06f, 0.07f, 1f);
    private static readonly Color ConfirmTitleColor = new Color(0.98f, 0.96f, 0.91f, 1f);
    private static readonly Color ConfirmHintColor = new Color(0.85f, 0.81f, 0.74f, 1f);
    private static readonly Color ConfirmMetaColor = new Color(0.74f, 0.78f, 0.82f, 1f);
    private static readonly Color SaveButtonColor = new Color(0.58f, 0.39f, 0.19f, 1f);
    private static readonly Color SaveButtonTextColor = new Color(0.98f, 0.97f, 0.94f, 1f);
    private static readonly Color CancelButtonColor = new Color(0.24f, 0.26f, 0.29f, 1f);
    private static readonly Color CancelButtonTextColor = new Color(0.90f, 0.92f, 0.95f, 1f);
    private static readonly Color ToastBackgroundColor = new Color(0.07f, 0.08f, 0.10f, 0.92f);
    private static readonly Color ToastSuccessBorderColor = new Color(0.88f, 0.72f, 0.42f, 0.92f);
    private static readonly Color ToastSuccessTextColor = new Color(0.98f, 0.96f, 0.90f, 1f);
    private static readonly Color ToastNeutralBorderColor = new Color(0.71f, 0.76f, 0.82f, 0.88f);
    private static readonly Color ToastNeutralTextColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color ToastErrorColor = new Color(0.96f, 0.54f, 0.49f, 1f);

    public static RuntimePhotoCaptureManager Instance { get; private set; }
    public static bool IsCaptureInProgress => Instance != null && Instance.captureInProgress;

    private Canvas canvas;
    private Image flashImage;
    private Image shutterTopImage;
    private Image shutterBottomImage;
    private RectTransform shutterTopRect;
    private RectTransform shutterBottomRect;
    private CanvasGroup confirmCanvasGroup;
    private RectTransform confirmPanelRect;
    private RawImage confirmPreviewImage;
    private AspectRatioFitter confirmPreviewFitter;
    private TextMeshProUGUI confirmTitleText;
    private TextMeshProUGUI confirmHintText;
    private TextMeshProUGUI confirmMetaText;
    private Button saveButton;
    private Button cancelButton;
    private Outline toastOutline;
    private CanvasGroup toastCanvasGroup;
    private TextMeshProUGUI toastText;
    private bool captureInProgress;
    private bool visible;
    private bool? pendingConfirmDecision;

    private GameObject cachedPlayerObject;
    private PlayerMove cachedPlayerMove;
    private PlayerAttack cachedPlayerAttack;
    private PlayerInteraction cachedPlayerInteraction;
    private Rigidbody2D cachedPlayerBody;
    private bool cachedPlayerStateValid;
    private bool wasMoveEnabled;
    private bool wasCanMove;
    private bool wasAttackEnabled;
    private bool wasInteractionEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RuntimePhotoCaptureManager manager = EnsureInstance();
        if (manager != null)
        {
            manager.PrepareForScene(scene.name);
        }
    }

    public static RuntimePhotoCaptureManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimePhotoCaptureManager existing = FindObjectOfType<RuntimePhotoCaptureManager>(true);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimePhotoCaptureManager");
        Instance = runtimeObject.AddComponent<RuntimePhotoCaptureManager>();
        return Instance;
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
        EnsureUi();
        PrepareForScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (!visible || captureInProgress)
        {
            return;
        }

        if (!ShouldAllowCapture())
        {
            return;
        }

        KeyCode photoKey = GameSettingsStore.GetKeyBinding(GameInputAction.PhotoCapture);
        if (photoKey == KeyCode.None || !Input.GetKeyDown(photoKey))
        {
            return;
        }

        StartCoroutine(CaptureRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void PrepareForScene(string sceneName)
    {
        captureInProgress = false;
        pendingConfirmDecision = null;
        visible = GameplayStageCatalog.IsGameplayScene(sceneName);
        ClearCachedPlayerReferences();
        ApplyVisibilityState();
        HideConfirmationImmediate();
        ResetOverlayState();
    }

    private bool ShouldAllowCapture()
    {
        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return false;
        }

        if (GameplayFailureController.IsFailureActive || GameplayStageIntroDirector.IsIntroActive)
        {
            return false;
        }

        if (RuntimePauseMenu.IsPauseOpen)
        {
            return false;
        }

        if (RuntimeSettingsPanel.Instance != null && RuntimeSettingsPanel.Instance.IsCapturingBinding)
        {
            return false;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return false;
        }

        return true;
    }

    private IEnumerator CaptureRoutine()
    {
        captureInProgress = true;
        Texture2D screenshot = null;
        bool pauseApplied = false;
        bool overlaysHidden = false;
        bool controlsLocked = false;

        try
        {
            LockPlayerControlsForCapture();
            controlsLocked = true;

            RuntimeGameplayPauseController.RequestPause(PauseReason);
            pauseApplied = true;

            SetGameplayOverlaysHidden(true);
            overlaysHidden = true;

            yield return new WaitForEndOfFrame();
            screenshot = ScreenCapture.CaptureScreenshotAsTexture();

            yield return PlayShutterRoutine();

            if (screenshot == null)
            {
                yield return PlayToastRoutine("留念拍摄失败", ToastErrorColor, ToastErrorColor);
                yield break;
            }

            bool shouldSave = false;
            yield return WaitForConfirmationRoutine(screenshot, result => shouldSave = result);

            if (!shouldSave)
            {
                yield return PlayToastRoutine("已取消本次留念", ToastNeutralTextColor, ToastNeutralBorderColor);
                yield break;
            }

            PhotoAlbumEntry savedEntry = SaveScreenshot(screenshot);
            if (savedEntry != null)
            {
                yield return PlayToastRoutine("留念已保存到本地相册", ToastSuccessTextColor, ToastSuccessBorderColor);
            }
            else
            {
                yield return PlayToastRoutine("留念保存失败", ToastErrorColor, ToastErrorColor);
            }
        }
        finally
        {
            HideConfirmationImmediate();

            if (screenshot != null)
            {
                Destroy(screenshot);
            }

            if (overlaysHidden)
            {
                SetGameplayOverlaysHidden(false);
            }

            if (controlsLocked)
            {
                RestorePlayerControlsAfterCapture();
            }

            if (pauseApplied)
            {
                RuntimeGameplayPauseController.ReleasePause(PauseReason);
            }

            captureInProgress = false;
        }
    }

    private PhotoAlbumEntry SaveScreenshot(Texture2D screenshot)
    {
        if (screenshot == null)
        {
            return null;
        }

        byte[] pngBytes;
        try
        {
            pngBytes = screenshot.EncodeToPNG();
        }
        catch (UnityException exception)
        {
            Debug.LogWarning($"截图编码失败：{exception.Message}");
            return null;
        }

        if (pngBytes == null || pngBytes.Length == 0)
        {
            return null;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        GameplayStageDefinition stageDefinition = GameplayStageCatalog.GetStageByScene(sceneName);
        string stageId = stageDefinition != null ? stageDefinition.stageId : GameplayStageRuntime.SelectedStageId;
        return PhotoAlbumRepository.SaveCapture(
            pngBytes,
            screenshot.width,
            screenshot.height,
            sceneName,
            stageId);
    }

    private IEnumerator PlayShutterRoutine()
    {
        EnsureUi();
        ApplyVisibilityState();
        ResetOverlayState();

        float elapsed = 0f;
        while (elapsed < ShutterDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ShutterDuration);
            float shutterProgress = progress < 0.45f
                ? progress / 0.45f
                : 1f - Mathf.InverseLerp(0.45f, 1f, progress);
            shutterProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shutterProgress));
            SetShutterAmount(shutterProgress);

            float flashAlpha = progress < 0.22f
                ? Mathf.Lerp(0f, FlashPeakAlpha, progress / 0.22f)
                : Mathf.Lerp(FlashPeakAlpha, 0f, Mathf.InverseLerp(0.22f, 1f, progress));
            flashImage.color = new Color(1f, 1f, 1f, flashAlpha);

            yield return null;
        }

        ResetOverlayState();
    }

    private IEnumerator WaitForConfirmationRoutine(Texture2D screenshot, Action<bool> onCompleted)
    {
        EnsureUi();
        ApplyVisibilityState();
        pendingConfirmDecision = null;

        if (confirmPreviewImage != null)
        {
            confirmPreviewImage.texture = screenshot;
            confirmPreviewImage.color = screenshot != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (confirmPreviewFitter != null)
        {
            confirmPreviewFitter.aspectRatio = screenshot != null && screenshot.height > 0
                ? screenshot.width / (float)screenshot.height
                : 1.6f;
        }

        if (confirmTitleText != null)
        {
            confirmTitleText.text = "确认保存这张留念吗？";
        }

        if (confirmHintText != null)
        {
            confirmHintText.text = "点击保存写入本地相册，取消则丢弃这次拍摄。";
        }

        if (confirmMetaText != null)
        {
            confirmMetaText.text = BuildConfirmationMeta(screenshot);
        }

        SetConfirmationVisible(true);
        confirmCanvasGroup.alpha = 0f;
        confirmPanelRect.localScale = Vector3.one * 1.08f;

        float elapsed = 0f;
        while (elapsed < ConfirmIntroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ConfirmIntroDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            confirmCanvasGroup.alpha = eased;
            confirmPanelRect.localScale = Vector3.LerpUnclamped(Vector3.one * 1.08f, Vector3.one, eased);
            yield return null;
        }

        confirmCanvasGroup.alpha = 1f;
        confirmPanelRect.localScale = Vector3.one;
        confirmCanvasGroup.interactable = true;
        confirmCanvasGroup.blocksRaycasts = true;

        while (!pendingConfirmDecision.HasValue)
        {
            yield return null;
        }

        bool decision = pendingConfirmDecision.Value;
        confirmCanvasGroup.interactable = false;
        confirmCanvasGroup.blocksRaycasts = false;

        elapsed = 0f;
        while (elapsed < ConfirmOutroDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ConfirmOutroDuration);
            confirmCanvasGroup.alpha = 1f - progress;
            confirmPanelRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.96f, progress);
            yield return null;
        }

        HideConfirmationImmediate();
        onCompleted?.Invoke(decision);
    }

    private IEnumerator PlayToastRoutine(string message, Color textColor, Color borderColor)
    {
        EnsureUi();
        ApplyVisibilityState();

        toastText.text = message;
        toastText.color = textColor;
        toastOutline.effectColor = borderColor;

        float elapsed = 0f;
        while (elapsed < ToastDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ToastDuration);
            if (progress <= ToastFadeInDuration / ToastDuration)
            {
                toastCanvasGroup.alpha = Mathf.InverseLerp(0f, ToastFadeInDuration / ToastDuration, progress);
            }
            else if (progress >= ToastFadeOutStart / ToastDuration)
            {
                toastCanvasGroup.alpha = 1f - Mathf.InverseLerp(ToastFadeOutStart / ToastDuration, 1f, progress);
            }
            else
            {
                toastCanvasGroup.alpha = 1f;
            }

            yield return null;
        }

        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
        }
    }

    private void LockPlayerControlsForCapture()
    {
        ResolvePlayerReferences();
        cachedPlayerStateValid = true;

        if (cachedPlayerInteraction != null)
        {
            wasInteractionEnabled = cachedPlayerInteraction.enabled;
            cachedPlayerInteraction.ClearCurrentInteractable();
            cachedPlayerInteraction.SetInteractUiSuppressed(true);
            cachedPlayerInteraction.enabled = false;
        }

        if (cachedPlayerMove != null)
        {
            wasMoveEnabled = cachedPlayerMove.enabled;
            wasCanMove = cachedPlayerMove.canMove;
            cachedPlayerMove.canMove = false;
            cachedPlayerMove.enabled = false;
        }

        if (cachedPlayerAttack != null)
        {
            wasAttackEnabled = cachedPlayerAttack.enabled;
            cachedPlayerAttack.enabled = false;
        }

        if (cachedPlayerBody != null)
        {
            cachedPlayerBody.velocity = Vector2.zero;
        }
    }

    private void RestorePlayerControlsAfterCapture()
    {
        if (!cachedPlayerStateValid)
        {
            return;
        }

        ResolvePlayerReferences();

        if (cachedPlayerMove != null)
        {
            cachedPlayerMove.enabled = wasMoveEnabled;
            cachedPlayerMove.canMove = wasCanMove;
        }

        if (cachedPlayerAttack != null)
        {
            cachedPlayerAttack.enabled = wasAttackEnabled;
        }

        if (cachedPlayerInteraction != null)
        {
            cachedPlayerInteraction.SetInteractUiSuppressed(false);
            cachedPlayerInteraction.enabled = wasInteractionEnabled;
        }

        cachedPlayerStateValid = false;
    }

    private void SetGameplayOverlaysHidden(bool hidden)
    {
        GameplayStatusHudRuntime.SetVisible(!hidden);
        RuntimeMiniMapHud.SetExternallyHidden(hidden);

        if (UIRootManager.Instance != null)
        {
            if (hidden)
            {
                UIRootManager.Instance.HideBackpack(true);
            }
            else
            {
                UIRootManager.Instance.ShowBackpack(true);
            }
        }

        ResolvePlayerReferences();
        if (cachedPlayerInteraction != null)
        {
            cachedPlayerInteraction.SetInteractUiSuppressed(hidden);
        }
    }

    private void ResolvePlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == cachedPlayerObject)
        {
            return;
        }

        cachedPlayerObject = playerObject;
        cachedPlayerMove = playerObject != null ? playerObject.GetComponent<PlayerMove>() : null;
        cachedPlayerAttack = playerObject != null ? playerObject.GetComponent<PlayerAttack>() : null;
        cachedPlayerInteraction = playerObject != null ? playerObject.GetComponent<PlayerInteraction>() : null;
        cachedPlayerBody = playerObject != null ? playerObject.GetComponent<Rigidbody2D>() : null;
    }

    private void ClearCachedPlayerReferences()
    {
        cachedPlayerObject = null;
        cachedPlayerMove = null;
        cachedPlayerAttack = null;
        cachedPlayerInteraction = null;
        cachedPlayerBody = null;
        cachedPlayerStateValid = false;
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        flashImage = CreateImage("Flash", canvasObject.transform, Color.clear, false);
        StretchRect(flashImage.rectTransform);

        shutterTopImage = CreateImage("ShutterTop", canvasObject.transform, ShutterColor, false);
        shutterTopRect = shutterTopImage.rectTransform;
        shutterTopRect.anchorMin = new Vector2(0f, 1f);
        shutterTopRect.anchorMax = new Vector2(1f, 1f);
        shutterTopRect.pivot = new Vector2(0.5f, 1f);
        shutterTopRect.sizeDelta = Vector2.zero;
        shutterTopRect.anchoredPosition = Vector2.zero;

        shutterBottomImage = CreateImage("ShutterBottom", canvasObject.transform, ShutterColor, false);
        shutterBottomRect = shutterBottomImage.rectTransform;
        shutterBottomRect.anchorMin = new Vector2(0f, 0f);
        shutterBottomRect.anchorMax = new Vector2(1f, 0f);
        shutterBottomRect.pivot = new Vector2(0.5f, 0f);
        shutterBottomRect.sizeDelta = Vector2.zero;
        shutterBottomRect.anchoredPosition = Vector2.zero;

        GameObject confirmRoot = new GameObject("ConfirmRoot", typeof(RectTransform), typeof(CanvasGroup));
        confirmRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform confirmRootRect = confirmRoot.GetComponent<RectTransform>();
        StretchRect(confirmRootRect);
        confirmCanvasGroup = confirmRoot.GetComponent<CanvasGroup>();
        confirmCanvasGroup.alpha = 0f;
        confirmCanvasGroup.interactable = false;
        confirmCanvasGroup.blocksRaycasts = false;

        Image confirmBackdrop = CreateImage("Backdrop", confirmRoot.transform, ConfirmBackdropColor, true);
        StretchRect(confirmBackdrop.rectTransform);

        GameObject confirmPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        confirmPanel.transform.SetParent(confirmRoot.transform, false);
        confirmPanelRect = confirmPanel.GetComponent<RectTransform>();
        confirmPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmPanelRect.pivot = new Vector2(0.5f, 0.5f);
        confirmPanelRect.sizeDelta = new Vector2(1080f, 720f);
        Image confirmPanelImage = confirmPanel.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(confirmPanelImage, ConfirmPanelColor, 22, 18, 1.2f);
        Outline confirmPanelOutline = confirmPanel.GetComponent<Outline>();
        confirmPanelOutline.effectColor = ConfirmPanelBorderColor;
        confirmPanelOutline.effectDistance = new Vector2(1f, -1f);

        confirmTitleText = CreateText(
            "Title",
            confirmPanel.transform,
            string.Empty,
            36f,
            ConfirmTitleColor,
            TextAlignmentOptions.Center);
        SetCenteredRect(confirmTitleText.rectTransform, new Vector2(0f, 298f), new Vector2(840f, 54f));

        confirmHintText = CreateText(
            "Hint",
            confirmPanel.transform,
            string.Empty,
            21f,
            ConfirmHintColor,
            TextAlignmentOptions.Center);
        SetCenteredRect(confirmHintText.rectTransform, new Vector2(0f, 258f), new Vector2(820f, 32f));

        GameObject previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image));
        previewFrame.transform.SetParent(confirmPanel.transform, false);
        RectTransform previewFrameRect = previewFrame.GetComponent<RectTransform>();
        previewFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewFrameRect.pivot = new Vector2(0.5f, 0.5f);
        previewFrameRect.sizeDelta = new Vector2(840f, 440f);
        previewFrameRect.anchoredPosition = new Vector2(0f, 26f);
        Image previewFrameImage = previewFrame.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(previewFrameImage, ConfirmPreviewFrameColor, 20, 16, 1.2f);

        GameObject previewViewport = new GameObject("PreviewViewport", typeof(RectTransform), typeof(RectMask2D));
        previewViewport.transform.SetParent(previewFrame.transform, false);
        RectTransform previewViewportRect = previewViewport.GetComponent<RectTransform>();
        SetStretch(previewViewportRect, 20f, 20f, 20f, 20f);

        GameObject previewImageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        previewImageObject.transform.SetParent(previewViewport.transform, false);
        confirmPreviewImage = previewImageObject.GetComponent<RawImage>();
        confirmPreviewImage.color = new Color(1f, 1f, 1f, 0f);
        confirmPreviewFitter = previewImageObject.GetComponent<AspectRatioFitter>();
        confirmPreviewFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        confirmPreviewFitter.aspectRatio = 1.6f;
        StretchRect(confirmPreviewImage.rectTransform);

        confirmMetaText = CreateText(
            "Meta",
            confirmPanel.transform,
            string.Empty,
            20f,
            ConfirmMetaColor,
            TextAlignmentOptions.Center);
        SetCenteredRect(confirmMetaText.rectTransform, new Vector2(0f, -248f), new Vector2(820f, 76f));

        saveButton = CreateButton(
            "SaveButton",
            confirmPanel.transform,
            "保存留念",
            SaveButtonColor,
            SaveButtonTextColor,
            new Vector2(220f, 58f));
        saveButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(132f, -312f);
        saveButton.onClick.AddListener(ConfirmSave);

        cancelButton = CreateButton(
            "CancelButton",
            confirmPanel.transform,
            "取消",
            CancelButtonColor,
            CancelButtonTextColor,
            new Vector2(180f, 58f));
        cancelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120f, -312f);
        cancelButton.onClick.AddListener(CancelSave);

        GameObject toastRoot = new GameObject("Toast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Outline));
        toastRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 0.14f);
        toastRect.anchorMax = new Vector2(0.5f, 0.14f);
        toastRect.pivot = new Vector2(0.5f, 0.5f);
        toastRect.sizeDelta = new Vector2(420f, 74f);

        Image toastBackgroundImage = toastRoot.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(toastBackgroundImage, ToastBackgroundColor, 18, 16);
        toastBackgroundImage.raycastTarget = false;

        toastCanvasGroup = toastRoot.GetComponent<CanvasGroup>();
        toastCanvasGroup.interactable = false;
        toastCanvasGroup.blocksRaycasts = false;

        toastOutline = toastRoot.GetComponent<Outline>();
        toastOutline.effectDistance = new Vector2(1f, -1f);

        GameObject toastTextObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastTextObject.transform.SetParent(toastRoot.transform, false);
        toastText = toastTextObject.GetComponent<TextMeshProUGUI>();
        toastText.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        toastText.fontSize = 28f;
        toastText.alignment = TextAlignmentOptions.Center;
        toastText.enableWordWrapping = false;
        toastText.raycastTarget = false;
        StretchRect(toastText.rectTransform);

        ResetOverlayState();
        HideConfirmationImmediate();
    }

    private void ApplyVisibilityState()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
    }

    private void ResetOverlayState()
    {
        if (flashImage != null)
        {
            flashImage.color = new Color(1f, 1f, 1f, 0f);
        }

        SetShutterAmount(0f);

        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
        }
    }

    private void HideConfirmationImmediate()
    {
        pendingConfirmDecision = null;

        if (confirmPreviewImage != null)
        {
            confirmPreviewImage.texture = null;
            confirmPreviewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (confirmCanvasGroup != null)
        {
            confirmCanvasGroup.alpha = 0f;
            confirmCanvasGroup.interactable = false;
            confirmCanvasGroup.blocksRaycasts = false;
            if (confirmCanvasGroup.gameObject.activeSelf)
            {
                confirmCanvasGroup.gameObject.SetActive(false);
            }
        }

        if (confirmPanelRect != null)
        {
            confirmPanelRect.localScale = Vector3.one;
        }
    }

    private void SetConfirmationVisible(bool shouldShow)
    {
        if (confirmCanvasGroup == null)
        {
            return;
        }

        confirmCanvasGroup.gameObject.SetActive(shouldShow);
    }

    private void SetShutterAmount(float amount)
    {
        float height = Mathf.Lerp(0f, ShutterMaxHeight, Mathf.Clamp01(amount));
        if (shutterTopRect != null)
        {
            shutterTopRect.sizeDelta = new Vector2(0f, height);
        }

        if (shutterBottomRect != null)
        {
            shutterBottomRect.sizeDelta = new Vector2(0f, height);
        }

        float alpha = Mathf.Lerp(0f, ShutterColor.a, Mathf.Clamp01(amount));
        if (shutterTopImage != null)
        {
            shutterTopImage.color = new Color(ShutterColor.r, ShutterColor.g, ShutterColor.b, alpha);
        }

        if (shutterBottomImage != null)
        {
            shutterBottomImage.color = new Color(ShutterColor.r, ShutterColor.g, ShutterColor.b, alpha);
        }
    }

    private void ConfirmSave()
    {
        pendingConfirmDecision = true;
    }

    private void CancelSave()
    {
        pendingConfirmDecision = false;
    }

    private string BuildConfirmationMeta(Texture2D screenshot)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        GameplayStageDefinition stageDefinition = GameplayStageCatalog.GetStageByScene(sceneName);
        string stageLabel = stageDefinition != null
            ? stageDefinition.displayName
            : (string.IsNullOrWhiteSpace(GameplayStageRuntime.SelectedStageId)
                ? "未记录"
                : GameplayStageRuntime.SelectedStageId);
        string resolutionText = screenshot != null
            ? $"{screenshot.width} x {screenshot.height}"
            : "未知";
        return $"关卡：{stageLabel}    场景：{sceneName}\n分辨率：{resolutionText}";
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, backgroundColor, 16, 14, 1.2f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 24f, textColor, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform);
        text.raycastTarget = false;

        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetStretch(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }
}
