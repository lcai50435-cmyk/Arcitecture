using UnityEngine;
using UnityEngine.UI;

public class SubmitSelectionPanelUI : MonoBehaviour
{
    private const float DoubleClickWindow = 0.32f;

    public GameObject panelRoot;
    public Button handbookCloseButton;
    public SubmitSelectionSlotUI[] slotUIs;

    private BackpackMananger backpack;
    private PlayerGetArchitectural playerGetArchitectural;
    private BackpackUI backpackUI;
    private int selectedIndex = -1;
    private float lastSelectedClickTime = -10f;
    private bool isOpen;
    private CatalogueBuildingId currentTargetBuilding;

    private void Start()
    {
        ResolveRuntimeDependencies();

        if (slotUIs != null)
        {
            for (int i = 0; i < slotUIs.Length; i++)
            {
                if (slotUIs[i] != null)
                {
                    slotUIs[i].Init(this);
                }
            }
        }

        ClosePanelImmediate();
    }

    public void TogglePanelForBuilding(int buildingIndex)
    {
        CatalogueBuildingId target = (CatalogueBuildingId)buildingIndex;
        if (isOpen && currentTargetBuilding == target)
        {
            ClosePanel();
            return;
        }

        currentTargetBuilding = target;
        OpenPanel();
    }

    public void OpenPanel()
    {
        ResolveRuntimeDependencies();
        isOpen = true;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(ResolveModalType(), RuntimeModalOpenSource.None, true);
        }
        else
        {
            SetFallbackPanelVisible(true);
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.interactable = false;
            handbookCloseButton.gameObject.SetActive(false);
        }

        selectedIndex = -1;
        lastSelectedClickTime = -10f;
        RefreshPanel();
    }

    public void ClosePanel()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
        }
        else
        {
            SetFallbackPanelVisible(false);
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.gameObject.SetActive(true);
            handbookCloseButton.interactable = true;
        }

        selectedIndex = -1;
        lastSelectedClickTime = -10f;
        RefreshPanel();
    }

    private void ClosePanelImmediate()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            if (root != null)
            {
                root.SetActive(false);
            }
        }
        else
        {
            SetFallbackPanelVisible(false);
        }

        if (handbookCloseButton != null)
        {
            handbookCloseButton.gameObject.SetActive(true);
            handbookCloseButton.interactable = true;
        }

        selectedIndex = -1;
        lastSelectedClickTime = -10f;
    }

    public void OnSlotPressed(int slotIndex, int clickCount)
    {
        ResolveRuntimeDependencies();
        if (backpack == null)
        {
            return;
        }

        ArchitecturalCrystal? nullableItem = backpack.GetItem(slotIndex);
        if (!nullableItem.HasValue)
        {
            selectedIndex = -1;
            lastSelectedClickTime = -10f;
            RefreshPanel();
            return;
        }

        float now = Time.unscaledTime;
        bool isSameSlot = selectedIndex == slotIndex;
        bool isDoubleClick = clickCount >= 2 ||
                             (isSameSlot && now - lastSelectedClickTime <= DoubleClickWindow);

        selectedIndex = slotIndex;
        lastSelectedClickTime = now;
        RefreshPanel();

        if (!isDoubleClick)
        {
            return;
        }

        if (playerGetArchitectural != null)
        {
            playerGetArchitectural.SubmitSingleItemToBuilding(slotIndex, currentTargetBuilding);
        }

        selectedIndex = -1;
        lastSelectedClickTime = -10f;
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        RefreshPanel();
    }

    public void RefreshPanel()
    {
        ResolveRuntimeDependencies();
        if (backpack == null)
        {
            Debug.LogError("SubmitSelectionPanelUI missing BackpackMananger");
            return;
        }

        if (slotUIs == null)
        {
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            SubmitSelectionSlotUI slot = slotUIs[i];
            if (slot == null)
            {
                continue;
            }

            int realIndex = slot.slotIndex;
            ArchitecturalCrystal? nullableItem = backpack.GetItem(realIndex);
            if (nullableItem.HasValue && nullableItem.Value.IsCommonStructure)
            {
                slot.Refresh(nullableItem.Value, true, selectedIndex == realIndex);
                continue;
            }

            slot.Refresh(default, false, false);
        }
    }

    private void ResolveRuntimeDependencies()
    {
        if (backpack == null)
        {
            backpack = BackpackMananger.Instance;
        }

        if (backpack == null)
        {
            GameObject manager = new GameObject("RuntimeBackpackManager");
            backpack = manager.AddComponent<BackpackMananger>();
        }

        if (playerGetArchitectural == null)
        {
            playerGetArchitectural = FindObjectOfType<PlayerGetArchitectural>();
        }

        if (backpackUI == null)
        {
            backpackUI = FindObjectOfType<BackpackUI>(true);
        }

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }

    private void SetFallbackPanelVisible(bool visible)
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        GameObject packBagCanvasRoot = FindPackBagCanvasRoot();
        if (visible)
        {
            if (packBagCanvasRoot != null)
            {
                packBagCanvasRoot.SetActive(true);
            }

            SubmitSelectionPanelUI[] allPanels = FindObjectsOfType<SubmitSelectionPanelUI>(true);
            for (int i = 0; i < allPanels.Length; i++)
            {
                if (allPanels[i] == null || allPanels[i].panelRoot == null)
                {
                    continue;
                }

                allPanels[i].panelRoot.SetActive(allPanels[i] == this);
            }
        }
        else
        {
            panelRoot.SetActive(false);
            if (packBagCanvasRoot != null && !HasAnyVisibleSubmitPanel(packBagCanvasRoot.transform))
            {
                packBagCanvasRoot.SetActive(false);
            }
        }
    }

    private GameObject FindPackBagCanvasRoot()
    {
        Transform current = panelRoot != null ? panelRoot.transform : transform;
        while (current != null)
        {
            if (current.name == "PackBagCanvas")
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private static bool HasAnyVisibleSubmitPanel(Transform root)
    {
        SubmitSelectionPanelUI[] panels = root.GetComponentsInChildren<SubmitSelectionPanelUI>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].panelRoot != null && panels[i].panelRoot.activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    private RuntimeModalType ResolveModalType()
    {
        switch (currentTargetBuilding)
        {
            case CatalogueBuildingId.Building1:
                return RuntimeModalType.SubmitSelection1;
            case CatalogueBuildingId.Building2:
                return RuntimeModalType.SubmitSelection2;
            case CatalogueBuildingId.Building3:
                return RuntimeModalType.SubmitSelection3;
            default:
                return RuntimeModalType.SubmitSelection1;
        }
    }
}
