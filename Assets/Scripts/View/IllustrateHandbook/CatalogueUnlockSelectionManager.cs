using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 图鉴解锁选择管理器
/// 负责记录玩家当前还可以点亮几次
/// </summary>
public class CatalogueUnlockSelectionManager : MonoBehaviour
{
    public static CatalogueUnlockSelectionManager Instance;

    [Header("当前可用点亮次数（运行时观察）")]
    public int availableUnlockCount = 0;

    private readonly HashSet<string> unlockedSlotIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 增加可点亮次数
    /// </summary>
    public void AddUnlockCount(int count)
    {
        if (count <= 0) return;

        availableUnlockCount += count;
        Debug.Log($"增加可点亮次数：+{count}，当前剩余：{availableUnlockCount}");
    }

    /// <summary>
    /// 尝试消耗一次点亮次数并解锁某个槽位
    /// </summary>
    public bool TryUnlockSlot(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
        {
            Debug.LogWarning("slotId 为空，无法解锁");
            return false;
        }

        if (unlockedSlotIds.Contains(slotId))
        {
            Debug.Log($"槽位 {slotId} 已经点亮过了");
            return false;
        }

        if (availableUnlockCount <= 0)
        {
            Debug.Log("当前没有可用点亮次数");
            return false;
        }

        availableUnlockCount--;
        unlockedSlotIds.Add(slotId);

        Debug.Log($"成功点亮槽位：{slotId}，剩余可点亮次数：{availableUnlockCount}");
        return true;
    }

    /// <summary>
    /// 某个槽位是否已经点亮
    /// </summary>
    public bool IsSlotUnlocked(string slotId)
    {
        return !string.IsNullOrEmpty(slotId) && unlockedSlotIds.Contains(slotId);
    }
}