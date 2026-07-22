using UnityEngine;

/// <summary>
/// UI manager for catalogue toggling, other UI visibility, and player movement locking.
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
    private bool isClosingHandbook;
    private MonoBehaviour playerMovementScript;
    private bool wasPlayerEnabled = true;
    private Dialog dialogUI;
    private IllustratedHandbookTabsController tabsController;

    public bool IsHandbookOpen => isHandbookOpen;

    private void Awake()
    {
        bool sceneOwnsIllustratedUi = IllustratedUISceneLoader.IsIllustratedUiScene(gameObject.scene);
        bool currentOwnsIllustratedUi = Instance != null &&
                                        IllustratedUISceneLoader.IsIllustratedUiScene(Instance.gameObject.scene);

        if (Instance == null || (sceneOwnsIllustratedUi && !currentOwnsIllustratedUi))
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            UIManager[] managers = FindObjectsOfType<UIManager>(true);
            for (int i = 0; i < managers.Length; i++)
            {
                UIManager manager = managers[i];
                if (manager == null || manager == this)
                {
                    continue;
                }

                if (Instance == null ||
                    IllustratedUISceneLoader.IsIllustratedUiScene(manager.gameObject.scene))
                {
                    Instance = manager;
                    if (IllustratedUISceneLoader.IsIllustratedUiScene(manager.gameObject.scene))
                    {
                        break;
                    }
                }
            }
        }
    }

    private void Start()
    {
        EnsureTabsController();

        if (IllustratedUISceneLoader.IsIllustratedUiScene(gameObject.scene))
        {
            RefreshRuntimeBindings();
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

        isHandbookOpen = false;
        isClosingHandbook = false;
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

        EnsureTabsController();

        if (illustratedHandbook != null)
            illustratedHandbook.SetActive(false);

        if (detailedInformation != null)
            detailedInformation.SetActive(false);

        isHandbookOpen = false;
        isClosingHandbook = false;
        RefreshRuntimeBindings();
    }

    public void OpenIllustratedHandbook(RuntimeModalOpenSource source = RuntimeModalOpenSource.None)
    {
        OpenIllustratedHandbook(source, IllustratedHandbookPage.IllustratedHandbook);
    }

    public void OpenIllustratedHandbook(
        RuntimeModalOpenSource source,
        IllustratedHandbookPage initialPage)
    {
        EnsureTabsController();

        if (isHandbookOpen || isClosingHandbook)
        {
            tabsController?.OpenPage(initialPage);
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

        tabsController?.OpenPage(initialPage);

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.Handbook, source);
        }
        else
        {
            if (illustratedHandbook != null)
                illustratedHandbook.SetActive(true);

            if (detailedInformation != null)
                detailedInformation.SetActive(false);
        }

        if (interactTipUI != null)
        {
            interactTipUI.SetActive(false);
        }
    }

    public void CloseIllustratedHandbook()
    {
        if (isClosingHandbook)
        {
            Debug.Log("图鉴当前正在关闭");
            return;
        }

        if (!isHandbookOpen && !IsHandbookVisible())
        {
            Debug.Log("图鉴当前已处于关闭状态");
            return;
        }

        if (UIRootManager.Instance != null)
        {
            isClosingHandbook = true;
            UIRootManager.Instance.CloseModalFlow(CompleteCloseIllustratedHandbook);
            return;
        }

        CompleteCloseIllustratedHandbook();
    }

    public void RestoreUI()
    {
        if (isClosingHandbook)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsModalFlowOpen)
        {
            isClosingHandbook = true;
            UIRootManager.Instance.CloseModalFlow(CompleteCloseIllustratedHandbook);
            return;
        }

        CompleteCloseIllustratedHandbook();
    }

    private void CompleteCloseIllustratedHandbook()
    {
        tabsController?.ResetToDefaultPage();

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
        isClosingHandbook = false;
    }

    private bool IsHandbookVisible()
    {
        if (illustratedHandbook != null && illustratedHandbook.activeInHierarchy)
        {
            return true;
        }

        if (detailedInformation != null && detailedInformation.activeInHierarchy)
        {
            return true;
        }

        return UIRootManager.Instance != null &&
               UIRootManager.Instance.ActiveModalType == RuntimeModalType.Handbook;
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
        }
    }

    private void EnsureTabsController()
    {
        if (illustratedHandbook == null)
        {
            return;
        }

        if (tabsController != null && tabsController.gameObject == illustratedHandbook)
        {
            return;
        }

        IllustratedHandbookTabsController previousController = tabsController;
        tabsController = IllustratedHandbookTabsController.EnsureInstalled(this);
        if (tabsController != null)
        {
            illustratedHandbook = tabsController.gameObject;
            if (tabsController != previousController)
            {
                tabsController.ResetToDefaultPage();
            }

            UIRootManager.Instance?.RefreshRuntimeBindings();
        }
    }
}
