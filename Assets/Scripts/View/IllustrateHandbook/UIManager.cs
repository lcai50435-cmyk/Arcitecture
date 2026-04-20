using UnityEngine;

/// <summary>
/// UI管理器 - 控制图鉴开关、其他UI隐藏、玩家移动控制
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("界面")]
    public GameObject illustratedHandbook; 
    public GameObject detailedInformation;

    [Header("需要隐藏的其他UI")]
    public GameObject[] uiToHide;

    [Header("交互提示UI")]
    public GameObject interactTipUI;

    [Header("玩家控制")]
    public GameObject player;
    public string playerMovementScriptName = "PlayerController";

    private bool isHandbookOpen = false;
    private MonoBehaviour playerMovementScript;
    private bool wasPlayerEnabled = true;

    private Dialog dialogUI;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);
        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        dialogUI = FindObjectOfType<Dialog>();

        if (player != null)
        {
            playerMovementScript = player.GetComponent(playerMovementScriptName) as MonoBehaviour;
            if (playerMovementScript == null)
            {
                playerMovementScript = player.GetComponent<PlayerMove>();
                if (playerMovementScript == null)
                {
                    Debug.LogWarning("未找到玩家移动脚本，请检查脚本名称是否正确");
                }
            }
        }
        else
        {
            Debug.LogWarning("UIManager: 未拖入玩家物体，无法控制玩家移动");
        }
    }

    public void OpenIllustratedHandbook()
    {
        if (isHandbookOpen) return;

        isHandbookOpen = true;

        DisablePlayerMovement();
        HideOtherUI(true);

        if (interactTipUI != null)
            interactTipUI.SetActive(false);

        if (player != null)
        {
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction != null)
            {
                interaction.ClearCurrentInteractable();
            }
        }

        // 图鉴打开时：强制关闭弹窗，并禁用普通弹窗
        if (dialogUI == null)
            dialogUI = FindObjectOfType<Dialog>();

        if (dialogUI != null)
        {
            dialogUI.ForceHideImmediately();
            dialogUI.canShow = false;
        }

        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(true);

        Debug.Log("打开图鉴，玩家移动已禁用");
    }

    public void CloseIllustratedHandbook()
    {
        if (!isHandbookOpen) return;

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

        isHandbookOpen = false;

        Debug.Log("关闭图鉴，玩家移动已恢复");
    }

    public void RestoreUI()
    {
        HideOtherUI(false);
        EnablePlayerMovement();

        if (dialogUI == null)
            dialogUI = FindObjectOfType<Dialog>();

        if (dialogUI != null)
        {
            dialogUI.canShow = true;
            dialogUI.ForceHideImmediately();
        }

        if (isHandbookOpen)
        {
            isHandbookOpen = false;
        }
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
        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(!hide);
            }
        }
    }
}