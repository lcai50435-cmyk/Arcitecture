using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelSelectionSceneController : MonoBehaviour
{
    public const string SceneName = "LevelSelection";
    private const string ScenePath = "Assets/Scenes/LevelSelection.unity";
    private const string BaseSceneName = "NewBase";
    private const string ScrollRootName = "LevelSelectionHorizontalScroll";
    private const string ViewportName = "Viewport";
    private const string ContentName = "Content";
    private const string GeneratedCardSuffix = "_Card";
    private const string StagePreviewName = "StagePreview";
    private const string StageTitleName = "StageTitle";
    private const string StageMapName = "StageMap";
    private const string StageStatusName = "StageStatus";
    private const string StageHintName = "StageHint";
    private const string StageActionName = "StageAction";
    private const string StageLockName = "StageLock";
    private const string CloseButtonName = "LevelSelectionCloseButton";
    private const string BackdropMaskName = "LevelSelectionBackdropMask";
    private const string BackdropBlurName = "BackdropBlur";
    private const string BackdropTintName = "BackdropTint";
    private const string BackdropOverlayName = "BackdropOverlay";
    private const string StageCardFrameSpritePath = "Assets/File/Prop/UIProp/NewUI/Setting_4.png";
    private const string StageLockSpritePath = "Assets/File/Prop/UIProp/NewUI/Lock.png";
    private const string CloseButtonSpritePath = "Assets/File/UIResources/CloseButton.png";
    private const string FujianTulouPreviewSpritePath = "Assets/File/UIResources/FuJianTuLou_1.png";
    private const string ZhaozhouBridgePreviewSpritePath = "Assets/File/UIResources/ZhaoGouBridge.png";
    private const string ShuiXiangPreviewSpritePath = "Assets/File/UIResources/ShuiXiang.png";
    private static readonly Vector2 ScrollViewportSize = new Vector2(1640f, 740f);
    private static readonly Vector2 ScrollViewportPosition = new Vector2(0f, -70f);
    private static readonly Vector2 StageDisplaySize = new Vector2(500f, 640f);
    private static readonly Vector2 StagePreviewSize = new Vector2(380f, 240f);
    private static readonly Vector2 StagePreviewPosition = new Vector2(0f, 102f);
    private static readonly Vector2 StageLargeLockSize = new Vector2(190f, 190f);
    private static readonly Vector2 CloseButtonSize = new Vector2(78f, 78f);
    private static readonly Vector2 CloseButtonPosition = new Vector2(-92f, -82f);
    private static readonly Vector3 StageDisplayScale = Vector3.one;
    private const float StageSpacing = 64f;
    private const float PanelBackgroundAlpha = 0.91f;
    private static readonly Color StageTitleColor = new Color(0.12f, 0.08f, 0.04f, 1f);
    private static readonly Color StageMutedTextColor = new Color(0.36f, 0.28f, 0.20f, 1f);
    private static readonly Color StageUnlockedTextColor = new Color(0.25f, 0.46f, 0.22f, 1f);
    private static readonly Color StageLockedTextColor = new Color(0.52f, 0.30f, 0.22f, 1f);
    private static readonly Color StageUnavailableImageColor = new Color(0.72f, 0.72f, 0.72f, 0.72f);

    private readonly HashSet<Button> stageButtons = new HashSet<Button>();
    private static bool hasPendingBaseReturnPosition;
    private static Vector3 pendingBaseReturnPosition;
    private ScrollRect stageScrollRect;
    private static Texture2D pendingBackdropTexture;
    private static RenderTexture pendingBlurredBackdropTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static bool CanLoadScene()
    {
        return Application.CanStreamedLevelBeLoaded(SceneName) ||
               Application.CanStreamedLevelBeLoaded(ScenePath);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsBaseScene(scene))
        {
            RestorePendingBaseReturnPosition(scene);
            return;
        }

        if (!IsLevelSelectionScene(scene))
        {
            return;
        }

        EnsureController(scene);
    }

    private static bool IsLevelSelectionScene(Scene scene)
    {
        return scene.IsValid() && string.Equals(scene.name, SceneName, System.StringComparison.Ordinal);
    }

    private static bool IsBaseScene(Scene scene)
    {
        return scene.IsValid() && string.Equals(scene.name, BaseSceneName, System.StringComparison.Ordinal);
    }

    public static void CaptureBaseReturnPositionFromCurrentPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            CaptureBaseReturnPosition(playerObject.transform.position);
        }
    }

    public static void CaptureBaseReturnPosition(Vector3 position)
    {
        pendingBaseReturnPosition = position;
        hasPendingBaseReturnPosition = true;
    }

    public static void CaptureBackdropBeforeSceneLoad()
    {
        ReleasePendingBackdrop();

        Texture2D capturedTexture = RuntimeModalStyle.CaptureBackdropTexture();
        RenderTexture blurredTexture = RuntimeModalStyle.BuildBlurBackdrop(capturedTexture);
        if (blurredTexture == null)
        {
            if (capturedTexture != null)
            {
                Destroy(capturedTexture);
            }

            return;
        }

        pendingBackdropTexture = capturedTexture;
        pendingBlurredBackdropTexture = blurredTexture;
    }

    public static bool TryApplyPendingBaseReturnPosition(GameObject playerObject)
    {
        if (!hasPendingBaseReturnPosition || playerObject == null)
        {
            return false;
        }

        Rigidbody2D body = playerObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.position = pendingBaseReturnPosition;
        }

        playerObject.transform.position = pendingBaseReturnPosition;
        hasPendingBaseReturnPosition = false;
        return true;
    }

    private static void ClearPendingBaseReturnPosition()
    {
        hasPendingBaseReturnPosition = false;
    }

    private static void ReleasePendingBackdrop()
    {
        RawImage noBoundImage = null;
        RuntimeModalStyle.ReleaseBlurBackdrop(
            noBoundImage,
            ref pendingBackdropTexture,
            ref pendingBlurredBackdropTexture);
    }

    private static void RestorePendingBaseReturnPosition(Scene scene)
    {
        if (!hasPendingBaseReturnPosition)
        {
            return;
        }

        if (TryApplyPendingBaseReturnPosition(GameObject.FindGameObjectWithTag("Player")))
        {
            return;
        }

        GameObject restorerObject = new GameObject("LevelSelectionBaseReturnPositionRestorer");
        SceneManager.MoveGameObjectToScene(restorerObject, scene);
        restorerObject.AddComponent<BaseReturnPositionRestorer>();
    }

    private static void EnsureController(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            LevelSelectionSceneController existing = roots[i].GetComponentInChildren<LevelSelectionSceneController>(true);
            if (existing != null)
            {
                existing.BindScene();
                return;
            }
        }

        GameObject controllerObject = new GameObject("LevelSelectionSceneController");
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        controllerObject.AddComponent<LevelSelectionSceneController>().BindScene();
    }

    private void Start()
    {
        BindScene();
    }

    private void OnDestroy()
    {
        ReleasePendingBackdrop();
    }

    private void BindScene()
    {
        stageButtons.Clear();
        HideLegacyStageRoots();
        RectTransform panelRoot = FindPanelRoot();
        EnsureBackdropMask(panelRoot);
        ApplyPanelTransparency(panelRoot);

        IReadOnlyList<GameplayStageDefinition> stageDefinitions = GameplayStageCatalog.GetAll();
        Transform[] stageRoots = EnsureCatalogStageRoots(stageDefinitions);
        ArrangeHorizontalScroller(stageRoots);
        BindCatalogStages(stageRoots, stageDefinitions);
        EnsureCloseButton();
        BindBackButtons();
    }

    private static void EnsureBackdropMask(RectTransform panelRoot)
    {
        if (panelRoot == null)
        {
            return;
        }

        RectTransform backdropParent = panelRoot.parent as RectTransform;
        if (backdropParent == null)
        {
            backdropParent = panelRoot;
        }

        Transform existing = FindDirectChildByName(backdropParent, BackdropMaskName);
        RectTransform backdropRect = existing as RectTransform;
        Image hitMaskImage;
        if (backdropRect == null)
        {
            GameObject backdropObject = new GameObject(BackdropMaskName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.SetParent(backdropParent, false);
            hitMaskImage = backdropObject.GetComponent<Image>();
        }
        else
        {
            hitMaskImage = backdropRect.GetComponent<Image>();
            if (hitMaskImage == null)
            {
                hitMaskImage = backdropRect.gameObject.AddComponent<Image>();
            }
        }

        StretchRect(backdropRect);
        backdropRect.localScale = Vector3.one;
        backdropRect.localRotation = Quaternion.identity;
        backdropRect.SetAsFirstSibling();

        hitMaskImage.color = new Color(0f, 0f, 0f, 0.001f);
        hitMaskImage.raycastTarget = true;

        RawImage blurImage = EnsureBackdropBlur(backdropRect);
        Image tintImage = EnsureBackdropImage(backdropRect, BackdropTintName);
        Image overlayImage = EnsureBackdropImage(backdropRect, BackdropOverlayName);
        RuntimeModalStyle.ApplyBackdropState(blurImage, tintImage, overlayImage, 1f);

        if (pendingBlurredBackdropTexture == null)
        {
            blurImage.color = Color.clear;
        }
    }

    private static RawImage EnsureBackdropBlur(RectTransform backdropRoot)
    {
        Transform existing = backdropRoot.Find(BackdropBlurName);
        RectTransform blurRect;
        RawImage blurImage;
        if (existing == null)
        {
            GameObject blurObject = new GameObject(BackdropBlurName, typeof(RectTransform), typeof(RawImage));
            blurRect = blurObject.GetComponent<RectTransform>();
            blurRect.SetParent(backdropRoot, false);
            blurImage = blurObject.GetComponent<RawImage>();
        }
        else
        {
            blurRect = existing as RectTransform;
            blurImage = existing.GetComponent<RawImage>();
            if (blurImage == null)
            {
                blurImage = existing.gameObject.AddComponent<RawImage>();
            }
        }

        StretchRect(blurRect);
        blurRect.SetAsFirstSibling();
        blurImage.texture = pendingBlurredBackdropTexture;
        blurImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        blurImage.raycastTarget = false;
        return blurImage;
    }

    private static Image EnsureBackdropImage(RectTransform backdropRoot, string objectName)
    {
        Transform existing = backdropRoot.Find(objectName);
        RectTransform imageRect;
        Image image;
        if (existing == null)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(backdropRoot, false);
            image = imageObject.GetComponent<Image>();
        }
        else
        {
            imageRect = existing as RectTransform;
            image = existing.GetComponent<Image>();
            if (image == null)
            {
                image = existing.gameObject.AddComponent<Image>();
            }
        }

        StretchRect(imageRect);
        image.raycastTarget = false;
        return image;
    }

    private static void ApplyPanelTransparency(RectTransform panelRoot)
    {
        if (panelRoot == null)
        {
            return;
        }

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage == null)
        {
            return;
        }

        Color panelColor = panelImage.color;
        panelColor.a = PanelBackgroundAlpha;
        panelImage.color = panelColor;
    }

    private Transform[] EnsureCatalogStageRoots(IReadOnlyList<GameplayStageDefinition> stageDefinitions)
    {
        RectTransform panelRoot = FindPanelRoot();
        if (panelRoot == null || stageDefinitions == null)
        {
            return new Transform[0];
        }

        Transform[] stageRoots = new Transform[stageDefinitions.Count];
        for (int i = 0; i < stageDefinitions.Count; i++)
        {
            GameplayStageDefinition stage = stageDefinitions[i];
            if (stage == null)
            {
                continue;
            }

            stageRoots[i] = EnsureCatalogStageRoot(panelRoot, stage, i + 1);
        }

        return stageRoots;
    }

    private Transform EnsureCatalogStageRoot(RectTransform panelRoot, GameplayStageDefinition stage, int stageNumber)
    {
        string cardName = $"{stage.stageId}{GeneratedCardSuffix}";
        Transform existing = FindChildByName(panelRoot, cardName);
        RectTransform cardRect = existing as RectTransform;
        if (cardRect == null)
        {
            GameObject cardObject = new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.SetParent(panelRoot, false);
        }

        cardRect.gameObject.SetActive(true);
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;
        cardRect.sizeDelta = StageDisplaySize;

        bool unlocked = GameplayStageCatalog.IsStageUnlocked(stage);
        bool placeholder = stage.isPlaceholder;
        Image background = cardRect.GetComponent<Image>();
        background.sprite = ResolveStageCardFrameSprite();
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.color = Color.white;
        background.raycastTarget = true;

        Button button = cardRect.GetComponent<Button>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
        }

        Sprite previewSprite = ResolveStagePreviewSprite(stage);
        bool hasPreviewSprite = previewSprite != null;
        EnsureStagePreview(cardRect, previewSprite, unlocked && !placeholder);
        EnsureStageLock(cardRect, !hasPreviewSprite);
        EnsureStageCardText(cardRect, StageTitleName, stage.displayName, 31f, new Vector2(0f, 278f), new Vector2(410f, 58f), StageTitleColor);
        EnsureStageCardText(cardRect, StageMapName, $"地图：{stage.mapTitle}", 22f, new Vector2(0f, -50f), new Vector2(400f, 36f), StageMutedTextColor);
        EnsureStageCardText(
            cardRect,
            StageStatusName,
            placeholder ? "未开放" : unlocked ? "可进入" : "未解锁",
            24f,
            new Vector2(0f, -106f),
            new Vector2(260f, 40f),
            placeholder || !unlocked ? StageLockedTextColor : StageUnlockedTextColor);
        EnsureStageCardText(
            cardRect,
            StageHintName,
            placeholder ? stage.lockedHint : unlocked ? $"点击进入第 {stageNumber} 关" : stage.lockedHint,
            19f,
            new Vector2(0f, -178f),
            new Vector2(390f, 64f),
            StageMutedTextColor);
        EnsureStageCardText(
            cardRect,
            StageActionName,
            placeholder ? "敬请期待" : unlocked ? "进入关卡" : "先完成前置修复",
            23f,
            new Vector2(0f, -270f),
            new Vector2(300f, 46f),
            StageTitleColor);

        return cardRect;
    }

    private static Image EnsureStagePreview(RectTransform cardRoot, Sprite previewSprite, bool available)
    {
        Transform existing = cardRoot.Find(StagePreviewName);
        RectTransform previewRect;
        Image previewImage;
        if (existing == null)
        {
            GameObject previewObject = new GameObject(StagePreviewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewRect = previewObject.GetComponent<RectTransform>();
            previewRect.SetParent(cardRoot, false);
            previewImage = previewObject.GetComponent<Image>();
        }
        else
        {
            previewRect = existing as RectTransform;
            previewImage = existing.GetComponent<Image>();
            if (previewImage == null)
            {
                previewImage = existing.gameObject.AddComponent<Image>();
            }
        }

        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = StagePreviewPosition;
        previewRect.sizeDelta = StagePreviewSize;
        previewRect.localScale = Vector3.one;
        previewRect.SetAsLastSibling();

        previewImage.sprite = previewSprite;
        previewImage.type = Image.Type.Simple;
        previewImage.preserveAspect = true;
        previewImage.color = available ? Color.white : StageUnavailableImageColor;
        previewImage.raycastTarget = false;
        previewImage.gameObject.SetActive(previewSprite != null);
        return previewImage;
    }

    private static Image EnsureStageLock(RectTransform cardRoot, bool visible)
    {
        Transform existing = cardRoot.Find(StageLockName);
        RectTransform lockRect;
        Image lockImage;
        if (existing == null)
        {
            GameObject lockObject = new GameObject(StageLockName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockRect = lockObject.GetComponent<RectTransform>();
            lockRect.SetParent(cardRoot, false);
            lockImage = lockObject.GetComponent<Image>();
        }
        else
        {
            lockRect = existing as RectTransform;
            lockImage = existing.GetComponent<Image>();
            if (lockImage == null)
            {
                lockImage = existing.gameObject.AddComponent<Image>();
            }
        }

        lockRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockRect.pivot = new Vector2(0.5f, 0.5f);
        lockRect.anchoredPosition = StagePreviewPosition;
        lockRect.sizeDelta = StageLargeLockSize;
        lockRect.localScale = Vector3.one;
        lockRect.SetAsLastSibling();

        lockImage.sprite = LoadSprite(StageLockSpritePath);
        lockImage.type = Image.Type.Simple;
        lockImage.preserveAspect = true;
        lockImage.color = Color.white;
        lockImage.raycastTarget = false;
        lockImage.gameObject.SetActive(visible && lockImage.sprite != null);
        return lockImage;
    }

    private static void EnsureCloseButton()
    {
        RectTransform panelRoot = FindPanelRoot();
        if (panelRoot == null)
        {
            return;
        }

        Transform existing = FindChildByName(panelRoot, CloseButtonName);
        RectTransform closeRect = existing as RectTransform;
        if (closeRect == null)
        {
            GameObject closeObject = new GameObject(CloseButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.SetParent(panelRoot, false);
        }

        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = CloseButtonPosition;
        closeRect.sizeDelta = CloseButtonSize;
        closeRect.localScale = Vector3.one;
        closeRect.localRotation = Quaternion.identity;
        closeRect.SetAsLastSibling();
        closeRect.gameObject.SetActive(true);

        Image closeImage = closeRect.GetComponent<Image>();
        closeImage.sprite = LoadSprite(CloseButtonSpritePath);
        closeImage.type = Image.Type.Simple;
        closeImage.preserveAspect = true;
        closeImage.color = Color.white;
        closeImage.raycastTarget = true;

        Button closeButton = closeRect.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.targetGraphic = closeImage;
        EnsureCloseFallbackGlyph(closeRect, closeImage.sprite == null);
    }

    private static void EnsureCloseFallbackGlyph(RectTransform closeRect, bool visible)
    {
        Transform existing = closeRect.Find("CloseGlyph");
        TextMeshProUGUI label;
        RectTransform labelRect;
        if (existing == null)
        {
            GameObject labelObject = new GameObject("CloseGlyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(closeRect, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            labelRect = existing as RectTransform;
            label = existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = "X";
        label.fontSize = 42f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = StageTitleColor;
        label.raycastTarget = false;
        label.gameObject.SetActive(visible);
    }

    private static Sprite ResolveStageCardFrameSprite()
    {
        return LoadSprite(StageCardFrameSpritePath) ?? FindLegacyStageSprite("FirstPass", "Back");
    }

    private static Sprite ResolveStagePreviewSprite(GameplayStageDefinition stage)
    {
        if (stage == null || stage.isPlaceholder)
        {
            return null;
        }

        Sprite sprite = LoadSprite(ResolveStagePreviewSpritePath(stage.stageId));
        if (sprite != null)
        {
            return sprite;
        }

        string legacyRootName = ResolveLegacyStageRootName(stage.stageId);
        return !string.IsNullOrEmpty(legacyRootName)
            ? FindLegacyStageSprite(legacyRootName, "Button")
            : null;
    }

    private static string ResolveStagePreviewSpritePath(string stageId)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(stageId);
        return ResolveStagePreviewSpritePath(stage);
    }

    private static string ResolveStagePreviewSpritePath(GameplayStageDefinition stage)
    {
        if (stage == null || stage.isPlaceholder)
        {
            return string.Empty;
        }

        switch (stage.stageBuildingId)
        {
            case CatalogueBuildingId.Building1:
                return FujianTulouPreviewSpritePath;
            case CatalogueBuildingId.Building2:
                return ZhaozhouBridgePreviewSpritePath;
            case CatalogueBuildingId.Building3:
                return ShuiXiangPreviewSpritePath;
            default:
                return string.Empty;
        }
    }

    private static string ResolveLegacyStageRootName(string stageId)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(stageId);
        if (stage == null || stage.isPlaceholder)
        {
            return string.Empty;
        }

        switch (stage.stageBuildingId)
        {
            case CatalogueBuildingId.Building1:
                return "FirstPass";
            case CatalogueBuildingId.Building2:
                return "SecondPass";
            case CatalogueBuildingId.Building3:
                return "ThirdPass";
            default:
                return string.Empty;
        }
    }

    private static Sprite FindLegacyStageSprite(string rootName, string childName)
    {
        Transform stageRoot = FindStageRoot(rootName);
        Transform child = FindChildByName(stageRoot, childName);
        Image image = child != null ? child.GetComponent<Image>() : null;
        return image != null ? image.sprite : null;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        return RuntimeProjectSpriteLoader.LoadSprite(assetPath, false, SpriteMeshType.FullRect);
    }

    private static TextMeshProUGUI EnsureStageCardText(
        RectTransform cardRoot,
        string objectName,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        Transform existing = cardRoot.Find(objectName);
        TextMeshProUGUI label;
        RectTransform labelRect;
        if (existing == null)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(cardRoot, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            labelRect = existing as RectTransform;
            label = existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = sizeDelta;
        labelRect.localScale = Vector3.one;
        labelRect.SetAsLastSibling();

        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = Mathf.Max(14f, fontSize - 8f);
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private void BindCatalogStages(Transform[] stageRoots, IReadOnlyList<GameplayStageDefinition> stageDefinitions)
    {
        if (stageRoots == null || stageDefinitions == null)
        {
            return;
        }

        int count = Mathf.Min(stageRoots.Length, stageDefinitions.Count);
        for (int i = 0; i < count; i++)
        {
            BindStage(stageRoots[i], stageDefinitions[i]);
        }
    }

    private void BindStage(Transform stageRoot, string stageId)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(stageId);
        BindStage(stageRoot, stage);
    }

    private void BindStage(Transform stageRoot, GameplayStageDefinition stage)
    {
        if (stageRoot == null || stage == null)
        {
            return;
        }

        Button button = stageRoot.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            return;
        }

        bool unlocked = GameplayStageCatalog.IsStageUnlocked(stage);
        stageButtons.Add(button);
        HidePlaceholderButtonLabels(button.transform);

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.interactable = unlocked && !stage.isPlaceholder;
        button.onClick.AddListener(() => EnterStage(stage));
    }

    private void ArrangeHorizontalScroller(params Transform[] stageRoots)
    {
        RectTransform panelRoot = FindPanelRoot();
        if (panelRoot == null)
        {
            return;
        }

        RectTransform scrollRoot = EnsureScrollRoot(panelRoot);
        RectTransform viewport = EnsureViewport(scrollRoot);
        RectTransform content = EnsureContent(viewport);
        stageScrollRect = EnsureScrollRect(scrollRoot, viewport, content);

        int validStageCount = 0;
        for (int i = 0; i < stageRoots.Length; i++)
        {
            if (stageRoots[i] != null)
            {
                validStageCount++;
            }
        }

        if (validStageCount == 0)
        {
            return;
        }

        float sidePadding = validStageCount <= 1
            ? Mathf.Max(0f, (ScrollViewportSize.x - StageDisplaySize.x) * 0.5f)
            : 48f;
        float contentWidth = sidePadding * 2f + validStageCount * StageDisplaySize.x + Mathf.Max(0, validStageCount - 1) * StageSpacing;
        content.sizeDelta = new Vector2(contentWidth, ScrollViewportSize.y);

        int visibleIndex = 0;
        for (int i = 0; i < stageRoots.Length; i++)
        {
            RectTransform stageRect = stageRoots[i] as RectTransform;
            if (stageRect == null)
            {
                continue;
            }

            PrepareStageRoot(stageRect);
            stageRect.SetParent(content, false);
            stageRect.anchorMin = new Vector2(0f, 0.5f);
            stageRect.anchorMax = new Vector2(0f, 0.5f);
            stageRect.pivot = new Vector2(0.5f, 0.5f);
            stageRect.sizeDelta = StageDisplaySize;
            stageRect.localScale = StageDisplayScale;
            stageRect.anchoredPosition = new Vector2(
                sidePadding + StageDisplaySize.x * 0.5f + visibleIndex * (StageDisplaySize.x + StageSpacing),
                0f);
            stageRect.SetAsLastSibling();
            visibleIndex++;
        }

        Canvas.ForceUpdateCanvases();
        stageScrollRect.horizontalNormalizedPosition = ResolveInitialScrollPosition(validStageCount);
    }

    private static RectTransform FindPanelRoot()
    {
        Transform panelRoot = FindRootByName("BackGround");
        return panelRoot as RectTransform;
    }

    private static RectTransform EnsureScrollRoot(RectTransform panelRoot)
    {
        Transform existing = FindChildByName(panelRoot, ScrollRootName);
        RectTransform scrollRoot = existing as RectTransform;
        if (scrollRoot == null)
        {
            GameObject scrollObject = new GameObject(ScrollRootName, typeof(RectTransform));
            scrollRoot = scrollObject.GetComponent<RectTransform>();
            scrollRoot.SetParent(panelRoot, false);
        }

        scrollRoot.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRoot.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        scrollRoot.anchoredPosition = ScrollViewportPosition;
        scrollRoot.sizeDelta = ScrollViewportSize;
        scrollRoot.localScale = Vector3.one;
        scrollRoot.SetAsLastSibling();
        return scrollRoot;
    }

    private static RectTransform EnsureViewport(RectTransform scrollRoot)
    {
        Transform existing = FindChildByName(scrollRoot, ViewportName);
        RectTransform viewport = existing as RectTransform;
        if (viewport == null)
        {
            GameObject viewportObject = new GameObject(ViewportName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollRoot, false);
        }

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewport.localScale = Vector3.one;

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
        }

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        return viewport;
    }

    private static RectTransform EnsureContent(RectTransform viewport)
    {
        Transform existing = FindChildByName(viewport, ContentName);
        RectTransform content = existing as RectTransform;
        if (content == null)
        {
            GameObject contentObject = new GameObject(ContentName, typeof(RectTransform));
            content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
        }

        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(0f, 0.5f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = Vector3.one;
        return content;
    }

    private static ScrollRect EnsureScrollRect(RectTransform scrollRoot, RectTransform viewport, RectTransform content)
    {
        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 52f;
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return scrollRect;
    }

    private static void PrepareStageRoot(RectTransform stageRect)
    {
        if (stageRect.name.EndsWith(GeneratedCardSuffix, System.StringComparison.Ordinal))
        {
            return;
        }

        Canvas[] canvases = stageRect.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].enabled = false;
        }

        CanvasScaler[] scalers = stageRect.GetComponentsInChildren<CanvasScaler>(true);
        for (int i = 0; i < scalers.Length; i++)
        {
            scalers[i].enabled = false;
        }

        GraphicRaycaster[] raycasters = stageRect.GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
        {
            raycasters[i].enabled = false;
        }
    }

    private static float ResolveInitialScrollPosition(int visibleStageCount)
    {
        if (visibleStageCount <= 1)
        {
            return 0f;
        }

        int selectedIndex = Mathf.Clamp(GameplayStageCatalog.GetStageIndex(GameplayStageRuntime.SelectedStageId), 0, visibleStageCount - 1);
        return selectedIndex / (float)(visibleStageCount - 1);
    }

    private void BindBackButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || stageButtons.Contains(button))
            {
                continue;
            }

            HidePlaceholderButtonLabels(button.transform);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ReturnToBase);
        }
    }

    private void EnterStage(GameplayStageDefinition stage)
    {
        if (stage == null || !GameplayStageCatalog.IsStageUnlocked(stage))
        {
            return;
        }

        ClearPendingBaseReturnPosition();
        GameplayStageRuntime.SelectStage(stage.stageId);
        GameProgressPersistence.SaveIfReady();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(stage.sceneName);
            return;
        }

        SceneManager.LoadScene(stage.sceneName);
    }

    private void ReturnToBase()
    {
        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(BaseSceneName);
            return;
        }

        SceneManager.LoadScene(BaseSceneName);
    }

    private static Transform FindStageRoot(string rootName)
    {
        RectTransform[] candidates = FindObjectsOfType<RectTransform>(true);
        Transform best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (candidate == null || !string.Equals(candidate.name, rootName, System.StringComparison.Ordinal))
            {
                continue;
            }

            int score = ScoreStageRoot(candidate);
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static void HideLegacyStageRoots()
    {
        HideLegacyStageRoot("FirstPass");
        HideLegacyStageRoot("SecondPass");
        HideLegacyStageRoot("ThirdPass");
    }

    private static void HideLegacyStageRoot(string rootName)
    {
        RectTransform[] candidates = FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            RectTransform candidate = candidates[i];
            if (candidate != null && string.Equals(candidate.name, rootName, System.StringComparison.Ordinal))
            {
                candidate.gameObject.SetActive(false);
            }
        }
    }

    private static Transform FindRootByName(string rootName)
    {
        RectTransform[] candidates = FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            RectTransform candidate = candidates[i];
            if (candidate != null && string.Equals(candidate.name, rootName, System.StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static int ScoreStageRoot(Transform candidate)
    {
        int score = 0;
        if (candidate.GetComponent<Canvas>() != null)
        {
            score += 4;
        }

        if (candidate.GetComponentInChildren<Button>(true) != null)
        {
            score += 4;
        }

        if (FindChildByName(candidate, "BuildingName") != null)
        {
            score += 2;
        }

        return score;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, targetName, System.StringComparison.Ordinal))
            {
                return child;
            }

            Transform nested = FindChildByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static Transform FindDirectChildByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && string.Equals(child.name, targetName, System.StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void HidePlaceholderButtonLabels(Transform root)
    {
        if (root == null)
        {
            return;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && string.Equals(text.text, "Button", System.StringComparison.Ordinal))
            {
                text.text = string.Empty;
                text.raycastTarget = false;
            }
        }
    }

    private sealed class BaseReturnPositionRestorer : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int i = 0; i < 12 && hasPendingBaseReturnPosition; i++)
            {
                if (TryApplyPendingBaseReturnPosition(GameObject.FindGameObjectWithTag("Player")))
                {
                    break;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
