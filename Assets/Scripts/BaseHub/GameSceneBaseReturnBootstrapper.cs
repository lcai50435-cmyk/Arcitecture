using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameSceneBaseReturnBootstrapper : MonoBehaviour
{
    private const string BaseSceneName = "NewBase";
    private const KeyCode PlayerPanelHotkey = KeyCode.I;
    private const string PlayerUiCanvasName = "PlayerUI";
    private const string RuntimePanelCanvasName = "RuntimePlayerPanelCanvas";
    private const string RuntimeUiRootName = "RuntimeUIRootManager";

    private GameObject playerObject;
    private CharacterCore playerCore;
    private PlayerAttack playerAttack;
    private PlayerProfileData playerProfile;
    private SpiritPanelUI spiritPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (!GameplayStageCatalog.IsGameplayScene(scene.name)) return;
        if (FindObjectOfType<GameSceneBaseReturnBootstrapper>() != null) return;

        GameObject bootstrapper = new GameObject("GameSceneBaseReturnUI");
        bootstrapper.AddComponent<GameSceneBaseReturnBootstrapper>().Build();
    }

    private void Build()
    {
        EnsureEventSystem();
        EnsureGameplayUiRoot(true);
        ResolveRuntimePlayerPanel();
    }

    private void Update()
    {
        if (!IsGameSceneActive())
        {
            return;
        }

        ResolveRuntimePlayerPanel();
        HandlePlayerPanelHotkey();
    }

    internal static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static UIRootManager EnsureGameplayUiRoot(bool refreshBindings = false)
    {
        UIRootManager rootManager = UIRootManager.Instance ?? FindObjectOfType<UIRootManager>(true);
        if (rootManager != null)
        {
            if (refreshBindings)
            {
                rootManager.RefreshRuntimeBindings();
            }

            return rootManager;
        }

        GameObject rootObject = new GameObject(RuntimeUiRootName);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            rootObject.layer = uiLayer;
        }

        Canvas canvas = rootObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder - 20;
        canvas.overrideSorting = true;

        CanvasScaler scaler = rootObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        rootObject.AddComponent<GraphicRaycaster>();
        rootManager = rootObject.AddComponent<UIRootManager>();
        BackpackUI.EnsureRuntimeInstance();
        rootManager.RefreshRuntimeBindings();
        return rootManager;
    }

    private void HandlePlayerPanelHotkey()
    {
        if (!Input.GetKeyDown(PlayerPanelHotkey))
        {
            return;
        }

        UIRootManager rootManager = EnsureGameplayUiRoot();
        if (rootManager == null)
        {
            return;
        }

        if (playerCore == null || playerProfile == null)
        {
            ResolveRuntimePlayerPanel();
        }

        if (playerCore == null || playerProfile == null)
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
        {
            return;
        }

        if (rootManager.IsAnyGameplayBlockingUIOpen())
        {
            if (rootManager.ActiveModalType == RuntimeModalType.Handbook)
            {
                UIManager handbookManager = null;
                if (!IllustratedUISceneLoader.TryGetUIManager(out handbookManager))
                {
                    handbookManager = UIManager.Instance ?? FindObjectOfType<UIManager>(true);
                }

                if (handbookManager != null)
                {
                    handbookManager.CloseIllustratedHandbook();
                }
                else
                {
                    rootManager.CloseModalFlow();
                }
            }
            else if (rootManager.ActiveModalType == RuntimeModalType.Spirit)
            {
                rootManager.CloseModalFlow();
            }

            return;
        }

        OpenSpiritPanel(rootManager);
    }

    private void ResolveRuntimePlayerPanel()
    {
        if (!TryResolveRuntimePlayer(out GameObject nextPlayerObject, out CharacterCore nextPlayerCore, out PlayerAttack nextPlayerAttack))
        {
            return;
        }

        PlayerProfileData nextPlayerProfile = nextPlayerObject.GetComponent<PlayerProfileData>();
        if (nextPlayerProfile == null)
        {
            nextPlayerProfile = nextPlayerObject.AddComponent<PlayerProfileData>();
        }

        SpriteRenderer playerRenderer = nextPlayerObject.GetComponent<SpriteRenderer>();
        nextPlayerProfile.SyncRuntimeState(nextPlayerCore, nextPlayerAttack, playerRenderer != null ? playerRenderer.sprite : null);

        bool playerChanged = playerObject != nextPlayerObject ||
                             playerCore != nextPlayerCore ||
                             playerAttack != nextPlayerAttack ||
                             playerProfile != nextPlayerProfile;

        playerObject = nextPlayerObject;
        playerCore = nextPlayerCore;
        playerAttack = nextPlayerAttack;
        playerProfile = nextPlayerProfile;

        UIRootManager rootManager = EnsureGameplayUiRoot();

        Transform panelParent = ResolvePanelParent(rootManager);
        spiritPanel = ResolveExistingSpiritPanel(rootManager);

        if (spiritPanel == null)
        {
            if (panelParent == null)
            {
                return;
            }

            spiritPanel = RuntimePlayerPanelBuilder.Create(panelParent, "SpiritPanel");
            playerChanged = true;
        }

        if (spiritPanel == null)
        {
            return;
        }

        if (EnsureSpiritPanelParent(spiritPanel, panelParent))
        {
            playerChanged = true;
        }

        if (playerChanged)
        {
            spiritPanel.Bind(playerCore, playerProfile);
            spiritPanel.SetCloseAction(CloseSpiritPanelFromUi);
            SyncPlayerAttributeManager();
        }

        if (playerChanged || !IsSpiritPanelRegistered(rootManager, spiritPanel))
        {
            RegisterSpiritPanel(rootManager);
        }
    }

    private Transform ResolvePanelParent(UIRootManager rootManager)
    {
        Canvas runtimeCanvas = EnsureRuntimePanelCanvas();
        if (runtimeCanvas != null)
        {
            return runtimeCanvas.transform;
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas fallbackCanvas = null;
        Scene activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.scene != activeScene)
            {
                continue;
            }

            if (canvas.name == PlayerUiCanvasName)
            {
                return canvas.transform;
            }

            if (canvas.name == RuntimePanelCanvasName)
            {
                return canvas.transform;
            }

            if (fallbackCanvas == null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                fallbackCanvas = canvas;
            }
        }

        if (rootManager != null && rootManager.handbookUI != null)
        {
            Canvas handbookCanvas = rootManager.handbookUI.GetComponentInParent<Canvas>();
            if (handbookCanvas != null)
            {
                return handbookCanvas.transform;
            }

            if (rootManager.handbookUI.transform.parent != null)
            {
                return rootManager.handbookUI.transform.parent;
            }
        }

        return fallbackCanvas != null ? fallbackCanvas.transform : EnsureRuntimePanelCanvas().transform;
    }

    private void OpenSpiritPanel(UIRootManager rootManager)
    {
        if (playerCore == null || playerProfile == null)
        {
            return;
        }

        playerProfile.SyncRuntimeState(
            playerCore,
            playerAttack,
            playerObject != null && playerObject.TryGetComponent(out SpriteRenderer spriteRenderer)
                ? spriteRenderer.sprite
                : null);

        if (IllustratedUISceneLoader.Open(
                RuntimeModalOpenSource.None,
                IllustratedHandbookPage.PersonalInformation,
                null,
                null,
                playerObject))
        {
            return;
        }

        if (spiritPanel == null)
        {
            return;
        }

        spiritPanel.Bind(playerCore, playerProfile);
        RegisterSpiritPanel(rootManager);
        rootManager.OpenModal(RuntimeModalType.Spirit, RuntimeModalOpenSource.None);
        spiritPanel.Open();
    }

    private void CloseSpiritPanelFromUi()
    {
        UIRootManager rootManager = UIRootManager.Instance ?? FindObjectOfType<UIRootManager>(true);
        if (rootManager != null && rootManager.ActiveModalType == RuntimeModalType.Spirit)
        {
            rootManager.CloseModalFlow();
            return;
        }

        if (spiritPanel != null)
        {
            spiritPanel.gameObject.SetActive(false);
        }
    }

    private void SyncPlayerAttributeManager()
    {
        PlayerAttributeManager attributeManager = PlayerAttributeManager.Instance != null
            ? PlayerAttributeManager.Instance
            : FindObjectOfType<PlayerAttributeManager>(true);
        if (attributeManager == null)
        {
            return;
        }

        attributeManager.characterCore = playerCore;
        attributeManager.playerAttack = playerAttack;
        attributeManager.profileData = playerProfile;
        attributeManager.ApplyAllBonus();
    }

    private static bool TryResolveRuntimePlayer(out GameObject runtimePlayerObject, out CharacterCore runtimePlayerCore, out PlayerAttack runtimePlayerAttack)
    {
        runtimePlayerObject = GameObject.FindGameObjectWithTag("Player");
        runtimePlayerCore = runtimePlayerObject != null ? runtimePlayerObject.GetComponent<CharacterCore>() : null;
        runtimePlayerAttack = runtimePlayerObject != null ? runtimePlayerObject.GetComponent<PlayerAttack>() : null;
        if (runtimePlayerObject != null && runtimePlayerCore != null && runtimePlayerAttack != null)
        {
            return true;
        }

        runtimePlayerAttack = FindObjectOfType<PlayerAttack>(true);
        runtimePlayerObject = runtimePlayerAttack != null ? runtimePlayerAttack.gameObject : null;
        runtimePlayerCore = runtimePlayerObject != null ? runtimePlayerObject.GetComponent<CharacterCore>() : null;
        if (runtimePlayerObject != null && runtimePlayerCore != null && runtimePlayerAttack != null)
        {
            return true;
        }

        runtimePlayerCore = FindObjectOfType<CharacterCore>(true);
        runtimePlayerObject = runtimePlayerCore != null ? runtimePlayerCore.gameObject : null;
        runtimePlayerAttack = runtimePlayerObject != null ? runtimePlayerObject.GetComponent<PlayerAttack>() : null;
        return runtimePlayerObject != null && runtimePlayerCore != null && runtimePlayerAttack != null;
    }

    private SpiritPanelUI ResolveExistingSpiritPanel(UIRootManager rootManager)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (spiritPanel != null && spiritPanel.gameObject.scene == activeScene)
        {
            return spiritPanel;
        }

        if (rootManager != null && rootManager.spiritPanelUI != null)
        {
            SpiritPanelUI registeredPanel = rootManager.spiritPanelUI.GetComponent<SpiritPanelUI>();
            if (registeredPanel != null && registeredPanel.gameObject.scene == activeScene)
            {
                return registeredPanel;
            }
        }

        SpiritPanelUI[] panels = FindObjectsOfType<SpiritPanelUI>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            SpiritPanelUI candidate = panels[i];
            if (candidate != null && candidate.gameObject.scene == activeScene)
            {
                return candidate;
            }
        }

        return null;
    }

    private void RegisterSpiritPanel(UIRootManager rootManager)
    {
        if (spiritPanel == null || rootManager == null)
        {
            return;
        }

        CanvasGroup spiritCanvasGroup = EnsureCanvasGroup(spiritPanel.gameObject);
        if (rootManager.spiritPanelUI != spiritCanvasGroup)
        {
            rootManager.spiritPanelUI = spiritCanvasGroup;
        }

        rootManager.RefreshRuntimeBindings();
    }

    private static bool IsSpiritPanelRegistered(UIRootManager rootManager, SpiritPanelUI panel)
    {
        if (rootManager == null || panel == null || rootManager.spiritPanelUI == null)
        {
            return false;
        }

        return rootManager.spiritPanelUI.gameObject == panel.gameObject;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static Canvas EnsureRuntimePanelCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Scene activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas existingCanvas = canvases[i];
            if (existingCanvas != null &&
                existingCanvas.gameObject.scene == activeScene &&
                existingCanvas.name == RuntimePanelCanvasName)
            {
                return existingCanvas;
            }
        }

        GameObject canvasObject = new GameObject(RuntimePanelCanvasName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder;
        canvas.overrideSorting = true;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.SetActive(true);
        return canvas;
    }

    private static bool EnsureSpiritPanelParent(SpiritPanelUI panel, Transform expectedParent)
    {
        if (panel == null || expectedParent == null)
        {
            return false;
        }

        RectTransform panelRect = panel.transform as RectTransform;
        if (panel.transform.parent == expectedParent && panelRect != null && panelRect.anchorMin == Vector2.zero && panelRect.anchorMax == Vector2.one)
        {
            return false;
        }

        panel.transform.SetParent(expectedParent, false);

        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        return true;
    }

    public static bool IsGameSceneActive()
    {
        return GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    public static void SubmitCatalogueAndReturnToBase()
    {
        SubmitBackpackToCatalogue();
        ReturnToBaseScene();
    }

    public static void ReturnToBaseScene()
    {
        Time.timeScale = 1f;

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(BaseSceneName);
            return;
        }

        SceneManager.LoadScene(BaseSceneName);
    }

    private static void SubmitBackpackToCatalogue()
    {
        BackpackMananger backpack = BackpackMananger.Instance;
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();

        if (backpack == null || player == null)
        {
            return;
        }

        int itemCount = backpack.GetOccupiedCount();
        if (itemCount <= 0)
        {
            return;
        }

        player.SubmitAllCachedExp();
    }
}

public sealed class GameplayStageRuntimeBootstrapper : MonoBehaviour
{
    private const string BootstrapperObjectName = "GameplayStageRuntimeBootstrapper";
    private const string RuntimeBackpackManagerName = "RuntimeBackpackManager";
    private const string RuntimeCountdownManagerName = "RuntimeGameCountDownManager";
    private const string RuntimeUiRootName = "UIRootManager";
    private const string RuntimeReturnAnchorName = "RuntimeReturnToBaseInteractable";
    private const string ReturnPortalTileResourcePath = "FirstPassReturnPortal";
    private const string DeadSceneName = "DeadScene";
    private const float ReturnAnchorRadius = 0.64f;
    private const float ReturnPortalVisualScale = 2.08f;
    private const float DefaultReturnPortalFrameRate = 8f;

    private static bool sceneHookRegistered;
    private static Sprite returnAnchorSprite;
    private static AnimatedTile returnPortalTile;
    private static readonly List<ReturnPortalAnimationState> returnPortalAnimations = new List<ReturnPortalAnimationState>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (sceneHookRegistered)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneHookRegistered = false;
        }

        returnAnchorSprite = null;
        returnPortalTile = null;
        returnPortalAnimations.Clear();
    }

    private void Update()
    {
        UpdateReturnPortalAnimations();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureSceneHook();
        PrepareScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PrepareScene(scene);
    }

    private static void EnsureSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return GameplayStageCatalog.IsGameplayScene(sceneName);
    }

    private static GameplayStageRuntimeBootstrapper PrepareScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !IsSupportedScene(scene.name))
        {
            return null;
        }

        GameplayStageRuntimeBootstrapper bootstrapper = EnsureSceneBootstrapper(scene);
        GameSceneBaseReturnBootstrapper.EnsureEventSystem();
        EnsureUiRootManager(scene);
        EnsureBackpackManager();
        EnsureCountdownManager();
        EnsureGameplayHud();
        EnsurePlayerRuntimeComponents(scene);
        EnsureReturnToBaseInteractable(scene);
        return bootstrapper;
    }

    private static GameplayStageRuntimeBootstrapper EnsureSceneBootstrapper(Scene scene)
    {
        GameplayStageRuntimeBootstrapper[] existing = FindObjectsOfType<GameplayStageRuntimeBootstrapper>(true);
        for (int i = 0; i < existing.Length; i++)
        {
            GameplayStageRuntimeBootstrapper candidate = existing[i];
            if (candidate != null && candidate.gameObject.scene == scene)
            {
                return candidate;
            }
        }

        GameObject bootstrapperObject = new GameObject(BootstrapperObjectName);
        SceneManager.MoveGameObjectToScene(bootstrapperObject, scene);
        return bootstrapperObject.AddComponent<GameplayStageRuntimeBootstrapper>();
    }

    private static UIRootManager EnsureUiRootManager(Scene scene)
    {
        UIRootManager manager = FindSceneComponent<UIRootManager>(scene);
        if (manager == null)
        {
            GameObject managerObject = new GameObject(RuntimeUiRootName);
            SceneManager.MoveGameObjectToScene(managerObject, scene);
            manager = managerObject.AddComponent<UIRootManager>();
        }

        manager.RefreshRuntimeBindings();
        manager.ShowBackpack(true);
        return manager;
    }

    private static BackpackMananger EnsureBackpackManager()
    {
        if (BackpackMananger.Instance != null)
        {
            return BackpackMananger.Instance;
        }

        BackpackMananger existing = FindObjectOfType<BackpackMananger>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new GameObject(RuntimeBackpackManagerName);
        return managerObject.AddComponent<BackpackMananger>();
    }

    private static GameCountDownManager EnsureCountdownManager()
    {
        GameCountDownManager manager = GameCountDownManager.Instance != null
            ? GameCountDownManager.Instance
            : FindObjectOfType<GameCountDownManager>(true);

        if (manager == null)
        {
            GameObject managerObject = new GameObject(RuntimeCountdownManagerName);
            manager = managerObject.AddComponent<GameCountDownManager>();
        }

        manager.totalTime = 300f;
        manager.SetInBaseState(GameplayStageIntroDirector.IsIntroActive);
        return manager;
    }

    private static void EnsureGameplayHud()
    {
        RuntimeMiniMapHud.EnsureInstance();
        BackpackUI.EnsureRuntimeInstance();
        GameplayStatusHudRuntime.EnsureHealthGauge(null);
        GameplayStatusHudRuntime.EnsureWeaponGauge(null);
        GameplayStatusHudRuntime.RefreshStructureProgressText();
    }

    private static void EnsurePlayerRuntimeComponents(Scene scene)
    {
        GameObject playerObject = ResolveScenePlayer(scene);
        if (playerObject == null)
        {
            return;
        }

        CharacterCore core = playerObject.GetComponent<CharacterCore>();
        PlayerMove move = playerObject.GetComponent<PlayerMove>();
        Animator animator = playerObject.GetComponent<Animator>();

        PlayerAttack attack = playerObject.GetComponent<PlayerAttack>();
        if (attack == null)
        {
            attack = playerObject.AddComponent<PlayerAttack>();
        }

        attack.anim = animator;
        attack.moveScript = move;
        if (attack.inkPoint == null)
        {
            attack.inkPoint = playerObject.transform;
        }

        BaseHubInkAttack baseHubAttack = playerObject.GetComponent<BaseHubInkAttack>();
        if (baseHubAttack != null)
        {
            baseHubAttack.enabled = false;
        }

        PlayerTakeDamage takeDamage = playerObject.GetComponent<PlayerTakeDamage>();
        if (takeDamage == null)
        {
            takeDamage = playerObject.AddComponent<PlayerTakeDamage>();
        }

        takeDamage.playerAnim = animator;
        takeDamage.playerMovement = move;

        PlayerDeathSceneLoader deathSceneLoader = playerObject.GetComponent<PlayerDeathSceneLoader>();
        if (deathSceneLoader == null)
        {
            deathSceneLoader = playerObject.AddComponent<PlayerDeathSceneLoader>();
        }

        deathSceneLoader.characterCore = core;
        deathSceneLoader.gameOverSceneName = DeadSceneName;

        PlayerAttributeManager attributeManager = playerObject.GetComponent<PlayerAttributeManager>();
        if (attributeManager != null)
        {
            attributeManager.characterCore = core;
            attributeManager.playerAttack = attack;
            attributeManager.playerTakeDamage = takeDamage;
            attributeManager.profileData = playerObject.GetComponent<PlayerProfileData>();
            attributeManager.ApplyAllBonus();
        }

        attack.RefreshInkUI();
        if (core != null && core.stats != null)
        {
            GameplayStatusHudRuntime.RefreshHealthText(core.currentHp, core.stats.maxHp);
        }
    }

    private static void EnsureReturnToBaseInteractable(Scene scene)
    {
        if (FindSceneComponent<BookInteract>(scene) != null)
        {
            return;
        }

        GameObject existingAnchor = FindSceneObject(scene, RuntimeReturnAnchorName);
        if (existingAnchor != null)
        {
            return;
        }

        GameObject playerObject = ResolveScenePlayer(scene);
        Vector3 spawnPosition = playerObject != null
            ? playerObject.transform.position + new Vector3(0.78f, -0.18f, 0f)
            : Vector3.zero;

        GameObject anchor = new GameObject(RuntimeReturnAnchorName);
        SceneManager.MoveGameObjectToScene(anchor, scene);
        anchor.transform.position = spawnPosition;
        anchor.transform.localScale = Vector3.one * 0.86f;

        CircleCollider2D trigger = anchor.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = ReturnAnchorRadius;

        CreateReturnPortalVisual(anchor.transform);

        anchor.AddComponent<BookInteract>();
    }

    private static void CreateReturnPortalVisual(Transform parent)
    {
        GameObject visual = new GameObject("ReturnPortalVisual");
        visual.transform.SetParent(parent, false);

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 6;

        Sprite[] portalSprites = GetReturnPortalSprites();
        if (portalSprites.Length > 0)
        {
            visual.transform.localScale = Vector3.one * ReturnPortalVisualScale;
            renderer.sprite = portalSprites[0];
            CenterVisualOnSprite(visual.transform, portalSprites[0]);
            RegisterReturnPortalAnimation(renderer, portalSprites, GetReturnPortalFrameRate());
            return;
        }

        renderer.sprite = GetReturnAnchorSprite();
    }

    private static Sprite[] GetReturnPortalSprites()
    {
        AnimatedTile portalTile = GetReturnPortalTile();
        return portalTile != null && portalTile.m_AnimatedSprites != null
            ? portalTile.m_AnimatedSprites
            : new Sprite[0];
    }

    private static AnimatedTile GetReturnPortalTile()
    {
        if (returnPortalTile == null)
        {
            returnPortalTile = Resources.Load<AnimatedTile>(ReturnPortalTileResourcePath);
        }

        return returnPortalTile;
    }

    private static float GetReturnPortalFrameRate()
    {
        AnimatedTile portalTile = GetReturnPortalTile();
        if (portalTile == null)
        {
            return DefaultReturnPortalFrameRate;
        }

        float minSpeed = Mathf.Max(0f, portalTile.m_MinSpeed);
        float maxSpeed = Mathf.Max(minSpeed, portalTile.m_MaxSpeed);
        float averageSpeed = (minSpeed + maxSpeed) * 0.5f;
        return averageSpeed > 0f ? averageSpeed : DefaultReturnPortalFrameRate;
    }

    private static void CenterVisualOnSprite(Transform visual, Sprite sprite)
    {
        if (visual == null || sprite == null)
        {
            return;
        }

        Vector3 scale = visual.localScale;
        Vector3 centerOffset = sprite.bounds.center;
        visual.localPosition = new Vector3(
            -centerOffset.x * scale.x,
            -centerOffset.y * scale.y,
            0f);
    }

    private static void RegisterReturnPortalAnimation(SpriteRenderer renderer, Sprite[] frames, float frameRate)
    {
        if (renderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        returnPortalAnimations.Add(new ReturnPortalAnimationState(renderer, frames, Mathf.Max(1f, frameRate)));
    }

    private static void UpdateReturnPortalAnimations()
    {
        for (int i = returnPortalAnimations.Count - 1; i >= 0; i--)
        {
            ReturnPortalAnimationState state = returnPortalAnimations[i];
            if (state.Renderer == null || state.Frames == null || state.Frames.Length == 0)
            {
                returnPortalAnimations.RemoveAt(i);
                continue;
            }

            int frameIndex = Mathf.FloorToInt(Time.time * state.FrameRate) % state.Frames.Length;
            Sprite frame = state.Frames[frameIndex];
            if (frame != null)
            {
                state.Renderer.sprite = frame;
            }
        }
    }

    private static GameObject ResolveScenePlayer(Scene scene)
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null && taggedPlayer.scene == scene)
        {
            return taggedPlayer;
        }

        PlayerMove move = FindSceneComponent<PlayerMove>(scene);
        return move != null ? move.gameObject : null;
    }

    private static T FindSceneComponent<T>(Scene scene)
        where T : Component
    {
        T[] components = FindObjectsOfType<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.scene == scene)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildByName(roots[i].transform, objectName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildByName(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Sprite GetReturnAnchorSprite()
    {
        if (returnAnchorSprite != null)
        {
            return returnAnchorSprite;
        }

        Texture2D texture = new Texture2D(32, 24, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = Color.clear;
        Color page = new Color(0.78f, 0.58f, 0.34f, 1f);
        Color pageLight = new Color(0.96f, 0.82f, 0.54f, 1f);
        Color spine = new Color(0.34f, 0.18f, 0.08f, 1f);
        Color outline = new Color(0.12f, 0.07f, 0.03f, 1f);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 4; y < 21; y++)
        {
            for (int x = 3; x < 29; x++)
            {
                bool border = x == 3 || x == 28 || y == 4 || y == 20 || x == 15 || x == 16;
                texture.SetPixel(x, y, border ? outline : (x < 16 ? pageLight : page));
            }
        }

        for (int y = 5; y < 20; y++)
        {
            texture.SetPixel(15, y, spine);
            texture.SetPixel(16, y, spine);
        }

        texture.Apply();
        returnAnchorSprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 24f), new Vector2(0.5f, 0.5f), 24f);
        returnAnchorSprite.name = "RuntimeReturnToBaseBook";
        return returnAnchorSprite;
    }

    private sealed class ReturnPortalAnimationState
    {
        public ReturnPortalAnimationState(SpriteRenderer renderer, Sprite[] frames, float frameRate)
        {
            Renderer = renderer;
            Frames = frames;
            FrameRate = frameRate;
        }

        public SpriteRenderer Renderer { get; }
        public Sprite[] Frames { get; }
        public float FrameRate { get; }
    }
}
