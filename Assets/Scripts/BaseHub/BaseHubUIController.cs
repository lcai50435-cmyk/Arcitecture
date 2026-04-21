using UnityEngine;

public class BaseHubUIController : MonoBehaviour
{
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
    private bool isClosingModal;
    private bool hasSavedPlayerState;
    private bool wasMoveEnabled;
    private bool wasCanMove;
    private bool wasBodySimulated = true;
    private bool wasInkAttackEnabled;

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
    }

    public void OpenIllustratedHandbook(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenIllustratedHandbook(source);
            return;
        }

        OpenModal(illustratedHandbookPanel, RuntimeModalType.Handbook, source);
    }

    public void OpenSpiritPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenModal(spiritPanel != null ? spiritPanel.gameObject : null, RuntimeModalType.Spirit, source);
        spiritPanel?.Open();
    }

    public void OpenStageSelectionPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenModal(stageSelectionPanel != null ? stageSelectionPanel.gameObject : null, RuntimeModalType.Stage, source);
        stageSelectionPanel?.Open();
    }

    public void OpenAlbumPanel(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenModal(albumPanel != null ? albumPanel.gameObject : null, RuntimeModalType.Album, source);
        albumPanel?.Open();
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

    private void CompleteCloseAll()
    {
        ClosePanelsOnly();
        SetInteractTipVisible(false);
        UnlockPlayer();
        isClosingModal = false;
    }
}
