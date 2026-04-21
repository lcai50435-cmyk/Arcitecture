using UnityEngine;
using UnityEngine.UI;

public class SubmitSelectionPanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button handbookCloseButton;
    public SubmitSelectionSlotUI[] slotUIs;

    private BackpackMananger backpack;
    private PlayerGetArchitectural playerGetArchitectural;
    private BackpackUI backpackUI;
    private int selectedIndex = -1;
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
            UIRootManager.Instance.ShowSubmitSelection((int)currentTargetBuilding);
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
        RefreshPanel();
        Debug.Log($"Open submit panel for {currentTargetBuilding}");
    }

    public void ClosePanel()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideSubmitSelection((int)currentTargetBuilding);
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
        RefreshPanel();
    }

    private void ClosePanelImmediate()
    {
        isOpen = false;

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideAllSubmitSelection();
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
    }

    public void OnSlotClicked(int slotIndex)
    {
        ResolveRuntimeDependencies();
        if (backpack == null)
        {
            return;
        }

        ArchitecturalCrystal? nullableItem = backpack.GetItem(slotIndex);
        if (!nullableItem.HasValue)
        {
            Debug.Log($"Slot {slotIndex} is empty");
            return;
        }

        if (selectedIndex != slotIndex)
        {
            selectedIndex = slotIndex;
            Debug.Log($"Selected backpack slot {slotIndex}");
            return;
        }

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
            ArchitecturalCrystal item = nullableItem.HasValue ? nullableItem.Value : default;
            slot.Refresh(item, nullableItem.HasValue);
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
            Debug.Log("Created runtime BackpackMananger for submit selection");
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
}
