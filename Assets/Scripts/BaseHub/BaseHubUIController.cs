using UnityEngine;

public class BaseHubUIController : MonoBehaviour
{
    [SerializeField] private GameObject illustratedHandbookPanel;
    [SerializeField] private SpiritPanelUI spiritPanel;
    [SerializeField] private GameObject interactTipUI;
    [SerializeField] private GameObject player;

    private PlayerMove playerMove;
    private Rigidbody2D playerBody;
    private PlayerInteraction playerInteraction;
    private BaseHubInkAttack playerInkAttack;
    private bool isModalOpen;
    private bool hasSavedPlayerState;
    private bool wasMoveEnabled;
    private bool wasCanMove;
    private bool wasBodySimulated = true;
    private bool wasInkAttackEnabled;

    public void Configure(
        GameObject playerObject,
        GameObject handbookPanel,
        SpiritPanelUI spirit,
        GameObject interactTip)
    {
        player = playerObject;
        illustratedHandbookPanel = handbookPanel;
        spiritPanel = spirit;
        interactTipUI = interactTip;

        playerMove = player != null ? player.GetComponent<PlayerMove>() : null;
        playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        playerInteraction = player != null ? player.GetComponent<PlayerInteraction>() : null;
        playerInkAttack = player != null ? player.GetComponent<BaseHubInkAttack>() : null;

        ClosePanelsOnly();
    }

    private void Update()
    {
        if (isModalOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAll();
        }
    }

    public void OpenIllustratedHandbook()
    {
        OpenModal(illustratedHandbookPanel);
    }

    public void OpenSpiritPanel()
    {
        OpenModal(spiritPanel != null ? spiritPanel.gameObject : null);
        spiritPanel?.Open();
    }

    public void CloseAll()
    {
        ClosePanelsOnly();
        SetInteractTipVisible(false);
        UnlockPlayer();
        isModalOpen = false;
    }

    private void OpenModal(GameObject panel)
    {
        if (panel == null) return;

        ClosePanelsOnly();
        LockPlayer();
        SetInteractTipVisible(false);
        panel.SetActive(true);
        isModalOpen = true;
    }

    private void ClosePanelsOnly()
    {
        if (illustratedHandbookPanel != null)
            illustratedHandbookPanel.SetActive(false);

        if (spiritPanel != null)
            spiritPanel.gameObject.SetActive(false);
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
}
