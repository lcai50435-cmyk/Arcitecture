using UnityEngine;

/// <summary>
/// 仅负责背包格子的图片显示/隐藏
/// 数据全部从PlayerBackpack获取，无存储逻辑
/// </summary>
public class BackpackUI : MonoBehaviour
{
    [Header("背包格子（拖入6个空GameObject）")]
    public GameObject[] backPackGrid; // 你的6个格子物体
    private BackpackMananger _backpack;

    private void Start()
    {
        _backpack = BackpackMananger.Instance;
        // 初始化UI（防止场景启动时格子有残留图片）
        RefreshUI();
    }

    /// <summary>
    /// 纯UI渲染：根据背包数据更新格子图片
    /// </summary>
    public void RefreshUI()
    {
        for (int i = 0; i < backPackGrid.Length; i++)
        {
            SpriteRenderer sr = backPackGrid[i].GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError($"第{i}个背包格子缺少SpriteRenderer组件！");
                continue;
            }

            ArchitecturalCrystal item = _backpack.GetItem(i);
            if (item != null)
            {
                // 有物品：显示背图标
                sr.sprite = item.backIcon;
                sr.enabled = true;
            }
            else
            {
                // 无物品：清空图片并隐藏
                sr.sprite = null;
                sr.enabled = false;
            }
        }
    }
}