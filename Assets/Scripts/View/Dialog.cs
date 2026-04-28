using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Dialog : MonoBehaviour
{
    private const string GameplayPauseReason = "RuntimeDialog";
    private const string RuntimeDialogObjectName = "RuntimeDialogController";
    private const string RuntimeDialogPanelName = "RuntimeDialogPanel";
    private const string RuntimeDialogPrefabPath = "Assets/Scripts/View/Prefab/CatagloueUI.prefab";
    private const int RuntimeDialogSortingOrder = 12000;
    private const float DefaultRevealDurationPerWeight = 0.03f;
    private const float MinimumRevealDuration = 0.2f;
    private const float MaximumRevealDuration = 1.8f;
    private const float TextFadeInDuration = 0.22f;
    private const float TextFloatDistance = 10f;
    private const float TextStartScaleFactor = 0.985f;
    private const float TextPopStrength = 0.018f;

    [Header("UI 组件")]
    public GameObject dialogPanel;
    public Text descriptionText;

    [Header("点击关闭按钮")]
    public Button clickCloseButton;

    [Header("需要隐藏的其他 UI")]
    public GameObject[] uiToHide;

    [Header("自动关闭时间")]
    public float displayDuration = 4f;

    [Header("是否允许普通弹窗显示")]
    public bool canShow = true;

    private BackpackMananger backpackManager;
    private Coroutine currentCoroutine;
    private bool waitingForClickClose;
    private bool isClosingDialog;
    private bool isRevealPlaying;
    private bool requestedGameplayPause;
    private bool subscribedToBackpack;
    private bool initialized;
    private Button backdropCloseButton;
    private string activeDialogContent = string.Empty;
    private RectTransform descriptionRectTransform;
    private Vector2 descriptionTextOrigin;
    private Vector3 descriptionTextScaleOrigin;
    private bool cachedDescriptionTransform;

    public static Dialog FindUsableInstance()
    {
        Dialog[] dialogs = FindObjectsOfType<Dialog>(true);
        for (int i = 0; i < dialogs.Length; i++)
        {
            Dialog dialog = dialogs[i];
            if (IsUsableDialog(dialog))
            {
                return dialog;
            }
        }

        return null;
    }

    private static bool IsUsableDialog(Dialog dialog)
    {
        if (dialog == null ||
            !dialog.gameObject.activeInHierarchy ||
            dialog.dialogPanel == null ||
            dialog.descriptionText == null)
        {
            return false;
        }

        Transform panelParent = dialog.dialogPanel.transform.parent;
        return panelParent == null || panelParent.gameObject.activeInHierarchy;
    }

    public static Dialog EnsureRuntimeInstance()
    {
        Dialog existing = FindUsableInstance();
        if (existing != null)
        {
            return existing;
        }

        return CreateRuntimeInstance();
    }

    public static Dialog EnsureGameplayRuntimeInstance()
    {
        Dialog[] dialogs = FindObjectsOfType<Dialog>(true);
        for (int i = 0; i < dialogs.Length; i++)
        {
            Dialog dialog = dialogs[i];
            if (dialog != null && dialog.IsRuntimeDialog())
            {
                return dialog;
            }
        }

        return CreateRuntimeInstance();
    }

    private void Start()
    {
        InitializeLifecycle(true);
    }

    private void OnDestroy()
    {
        UnsubscribeBackpack();

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.RemoveListener(OnClickCloseDialog);
        }

        if (backdropCloseButton != null)
        {
            backdropCloseButton.onClick.RemoveListener(OnClickCloseDialog);
        }
    }

    private void Update()
    {
        TrySubscribeBackpack();
    }

    private void TrySubscribeBackpack()
    {
        if (!ShouldSubscribeToBackpack())
        {
            UnsubscribeBackpack();
            return;
        }

        BackpackMananger currentBackpack = BackpackMananger.Instance;
        if (currentBackpack == null)
        {
            return;
        }

        if (subscribedToBackpack && backpackManager == currentBackpack)
        {
            return;
        }

        UnsubscribeBackpack();
        backpackManager = currentBackpack;
        backpackManager.OnFirstTimePickItemType += ShowDialogByCrystal;
        subscribedToBackpack = true;
    }

    private void UnsubscribeBackpack()
    {
        if (subscribedToBackpack && backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType -= ShowDialogByCrystal;
        }

        backpackManager = null;
        subscribedToBackpack = false;
    }

    private void ShowDialogByCrystal(ArchitecturalCrystal crystal)
    {
        if (!ShouldSubscribeToBackpack())
        {
            return;
        }

        if (crystal.isUnlockMaterial) return;

        string desc = BuildSpiritIntro(crystal);
        InternalShow(desc, false);
    }

    public void ShowAutoDialog(string desc)
    {
        if (!canShow) return;
        InternalShow(desc, true);
    }

    public void ShowAutoDialogForce(string desc)
    {
        InternalShow(desc, true);
    }

    public void ShowClickCloseDialog(string desc)
    {
        InternalShow(desc, false);
    }

    private bool InternalShow(string desc, bool autoClose)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Dialog 所在对象未激活，无法显示弹窗");
            return false;
        }

        activeDialogContent = desc ?? string.Empty;

        if (descriptionText != null)
        {
            PrepareDescriptionForReveal();
        }

        HideOtherUI(true);
        isClosingDialog = false;
        PauseGameForFirstPickDialog();

        if (IsRuntimeDialog())
        {
            ShowRuntimeDialogPanelDirectly();
        }
        else if (UIRootManager.Instance != null)
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }

            UIRootManager.Instance.OpenModal(RuntimeModalType.Dialog, RuntimeModalOpenSource.None);
        }
        else if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = !autoClose;
        isRevealPlaying = descriptionText != null && activeDialogContent.Length > 0;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(waitingForClickClose);
        }

        currentCoroutine = StartCoroutine(PlayDialogSequence(autoClose));

        return true;
    }

    private IEnumerator PlayDialogSequence(bool autoClose)
    {
        yield return RevealDialogText();

        isRevealPlaying = false;

        if (!autoClose)
        {
            currentCoroutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(displayDuration);
        currentCoroutine = null;
        CloseDialog();
    }

    private void OnClickCloseDialog()
    {
        if (!waitingForClickClose) return;

        if (isRevealPlaying)
        {
            CompleteRevealImmediately();
            return;
        }

        CloseDialog();
    }

    public void CloseDialog()
    {
        if (isClosingDialog)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = true;

        if (UIRootManager.Instance != null && UIRootManager.Instance.ActiveModalType == RuntimeModalType.Dialog)
        {
            UIRootManager.Instance.CloseModalFlow(CompleteCloseDialog);
            return;
        }

        CompleteCloseDialog();
    }

    public void ForceHideImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = false;
        isRevealPlaying = false;
        activeDialogContent = string.Empty;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (descriptionText != null)
        {
            RestoreDescriptionPresentation(clearText: true);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideDialog();
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
    }

    private void CompleteCloseDialog()
    {
        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
        isClosingDialog = false;
        isRevealPlaying = false;
        RestoreDescriptionPresentation(clearText: true);
    }

    private void PauseGameForFirstPickDialog()
    {
        if (requestedGameplayPause || !GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        RuntimeGameplayPauseController.RequestPause(GameplayPauseReason);
        requestedGameplayPause = true;
    }

    private void ResumeGameAfterFirstPickDialog()
    {
        if (!requestedGameplayPause)
        {
            return;
        }

        RuntimeGameplayPauseController.ReleasePause(GameplayPauseReason);
        requestedGameplayPause = false;
    }

    private string BuildSpiritIntro(ArchitecturalCrystal crystal)
    {
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"发现了 {crystal.DisplayName}。它能推进建筑录修复，并立即提供当前结构效果。"
            : crystal.textDescription;

        return $"精灵：\n发现了 {crystal.DisplayName}。\n{desc}\n\n点击按钮后继续探索。";
    }

    private static Dialog CreateRuntimeInstance()
    {
#if UNITY_EDITOR
        Dialog prefabDialog = CreateRuntimeInstanceFromPrefab();
        if (prefabDialog != null)
        {
            return prefabDialog;
        }
#endif

        GameObject controllerObject = new GameObject(RuntimeDialogObjectName);
        Dialog dialog = controllerObject.AddComponent<Dialog>();

        GameObject panelObject = new GameObject(
            RuntimeDialogPanelName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(Button));
        panelObject.transform.SetParent(controllerObject.transform, false);
        StretchRect(panelObject.GetComponent<RectTransform>());

        Canvas canvas = panelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = RuntimeDialogSortingOrder;

        CanvasScaler scaler = panelObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image backdrop = panelObject.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.48f);

        Button backdropButton = panelObject.GetComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;

        GameObject cardObject = new GameObject("RuntimeDialogCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObject.transform.SetParent(panelObject.transform, false);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        SetRect(cardRect, Vector2.zero, new Vector2(780f, 292f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        Image cardImage = cardObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(cardImage, new Color(0.13f, 0.11f, 0.09f, 0.96f), 12, 12, 1.2f);

        GameObject textObject = new GameObject("Description", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(cardObject.transform, false);
        Text description = textObject.GetComponent<Text>();
        description.fontSize = 25;
        description.lineSpacing = 1.12f;
        description.color = new Color(0.96f, 0.91f, 0.82f, 1f);
        description.alignment = TextAnchor.UpperLeft;
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        description.verticalOverflow = VerticalWrapMode.Overflow;
        StretchRectWithOffsets(description.rectTransform, 44f, 38f, 44f, 92f);
        RuntimeTextFontRepair.RepairLegacyText(description);

        Button closeButton = CreateRuntimeButton(cardObject.transform);

        dialog.dialogPanel = panelObject;
        dialog.descriptionText = description;
        dialog.clickCloseButton = closeButton;
        dialog.backdropCloseButton = backdropButton;
        dialog.uiToHide = new GameObject[0];
        panelObject.SetActive(false);
        dialog.InitializeLifecycle(false);
        return dialog;
    }

#if UNITY_EDITOR
    private static Dialog CreateRuntimeInstanceFromPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeDialogPrefabPath);
        if (prefab == null)
        {
            return null;
        }

        Transform sourceDialogCanvas = FindChildRecursive(prefab.transform, "DialogCanvas");
        if (sourceDialogCanvas == null)
        {
            return null;
        }

        GameObject instance = new GameObject("RuntimeDialogPrefabRoot");
        instance.SetActive(false);

        GameObject dialogCanvasObject = Object.Instantiate(sourceDialogCanvas.gameObject, instance.transform, false);
        dialogCanvasObject.name = "DialogCanvas";

        Dialog dialog = dialogCanvasObject.GetComponentInChildren<Dialog>(true);
        if (dialog == null || dialog.dialogPanel == null || dialog.descriptionText == null)
        {
            DestroyRuntimeCreatedObject(instance);
            return null;
        }

        dialog.gameObject.name = RuntimeDialogObjectName;
        dialogCanvasObject.SetActive(true);
        dialog.dialogPanel.SetActive(false);
        ConfigureRuntimeDialogCanvas(dialogCanvasObject.transform);

        dialog.backdropCloseButton = EnsureRuntimeBackdropButton(dialogCanvasObject.transform, dialog.dialogPanel.transform);
        dialog.uiToHide = new GameObject[0];
        dialog.InitializeLifecycle(false);
        instance.SetActive(true);
        return dialog;
    }

    private static void DestroyRuntimeCreatedObject(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(instance);
        }
        else
        {
            Object.DestroyImmediate(instance);
        }
    }
#endif

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ConfigureRuntimeDialogCanvas(Transform dialogCanvas)
    {
        Canvas canvas = dialogCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = RuntimeDialogSortingOrder;
        }

        CanvasScaler scaler = dialogCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (dialogCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            dialogCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static Button EnsureRuntimeBackdropButton(Transform dialogCanvas, Transform dialogPanel)
    {
        Transform existing = dialogCanvas.Find("RuntimeDialogBackdrop");
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        GameObject backdropObject = new GameObject(
            "RuntimeDialogBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        backdropObject.transform.SetParent(dialogCanvas, false);
        backdropObject.transform.SetAsFirstSibling();

        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        StretchRect(backdropRect);

        Image backdropImage = backdropObject.GetComponent<Image>();
        backdropImage.color = Color.clear;
        backdropImage.raycastTarget = true;

        Button backdropButton = backdropObject.GetComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;

        if (dialogPanel != null)
        {
            dialogPanel.SetAsLastSibling();
        }

        return backdropButton;
    }

    private void ShowRuntimeDialogPanelDirectly()
    {
        if (dialogPanel == null)
        {
            return;
        }

        dialogPanel.SetActive(true);
        dialogPanel.transform.SetAsLastSibling();

        Canvas canvas = dialogPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = dialogPanel.GetComponentInParent<Canvas>();
        }

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = RuntimeDialogSortingOrder;
        }

        CanvasGroup group = dialogPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private bool ShouldSubscribeToBackpack()
    {
        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return true;
        }

        return IsRuntimeDialog();
    }

    private bool IsRuntimeDialog()
    {
        return string.Equals(gameObject.name, RuntimeDialogObjectName, System.StringComparison.Ordinal);
    }

    private void InitializeLifecycle(bool forceHide)
    {
        if (initialized)
        {
            TrySubscribeBackpack();
            return;
        }

        initialized = true;
        RuntimeTextFontRepair.RepairLegacyText(descriptionText);
        CacheDescriptionTransform();
        TrySubscribeBackpack();

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.RemoveListener(OnClickCloseDialog);
            clickCloseButton.onClick.AddListener(OnClickCloseDialog);
        }

        if (backdropCloseButton != null)
        {
            backdropCloseButton.onClick.RemoveListener(OnClickCloseDialog);
            backdropCloseButton.onClick.AddListener(OnClickCloseDialog);
        }

        if (forceHide)
        {
            ForceHideImmediately();
        }
    }

    private static Button CreateRuntimeButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ContinueButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        SetRect(buttonRect, new Vector2(-44f, 34f), new Vector2(126f, 46f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        Image buttonImage = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(buttonImage, new Color(0.58f, 0.42f, 0.20f, 0.96f), 10, 10, 1.2f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        Text label = labelObject.GetComponent<Text>();
        label.text = "继续";
        label.fontSize = 22;
        label.color = new Color(1f, 0.96f, 0.84f, 1f);
        label.alignment = TextAnchor.MiddleCenter;
        StretchRect(label.rectTransform);
        RuntimeTextFontRepair.RepairLegacyText(label);

        return buttonObject.GetComponent<Button>();
    }

    private void HideOtherUI(bool hide)
    {
        if (uiToHide == null) return;

        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(!hide);
            }
        }
    }

    private IEnumerator RevealDialogText()
    {
        if (descriptionText == null)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(activeDialogContent))
        {
            RestoreDescriptionPresentation(clearText: true);
            yield break;
        }

        float[] cumulativeWeights = BuildCumulativeWeights(activeDialogContent);
        float totalWeight = cumulativeWeights[cumulativeWeights.Length - 1];
        float revealDuration = Mathf.Clamp(totalWeight * DefaultRevealDurationPerWeight, MinimumRevealDuration, MaximumRevealDuration);
        float elapsed = 0f;
        int lastVisibleCount = -1;

        while (elapsed < revealDuration)
        {
            float progress = Mathf.Clamp01(elapsed / revealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float visibleWeight = totalWeight * easedProgress;
            int visibleCount = ResolveVisibleCharacterCount(cumulativeWeights, visibleWeight);

            if (visibleCount != lastVisibleCount)
            {
                descriptionText.text = BuildRevealText(visibleCount);
                lastVisibleCount = visibleCount;
            }

            UpdateDescriptionPresentation(progress);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        descriptionText.text = activeDialogContent;
        UpdateDescriptionPresentation(1f);
    }

    private void CompleteRevealImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        isRevealPlaying = false;

        if (descriptionText != null)
        {
            descriptionText.text = activeDialogContent;
            UpdateDescriptionPresentation(1f);
        }
    }

    private void PrepareDescriptionForReveal()
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();
        descriptionText.supportRichText = true;
        descriptionText.canvasRenderer.SetAlpha(0f);
        descriptionText.text = BuildRevealText(0);

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = descriptionTextOrigin + Vector2.down * TextFloatDistance;
            descriptionRectTransform.localScale = MultiplyScale(descriptionTextScaleOrigin, TextStartScaleFactor);
        }
    }

    private void RestoreDescriptionPresentation(bool clearText)
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();
        descriptionText.canvasRenderer.SetAlpha(1f);
        descriptionText.text = clearText ? string.Empty : activeDialogContent;

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = descriptionTextOrigin;
            descriptionRectTransform.localScale = descriptionTextScaleOrigin;
        }
    }

    private void UpdateDescriptionPresentation(float progress)
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();

        float clampedProgress = Mathf.Clamp01(progress);
        float alphaProgress = Mathf.Clamp01(clampedProgress / TextFadeInDuration);
        float easedAlpha = Mathf.SmoothStep(0f, 1f, alphaProgress);
        float easedMotion = 1f - Mathf.Pow(1f - clampedProgress, 3f);
        float pop = Mathf.Sin(easedMotion * Mathf.PI) * TextPopStrength;
        float scaleFactor = Mathf.Lerp(TextStartScaleFactor, 1f, easedMotion) + pop;

        descriptionText.canvasRenderer.SetAlpha(easedAlpha);

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                descriptionTextOrigin + Vector2.down * TextFloatDistance,
                descriptionTextOrigin,
                easedMotion);
            descriptionRectTransform.localScale = MultiplyScale(descriptionTextScaleOrigin, scaleFactor);
        }
    }

    private string BuildRevealText(int visibleCount)
    {
        if (string.IsNullOrEmpty(activeDialogContent))
        {
            return string.Empty;
        }

        int clampedVisibleCount = Mathf.Clamp(visibleCount, 0, activeDialogContent.Length);
        if (clampedVisibleCount >= activeDialogContent.Length)
        {
            return activeDialogContent;
        }

        string visibleContent = clampedVisibleCount > 0
            ? activeDialogContent.Substring(0, clampedVisibleCount)
            : string.Empty;
        string hiddenContent = activeDialogContent.Substring(clampedVisibleCount);

        return $"{visibleContent}{WrapHiddenText(hiddenContent)}";
    }

    // 用透明富文本保留完整排版，避免 reveal 过程中出现换行抖动。
    private string WrapHiddenText(string hiddenContent)
    {
        if (string.IsNullOrEmpty(hiddenContent))
        {
            return string.Empty;
        }

        Color hiddenColor = descriptionText != null ? descriptionText.color : Color.white;
        hiddenColor.a = 0f;
        string hiddenColorHex = ColorUtility.ToHtmlStringRGBA(hiddenColor);
        return $"<color=#{hiddenColorHex}>{hiddenContent}</color>";
    }

    private void CacheDescriptionTransform()
    {
        if (cachedDescriptionTransform || descriptionText == null)
        {
            return;
        }

        descriptionRectTransform = descriptionText.rectTransform;
        if (descriptionRectTransform == null)
        {
            return;
        }

        descriptionTextOrigin = descriptionRectTransform.anchoredPosition;
        descriptionTextScaleOrigin = descriptionRectTransform.localScale;
        cachedDescriptionTransform = true;
    }

    private static Vector3 MultiplyScale(Vector3 originalScale, float factor)
    {
        return new Vector3(
            originalScale.x * factor,
            originalScale.y * factor,
            originalScale.z * factor);
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void StretchRectWithOffsets(
        RectTransform rectTransform,
        float left,
        float top,
        float right,
        float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(
        RectTransform rectTransform,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static float[] BuildCumulativeWeights(string content)
    {
        float[] cumulativeWeights = new float[content.Length];
        float total = 0f;

        for (int i = 0; i < content.Length; i++)
        {
            total += GetRevealWeight(content[i]);
            cumulativeWeights[i] = total;
        }

        return cumulativeWeights;
    }

    private static int ResolveVisibleCharacterCount(float[] cumulativeWeights, float visibleWeight)
    {
        if (cumulativeWeights == null || cumulativeWeights.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < cumulativeWeights.Length; i++)
        {
            if (cumulativeWeights[i] > visibleWeight)
            {
                return i;
            }
        }

        return cumulativeWeights.Length;
    }

    private static float GetRevealWeight(char character)
    {
        if (character == '\n' || character == '\r')
        {
            return 0.7f;
        }

        if (char.IsWhiteSpace(character))
        {
            return 0.35f;
        }

        switch (character)
        {
            case '，':
            case '。':
            case '！':
            case '？':
            case '；':
            case '：':
            case '、':
            case ',':
            case '.':
            case '!':
            case '?':
            case ';':
            case ':':
                return 1.65f;
            default:
                return 1f;
        }
    }
}
