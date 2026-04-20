using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家捡建筑结构物品
/// </summary>
public class PlayerGetArchitectural : MonoBehaviour
{
    private BackpackMananger backpack;
    private BackpackUI backpackUI;

    private void Start()
    {
        if (BackpackMananger.Instance == null)
        {
            Debug.LogError("背包管理器不存在！请放到场景里");
            return;
        }
        backpack = BackpackMananger.Instance;
        backpackUI = FindObjectOfType<BackpackUI>();
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="crystal"></param>
    public bool PickCrystal(ArchitecturalCrystal crystal)
    {
        // 调用背包的拾取方法，成功则刷新UI
        if (backpack.PickItem(crystal))
        {
            backpackUI.RefreshUI();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 提交所有构建度给基地
    /// </summary>
    public void SubmitAllCachedExp()
    {
        if (backpack.GetOccupiedCount() == 0)
        {
            Debug.Log("背包为空，无需上交！");
            return;
        }

        foreach (var item in backpack.backpackItems)
        {
            if (item == null) continue;

            if (item.isUnlockMaterial)
            {
                continue;
            }

            ExperienceManager.Instance.AddExperience(item.type, item.expValue);
        }

        backpack.ClearAllItems();
        backpackUI.RefreshUI();
    }

    public void SubmitSingleItem(int index)
    {
        if (backpack == null) return;

        ArchitecturalCrystal item = backpack.GetItem(index);
        if (item == null)
        {
            Debug.Log("该格子没有物品，无法提交");
            return;
        }

        // 专用点亮道具：禁止提交
        if (item.isUnlockMaterial)
        {
            Debug.Log("专用点亮道具不能提交，只能用于点亮");
            return;
        }

        ExperienceManager.Instance.AddExperience(item.type, item.expValue);

        backpack.RemoveItem(index);

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        Debug.Log($"已提交物品：{item.type}");
    }
    /// <summary>
    /// 消耗一个专用点亮道具
    /// 成功返回 true，失败返回 false
    /// </summary>
    public bool ConsumeOneUnlockMaterial()
    {
        if (backpack == null) return false;

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            ArchitecturalCrystal item = backpack.backpackItems[i];
            if (item != null && item.isUnlockMaterial)
            {
                backpack.RemoveItem(i);

                if (backpackUI != null)
                {
                    backpackUI.RefreshUI();
                }

                Debug.Log("成功消耗一个专用点亮道具");
                return true;
            }
        }

        Debug.Log("背包中没有专用点亮道具");
        return false;
    }
    public void SubmitSingleItemToBuilding(int index, CatalogueBuildingId buildingId)
    {
        if (backpack == null) return;

        ArchitecturalCrystal item = backpack.GetItem(index);
        if (item == null)
        {
            Debug.Log("该格子没有物品，无法提交");
            return;
        }

        // 专用点亮道具：禁止提交
        if (item.isUnlockMaterial)
        {
            Debug.Log("专用点亮道具不能提交，只能用于点亮");
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
            Debug.LogError($"没有找到建筑 {buildingId} 的 BuildingProgressController");
            return;
        }

        // 关键修复：进度条满了就不能再提交
        if (targetController.IsFull())
        {
            Debug.Log($"建筑 {buildingId} 的进度已满，不能再提交物品");
            return;
        }

        targetController.AddProgress(item.expValue);

        backpack.RemoveItem(index);

        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }

        Debug.Log($"已提交物品：{item.type} -> 目标建筑：{buildingId}");
    }
}
   
