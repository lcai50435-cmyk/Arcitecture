using UnityEngine;
using UnityEngine.UI;

public class SubmitSelectionPanelUI : MonoBehaviour
{
    [Header("窗口根物体")]
    public GameObject panelRoot;

    [Header("图鉴主页关闭按钮")]
    public Button handbookCloseButton;

    [Header("6个槽位")]
    public SubmitSelectionSlotUI[] slotUIs;

    private BackpackMananger backpack;
    private PlayerGetArchitectural playerGetArchitectural;
    private BackpackUI backpackUI;

    private int selectedIndex = -1;
    private bool isOpen = false;

    // 当前提交目标建筑
    private CatalogueBuildingId currentTargetBuilding;

    private void Start()
    {
        backpack = BackpackMananger.Instance;
        playerGetArchitectural = FindObjectOfType<PlayerGetArchitectural>();
        backpackUI = FindObjectOfType<BackpackUI>();

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].Init(this);
            }
            else
            {
                Debug.LogWarning($"slotUIs[{i}] 没有绑定");
            }
        }

        ClosePanelImmediate();
    }

    /// <summary>
    /// 指定建筑打开/关闭窗口
    /// </summary>
    public void TogglePanelForBuilding(int buildingIndex)
    {
        CatalogueBuildingId target = (CatalogueBuildingId)buildingIndex;

        // 同一个建筑再次点击：关闭
        if (isOpen && currentTargetBuilding == target)
        {
            ClosePanel();
            return;
        }

        // 切换到另一个建筑窗口
        currentTargetBuilding = target;
        OpenPanel();
    }

    /// <summary>
    /// 打开提交窗口
    /// </summary>
    public void OpenPanel()
    {
        isOpen = true;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.ShowSubmitSelection((int)currentTargetBuilding);
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.interactable = false;
            handbookCloseButton.gameObject.SetActive(false);
        }

        selectedIndex = -1;
        RefreshPanel();

        Debug.Log($"打开提交窗口，当前目标建筑：{currentTargetBuilding}");
    }

    /// <summary>
    /// 关闭提交窗口
    /// </summary>
    public void ClosePanel()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideSubmitSelection((int)currentTargetBuilding);
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.gameObject.SetActive(true);
            handbookCloseButton.interactable = true;
        }

        selectedIndex = -1;
        RefreshPanel();
    }

    /// <summary>
    /// 初始化时直接关闭全部提交窗口
    /// </summary>
    private void ClosePanelImmediate()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideAllSubmitSelection();
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.gameObject.SetActive(true);
            handbookCloseButton.interactable = true;
        }

        selectedIndex = -1;
    }

    /// <summary>
    /// 点击某个背包槽位
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (backpack == null) return;

        ArchitecturalCrystal item = backpack.GetItem(slotIndex);
        if (item == null)
        {
            Debug.Log($"点击的背包索引 {slotIndex} 是空的");
            return;
        }

        // 第一次点击：选中
        if (selectedIndex != slotIndex)
        {
            selectedIndex = slotIndex;
            Debug.Log($"第一次点击，选中背包索引：{slotIndex}");
            return;
        }

        // 第二次点击同一个：提交给当前建筑
        if (playerGetArchitectural != null)
        {
            playerGetArchitectural.SubmitSingleItemToBuilding(slotIndex, currentTargetBuilding);
        }

        selectedIndex = -1;

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        RefreshPanel();
    }

    /// <summary>
    /// 刷新窗口显示
    /// </summary>
    public void RefreshPanel()
    {
        if (backpack == null)
        {
            Debug.LogError("backpack 为空");
            return;
        }

        Debug.Log($"刷新提交窗口，背包数量：{backpack.GetOccupiedCount()}");

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
            {
                Debug.LogWarning($"slotUIs[{i}] 没有绑定");
                continue;
            }

            int realIndex = slotUIs[i].slotIndex;
            ArchitecturalCrystal item = backpack.GetItem(realIndex);

            Debug.Log($"窗口格子 {slotUIs[i].gameObject.name} -> 背包索引 {realIndex} -> {(item == null ? "空" : item.type.ToString())}");

            slotUIs[i].Refresh(item);
        }
    }
}