using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseHubUIController : MonoBehaviour
{
    private const KeyCode PlayerPanelHotkey = KeyCode.I;

    [SerializeField] private GameObject illustratedHandbookPanel;
    [SerializeField] private SpiritPanelUI spiritPanel;
    [SerializeField] private StageSelectionPanelUI stageSelectionPanel;
    [SerializeField] private BaseHubAlbumPanel albumPanel;
    [SerializeField] private GameObject interactTipUI;
    [SerializeField] private GameObject player;

    private PlayerMove playerMove;
    private Rigidbody2D playerBody;
    private PlayerInteraction playerInteraction;
    private BaseHubInkAttack playerInkAttack;
    private Transform handbookFocusTarget;
    private Transform spiritFocusTarget;
    private Transform stageFocusTarget;
    private Transform albumFocusTarget;
    private bool isClosingModal;
    private bool hasSavedPlayerState;
    private bool wasMoveEnabled;
    private bool wasCanMove;
    private bool wasBodySimulated = true;
    private bool wasInkAttackEnabled;
    private RuntimeSettingsPanel settingsPanel;

    public void Configure(
        GameObject playerObject,
        GameObject handbookPanel,
        SpiritPanelUI spirit,
        StageSelectionPanelUI stagePanel,
        BaseHubAlbumPanel photoAlbumPanel,
        GameObject interactTip)
    {
        player = playerObject;
        illustratedHandbookPanel = handbookPanel;
        spiritPanel = spirit;
        stageSelectionPanel = stagePanel;
        albumPanel = photoAlbumPanel;
        interactTipUI = interactTip;

        playerMove = player != null ? player.GetComponent<PlayerMove>() : null;
        playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        playerInteraction = player != null ? player.GetComponent<PlayerInteraction>() : null;
        playerInkAttack = player != null ? player.GetComponent<BaseHubInkAttack>() : null;

        isClosingModal = false;
        ClosePanelsOnly();
        EnsureSettingsPanel();
    }

    private void Update()
    {
        HandleSettingsHotkey();
        HandlePlayerPanelHotkey();
    }

    private void OnDestroy()
    {
        if (settingsPanel != null)
        {
            settingsPanel.ContinueRequested -= HandleSettingsPanelClosed;
        }
    }

    private void HandlePlayerPanelHotkey()
    {
        if (!Input.GetKeyDown(PlayerPanelHotkey) || spiritPanel == null || isClosingModal)
        {
            return;
        }

        if (settingsPanel != null && settingsPanel.IsShown)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsModalFlowOpen)
        {
            if (UIRootManager.Instance.ActiveModalType == RuntimeModalType.Spirit)
            {
                CloseAll();
            }

            return;
        }

        OpenSpiritPanel();
    }

    private void HandleSettingsHotkey()
    {
        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        if (pauseKey == KeyCode.None || !Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
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

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsModalFlowOpen)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        OpenSettingsPanel();
    }

    public void OpenIllustratedHandbook(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenHandbookPage(IllustratedHandbookPage.IllustratedHandbook, RuntimeModalType.Handbook, source);
    }

    public void OpenSpiritPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenHandbookPage(IllustratedHandbookPage.PersonalInformation, RuntimeModalType.Spirit, source);
    }

    public void OpenStageSelectionPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        ApplyCameraFocus(RuntimeModalType.Stage, source);
        OpenModal(stageSelectionPanel != null ? stageSelectionPanel.gameObject : null, RuntimeModalType.Stage, source);
        stageSelectionPanel?.Open();
    }

    public void OpenAlbumPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenHandbookPage(IllustratedHandbookPage.PhotoAlbum, RuntimeModalType.Album, source);
    }

    public void CloseAll()
    {
        if (isClosingModal)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsModalFlowOpen)
        {
            isClosingModal = true;
            UIRootManager.Instance.CloseModalFlow(CompleteCloseAll);
            return;
        }

        CompleteCloseAll();
    }

    public void OpenSettingsPanel()
    {
        OpenHandbookPage(IllustratedHandbookPage.Setting, RuntimeModalType.Handbook, RuntimeModalOpenSource.None);
    }

    private void OpenModal(GameObject panel, RuntimeModalType modalType, RuntimeModalOpenSource source)
    {
        if (panel == null) return;

        ClosePanelsOnly();
        LockPlayer();
        SetInteractTipVisible(false);
        panel.SetActive(true);
        isClosingModal = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(modalType, source);
        }
    }

    private void OpenHandbookPage(
        IllustratedHandbookPage page,
        RuntimeModalType focusType,
        RuntimeModalOpenSource source)
    {
        ApplyCameraFocus(focusType, source);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenIllustratedHandbook(source, page);
            return;
        }

        IllustratedHandbookTabsController tabsController = illustratedHandbookPanel != null
            ? illustratedHandbookPanel.GetComponent<IllustratedHandbookTabsController>()
            : null;
        tabsController?.OpenPage(page);
        OpenModal(illustratedHandbookPanel, RuntimeModalType.Handbook, source);
    }

    private void ClosePanelsOnly()
    {
        if (illustratedHandbookPanel != null)
            illustratedHandbookPanel.SetActive(false);

        if (spiritPanel != null)
            spiritPanel.gameObject.SetActive(false);

        if (stageSelectionPanel != null)
            stageSelectionPanel.gameObject.SetActive(false);

        if (albumPanel != null)
            albumPanel.gameObject.SetActive(false);
    }

    private void LockPlayer()
    {
        if (!hasSavedPlayerState)
        {
            wasMoveEnabled = playerMove == null || playerMove.enabled;
            wasCanMove = playerMove == null || playerMove.canMove;
            wasBodySimulated = playerBody == null || playerBody.simulated;
            wasInkAttackEnabled = playerInkAttack == null || playerInkAttack.enabled;
            hasSavedPlayerState = true;
        }

        if (playerInteraction != null)
            playerInteraction.ClearCurrentInteractable();

        if (playerMove != null)
        {
            playerMove.canMove = false;
            playerMove.enabled = false;
        }

        if (playerInkAttack != null)
            playerInkAttack.enabled = false;

        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.simulated = false;
        }
    }

    private void UnlockPlayer()
    {
        if (!hasSavedPlayerState) return;

        if (playerBody != null)
            playerBody.simulated = wasBodySimulated;

        if (playerMove != null)
        {
            playerMove.enabled = wasMoveEnabled;
            playerMove.canMove = wasCanMove;
        }

        if (playerInkAttack != null)
            playerInkAttack.enabled = wasInkAttackEnabled;

        hasSavedPlayerState = false;
    }

    private void SetInteractTipVisible(bool visible)
    {
        if (interactTipUI != null)
            interactTipUI.SetActive(visible);
    }

    private void EnsureSettingsPanel()
    {
        RuntimeSettingsPanel panel = RuntimeSettingsPanel.EnsureInstance();
        if (settingsPanel == panel)
        {
            return;
        }

        if (settingsPanel != null)
        {
            settingsPanel.ContinueRequested -= HandleSettingsPanelClosed;
        }

        settingsPanel = panel;
        settingsPanel.ContinueRequested -= HandleSettingsPanelClosed;
        settingsPanel.ContinueRequested += HandleSettingsPanelClosed;
    }

    private void HandleSettingsPanelClosed()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "NewBase", System.StringComparison.Ordinal))
        {
            return;
        }

        UIRootManager.Instance?.ShowBackpack();
        SetInteractTipVisible(false);
        UnlockPlayer();
        RuntimeCameraController.EnsureInstance().ClearHubFocus();
    }

    private void CompleteCloseAll()
    {
        ClosePanelsOnly();
        SetInteractTipVisible(false);
        UnlockPlayer();
        RuntimeCameraController.EnsureInstance().ClearHubFocus();
        isClosingModal = false;
    }

    private void ApplyCameraFocus(RuntimeModalType modalType, RuntimeModalOpenSource source)
    {
        RuntimeCameraController controller = RuntimeCameraController.EnsureInstance();
        if (source != RuntimeModalOpenSource.Interact)
        {
            controller.ClearHubFocus();
            return;
        }

        Transform focusTarget = ResolveFocusTarget(modalType);
        if (focusTarget != null)
        {
            controller.SetHubFocusTarget(focusTarget);
        }
        else
        {
            controller.ClearHubFocus();
        }
    }

    private Transform ResolveFocusTarget(RuntimeModalType modalType)
    {
        switch (modalType)
        {
            case RuntimeModalType.Handbook:
                handbookFocusTarget = ResolveCachedFocusTarget(handbookFocusTarget, () => FindObjectOfType<BaseHubBookInteract>(true));
                return handbookFocusTarget;
            case RuntimeModalType.Spirit:
                spiritFocusTarget = ResolveCachedFocusTarget(spiritFocusTarget, () => FindObjectOfType<SpiritInteract>(true));
                return spiritFocusTarget;
            case RuntimeModalType.Stage:
                stageFocusTarget = ResolveCachedFocusTarget(stageFocusTarget, () => FindObjectOfType<BaseHubGameSceneInteract>(true));
                return stageFocusTarget;
            case RuntimeModalType.Album:
                albumFocusTarget = ResolveCachedFocusTarget(albumFocusTarget, () => FindObjectOfType<BaseHubAlbumInteract>(true));
                return albumFocusTarget;
            default:
                return null;
        }
    }

    private static Transform ResolveCachedFocusTarget<T>(Transform cachedTarget, System.Func<T> resolver)
        where T : Component
    {
        if (cachedTarget != null)
        {
            return cachedTarget;
        }

        T component = resolver != null ? resolver() : null;
        return component != null ? component.transform : null;
    }
}
