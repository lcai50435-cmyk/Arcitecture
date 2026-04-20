using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家获取建筑类道具
/// </summary>
public class PlayerGetArchitectural : MonoBehaviour
{
    private BackpackMananger backpack;
    private BackpackUI backpackUI;

    private void Start()
    {
        if (BackpackMananger.Instance == null)
        {
            Debug.LogError("背包管理器不存在，请先创建");
            return;
        }
        backpack = BackpackMananger.Instance;
        backpackUI = FindObjectOfType<BackpackUI>();
    }

    /// <summary>
    /// 拾取道具入口
    /// </summary>
    public bool PickCrystal(ArchitecturalCrystal crystal)
    {
        // 调用背包拾取方法，成功则刷新UI
        if (backpack.PickItem(crystal))
        {
            backpackUI.RefreshUI();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 提交所有缓存经验值
    /// </summary>
    public void SubmitAllCachedExp()
    {
        if (backpack.GetOccupiedCount() == 0)
        {
            Debug.Log("背包为空，无法提交");
            return;
        }

        foreach (var item in backpack.backpackItems)
        {
            // 关键修改：判断可空类型是否有值
            if (!item.HasValue) continue;
            var crystal = item.Value;

            if (crystal.isUnlockMaterial)
            {
                continue;
            }

            ExperienceManager.Instance.AddExperience(crystal.type, crystal.expValue);
        }

        backpack.ClearAllItems();
        backpackUI.RefreshUI();
    }

    public void SubmitSingleItem(int index)
    {
        if (backpack == null) return;

        // 关键修改：接收可空类型
        ArchitecturalCrystal? item = backpack.GetItem(index);
        if (!item.HasValue)
        {
            Debug.Log("该位置没有道具，无法提交");
            return;
        }
        var crystal = item.Value;

        // 专用解锁材料，禁止提交
        if (crystal.isUnlockMaterial)
        {
            Debug.Log("专用解锁材料不能提交，只能用于解锁");
            return;
        }

        ExperienceManager.Instance.AddExperience(crystal.type, crystal.expValue);
        backpack.RemoveItem(index);

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        Debug.Log($"提交道具：{crystal.type}");
    }

    /// <summary>
    /// 消耗一个专用解锁材料
    /// 成功返回 true，失败返回 false
    /// </summary>
    public bool ConsumeOneUnlockMaterial()
    {
        if (backpack == null) return false;

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            // 关键修改：判断可空类型是否有值
            if (backpack.backpackItems[i].HasValue && backpack.backpackItems[i].Value.isUnlockMaterial)
            {
                backpack.RemoveItem(i);

                if (backpackUI != null)
                {
                    backpackUI.RefreshUI();
                }

                Debug.Log("成功消耗一个专用解锁材料");
                return true;
            }
        }

        Debug.Log("背包里没有专用解锁材料");
        return false;
    }

    public void SubmitSingleItemToBuilding(int index, CatalogueBuildingId buildingId)
    {
        if (backpack == null) return;

        // 关键修改：接收可空类型
        ArchitecturalCrystal? item = backpack.GetItem(index);
        if (!item.HasValue)
        {
            Debug.Log("该位置没有道具，无法提交");
            return;
        }
        var crystal = item.Value;

        // 专用解锁材料，禁止提交
        if (crystal.isUnlockMaterial)
        {
            Debug.Log("专用解锁材料不能提交，只能用于解锁");
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
            Debug.LogError($"未找到对应 {buildingId} 的 BuildingProgressController");
            return;
        }

        // 检查进度是否已满，满了则不能提交
        if (targetController.IsFull())
        {
            Debug.Log($"建筑 {buildingId} 的进度已达上限，无法提交道具");
            return;
        }

        targetController.AddProgress(crystal.expValue);
        backpack.RemoveItem(index);

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        Debug.Log($"提交道具：{crystal.type} -> 目标建筑{buildingId}");
    }
}