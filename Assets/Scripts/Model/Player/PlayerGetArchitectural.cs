using UnityEngine;

public class PlayerGetArchitectural : MonoBehaviour
{
    private BackpackMananger backpack;
    private BackpackUI backpackUI;

    private void Start()
    {
        ResolveRuntimeDependencies();
    }

    public bool PickCrystal(ArchitecturalCrystal crystal)
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return false;
        }

        if (!backpack.PickItem(crystal))
        {
            return false;
        }

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        return true;
    }

    public void SubmitAllCachedExp()
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return;
        }

        if (backpack.GetOccupiedCount() == 0)
        {
            Debug.Log("Backpack is empty");
            return;
        }

        foreach (ArchitecturalCrystal? item in backpack.backpackItems)
        {
            if (!item.HasValue)
            {
                continue;
            }

            ArchitecturalCrystal crystal = item.Value;
            if (crystal.isUnlockMaterial)
            {
                continue;
            }

            ExperienceManager.Instance.AddExperience(crystal.type, crystal.expValue);
        }

        backpack.ClearAllItems();
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
    }

    public void SubmitSingleItem(int index)
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return;
        }

        ArchitecturalCrystal? item = backpack.GetItem(index);
        if (!item.HasValue)
        {
            return;
        }

        ArchitecturalCrystal crystal = item.Value;
        if (crystal.isUnlockMaterial)
        {
            return;
        }

        ExperienceManager.Instance.AddExperience(crystal.type, crystal.expValue);
        backpack.RemoveItem(index);
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
    }

    public bool ConsumeOneUnlockMaterial()
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return false;
        }

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            if (!backpack.backpackItems[i].HasValue || !backpack.backpackItems[i].Value.isUnlockMaterial)
            {
                continue;
            }

            backpack.RemoveItem(i);
            if (backpackUI != null)
            {
                backpackUI.RefreshUI();
            }

            return true;
        }

        return false;
    }

    public void SubmitSingleItemToBuilding(int index, CatalogueBuildingId buildingId)
    {
        if (!ResolveRuntimeDependencies() || backpack == null)
        {
            return;
        }

        ArchitecturalCrystal? item = backpack.GetItem(index);
        if (!item.HasValue)
        {
            return;
        }

        ArchitecturalCrystal crystal = item.Value;
        if (crystal.isUnlockMaterial)
        {
            return;
        }

        BuildingProgressController[] allControllers = FindObjectsOfType<BuildingProgressController>();
        BuildingProgressController targetController = null;
        for (int i = 0; i < allControllers.Length; i++)
        {
            if (allControllers[i].buildingId == buildingId)
            {
                targetController = allControllers[i];
                break;
            }
        }

        if (targetController == null)
        {
            Debug.LogError($"Missing BuildingProgressController for {buildingId}");
            return;
        }

        if (targetController.IsFull())
        {
            return;
        }

        targetController.AddProgress(crystal.expValue);
        backpack.RemoveItem(index);
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
    }

    private bool ResolveRuntimeDependencies()
    {
        if (backpack == null)
        {
            backpack = BackpackMananger.Instance;
        }

        if (backpack == null)
        {
            GameObject manager = new GameObject("RuntimeBackpackManager");
            backpack = manager.AddComponent<BackpackMananger>();
            Debug.Log("Created runtime BackpackMananger for PlayerGetArchitectural");
        }

        if (backpackUI == null)
        {
            backpackUI = FindObjectOfType<BackpackUI>(true);
        }

        if (backpack == null)
        {
            Debug.LogError("PlayerGetArchitectural missing BackpackMananger");
            return false;
        }

        return true;
    }
}
