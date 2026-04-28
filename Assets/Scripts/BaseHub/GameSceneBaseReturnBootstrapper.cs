using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneBaseReturnBootstrapper : MonoBehaviour
{
    private const string BaseSceneName = "NewBase";
    private const KeyCode PlayerPanelHotkey = KeyCode.I;
    private const string PlayerUiCanvasName = "PlayerUI";
    private const string RuntimePanelCanvasName = "RuntimePlayerPanelCanvas";

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

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void HandlePlayerPanelHotkey()
    {
        if (!Input.GetKeyDown(PlayerPanelHotkey))
        {
            return;
        }

        UIRootManager rootManager = UIRootManager.Instance ?? FindObjectOfType<UIRootManager>(true);
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

        UIRootManager rootManager = UIRootManager.Instance ?? FindObjectOfType<UIRootManager>(true);

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
