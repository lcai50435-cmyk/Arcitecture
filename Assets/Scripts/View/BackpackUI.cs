using UnityEngine;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    public Image[] backPackGrid;
    private BackpackMananger backpack;

    private void Start()
    {
        backpack = BackpackMananger.Instance;
        RefreshUI();
    }

    public void RefreshUI()
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

        if (backPackGrid == null)
        {
            return;
        }

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
                image.enabled = true;
            }
            else
            {
                image.sprite = null;
                image.enabled = false;
            }
        }
    }
}
