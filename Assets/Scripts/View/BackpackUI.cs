using UnityEngine;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    public Image[] backPackGrid;
    private BackpackMananger backpack;
    private bool subscribedToRuntimeState;

    private void Start()
    {
        ResolveBackpackManager();
        RefreshUI();
    }

    private void OnEnable()
    {
        ResolveBackpackManager();
        SubscribeRuntimeEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeRuntimeEvents();
    }

    public void RefreshUI()
    {
        ResolveBackpackManager();

        if (backPackGrid == null)
        {
            return;
        }

        int specialInventory = RuntimeProgressState.EnsureInstance().AvailableSpecialStructureInventory;

        for (int i = 0; i < backPackGrid.Length; i++)
        {
            Image image = backPackGrid[i];
            if (image == null)
            {
                continue;
            }

            ArchitecturalCrystal? item = backpack.GetItem(i);
            if (item.HasValue)
            {
                ArchitecturalCrystal crystal = item.Value;
                image.sprite = crystal.backIcon;
                image.color = Color.white;
                image.enabled = true;
            }
            else if (specialInventory > 0)
            {
                ArchitecturalCrystal specialCrystal = ArchitecturalCrystalFactory.CreateSpecialStructureMaterial();
                image.sprite = specialCrystal.backIcon;
                image.color = Color.white;
                image.enabled = image.sprite != null;
                specialInventory--;
            }
            else
            {
                image.sprite = null;
                image.color = Color.white;
                image.enabled = false;
            }
        }
    }

    private void ResolveBackpackManager()
    {
        if (backpack == null)
        {
            backpack = BackpackMananger.Instance;
        }

        if (backpack == null)
        {
            GameObject manager = new GameObject("RuntimeBackpackManager");
            backpack = manager.AddComponent<BackpackMananger>();
            Debug.Log("Created runtime BackpackMananger for BackpackUI");
        }
    }

    private void SubscribeRuntimeEvents()
    {
        if (backpack != null)
        {
            backpack.OnInventoryChanged -= RefreshUI;
            backpack.OnInventoryChanged += RefreshUI;
        }

        if (!subscribedToRuntimeState)
        {
            RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshUI;
            subscribedToRuntimeState = true;
        }
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (backpack != null)
        {
            backpack.OnInventoryChanged -= RefreshUI;
        }

        if (subscribedToRuntimeState && RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshUI;
            subscribedToRuntimeState = false;
        }
    }
}
