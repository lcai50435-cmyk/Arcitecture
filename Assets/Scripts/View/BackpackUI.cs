using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仅负责背包格子的图片显示/隐藏
/// 数据全部从PlayerBackpack获取，无存储逻辑
/// </summary>
public class BackpackUI : MonoBehaviour
{
    [Header("背包格子（拖入6个Image组件）")]
    public Image[] backPackGrid;
    private BackpackMananger backpack;

    private void Start()
    {
        backpack = BackpackMananger.Instance;
        RefreshUI();
    }

    /// <summary>
    /// 纯UI渲染：根据背包数据更新格子图片
    /// </summary>
    public void RefreshUI()
    {
        for (int i = 0; i < backPackGrid.Length; i++)
        {
            Image image = backPackGrid[i];
            if (image == null)
            {
                Debug.LogError($"第{i}个背包格子缺少Image组件！");
                continue;
            }

            // ======================
            // 关键修改：可空结构体
            // ======================
            ArchitecturalCrystal? item = backpack.GetItem(i);

            if (item.HasValue)
            {
                // 有物品：显示背包图标
                ArchitecturalCrystal crystal = item.Value;
                image.sprite = crystal.backIcon;
                image.enabled = true;
            }
            else
            {
                // 无物品：清空图片并隐藏
                image.sprite = null;
                image.enabled = false;
            }
        }
    }
}