using UnityEngine;

/// <summary>
/// UI 管理器，负责图鉴开关、其他 UI 显隐与玩家移动锁定。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("图鉴")]
    public GameObject illustratedHandbook;
    public GameObject detailedInformation;

    [Header("打开图鉴时需要隐藏的 UI")]
    public GameObject[] uiToHide;

    [Header("交互提示 UI")]
    public GameObject interactTipUI;

    [Header("玩家控制")]
    public GameObject player;
    public string playerMovementScriptName = "PlayerController";

    private bool isHandbookOpen;
    private MonoBehaviour playerMovementScript;
    private bool wasPlayerEnabled = true;
    private Dialog dialogUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.CloseAllBookUI();
        }

        isHandbookOpen = false;
        RefreshRuntimeBindings();
    }

    public void ConfigureForRuntime(
        GameObject handbook,
        GameObject detail,
        GameObject[] hideTargets,
        GameObject interactTip,
        GameObject playerObject)
    {
        illustratedHandbook = handbook;
        detailedInformation = detail;
        uiToHide = hideTargets;
        interactTipUI = interactTip;
        player = playerObject;

        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        RefreshRuntimeBindings();
    }

    public void OpenIllustratedHandbook()
    {
        if (isHandbookOpen)
        {
            Debug.Log("图鉴已打开，忽略重复打开");
            return;
        }

        isHandbookOpen = true;
        DisablePlayerMovement();
        HideOtherUI(true);

        if (player != null)
        {
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null)
            {
                interaction.ClearCurrentInteractable();
            }
        }

        if (dialogUI == null)
            dialogUI = FindObjectOfType<Dialog>();

        if (dialogUI != null)
        {
            dialogUI.ForceHideImmediately();
            dialogUI.canShow = false;
        }

        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(true);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideAllDetail();
            UIRootManager.Instance.HideAllSubmitSelection();
            UIRootManager.Instance.HideDialog();
            UIRootManager.Instance.ShowHandbook();
            UIRootManager.Instance.HideInteractTip();
        }

        if (interactTipUI != null)
        {
            interactTipUI.SetActive(false);
        }
    }

    public void CloseIllustratedHandbook()
    {
        if (!isHandbookOpen)
        {
            Debug.Log("图鉴当前已处于关闭状态");
            return;
        }

        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.CloseAllBookUI();
        }

        HideOtherUI(false);
        EnablePlayerMovement();

        if (dialogUI == null)
            dialogUI = FindObjectOfType<Dialog>();

        if (dialogUI != null)
        {
            dialogUI.canShow = true;
            dialogUI.ForceHideImmediately();
        }

        if (interactTipUI != null)
        {
            interactTipUI.SetActive(true);
        }

        isHandbookOpen = false;
    }

    public void RestoreUI()
    {
        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        HideOtherUI(false);
        EnablePlayerMovement();

        if (dialogUI == null)
            dialogUI = FindObjectOfType<Dialog>();

        if (dialogUI != null)
        {
            dialogUI.canShow = true;
            dialogUI.ForceHideImmediately();
        }

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.CloseAllBookUI();
        }

        if (interactTipUI != null)
        {
            interactTipUI.SetActive(true);
        }

        isHandbookOpen = false;
    }

    private void DisablePlayerMovement()
    {
        if (playerMovementScript != null)
        {
            wasPlayerEnabled = playerMovementScript.enabled;
            playerMovementScript.enabled = false;
        }

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }
        }
    }

    private void EnablePlayerMovement()
    {
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = wasPlayerEnabled;
        }

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
            }
        }
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

    private void RefreshRuntimeBindings()
    {
        dialogUI = FindObjectOfType<Dialog>();

        if (player != null)
        {
            playerMovementScript = player.GetComponent(playerMovementScriptName) as MonoBehaviour;
            if (playerMovementScript == null)
            {
                playerMovementScript = player.GetComponent<PlayerMove>();
                if (playerMovementScript == null)
                {
                    Debug.LogWarning("未找到玩家移动脚本，请检查 playerMovementScriptName 或 PlayerMove 组件。");
                }
            }
        }
        else
        {
            playerMovementScript = null;
            Debug.LogWarning("UIManager 未绑定玩家对象，无法锁定移动。");
        }
    }
}
