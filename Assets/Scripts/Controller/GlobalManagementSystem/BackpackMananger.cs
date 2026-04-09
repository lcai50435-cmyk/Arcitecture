using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// 专门存储背包物品数据的核心类
/// 所有物品的拾取/删除/获取都通过此类操作
/// </summary>
public class BackpackMananger : MonoBehaviour
{
    public static BackpackMananger Instance; // 单例，全局可调用
    [HideInInspector] public List<ArchitecturalCrystal> backpackItems = new List<ArchitecturalCrystal>(); // 存储物品完整数据

    private int maxCapacity = 6; // 固定6个格子

    private HashSet<ArchitecturalType> alreadyPickedTypes = new HashSet<ArchitecturalType>(); // 记录是否第一次捡起该类型物品

    public delegate void FirstPickTipEvent(string textDescription); // 第一次捡起某类型物品触发事件
    public event FirstPickTipEvent OnFirstPick; // 第一次捡起事件

    /// <summary>
    /// 创建背包单例
    /// </summary>
    private void Awake()
    {
        // 单例初始化：确保全局只有一个背包实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region 判断是否第一次捡起物品
    /// <summary>
    /// 检查是否第一次拾取该类型物品
    /// </summary>
    public bool IsFirstPick(ArchitecturalType type)
    {
        // 玩家第一次捡起物品
        if (!alreadyPickedTypes.Contains(type))
        {
            alreadyPickedTypes.Add(type);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 触发第一次拾取的介绍事件
    /// </summary>
    public void TriggerFirstPickTip(string desc)
    {
        OnFirstPick?.Invoke(desc);
    }
    #endregion

    #region 对外API：物品操作（拾取/删除/获取）
    /// <summary>
    /// 拾取物品加入背包
    /// </summary>
    /// <returns>是否拾取成功（满了则失败）</returns>
    public bool PickItem(ArchitecturalCrystal crystal)
    {
        if (backpackItems.Count >= maxCapacity)
        {
            Debug.LogWarning("背包已满，无法拾取！");
            return false;
        }

        // 第一次捡起物品触发
        if (!alreadyPickedTypes.Contains(crystal.type))
        {
            alreadyPickedTypes.Add(crystal.type);

            // 发送第一次捡起物品的信息 // 尤其调用该物品的简介
            OnFirstPick?.Invoke(crystal.textDescription);
        }

        // 深拷贝物品数据，避免原物体销毁导致数据丢失
        ArchitecturalCrystal newItem = new ArchitecturalCrystal(
            crystal.type,
            crystal.expValue,
            crystal.icon,
            crystal.backIcon,
            crystal.textDescription
        );
        backpackItems.Add(newItem);
        Debug.Log($"拾取{newItem.type}，当前背包数量：{backpackItems.Count}");
        return true;
    }

    /// <summary>
    /// 删除指定索引的物品
    /// </summary>
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            backpackItems.RemoveAt(index);
            Debug.Log($"删除第{index}个格子物品，剩余：{backpackItems.Count}");
        }
    }

    /// <summary>
    /// 获取指定索引的物品
    /// </summary>
    public ArchitecturalCrystal GetItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            return backpackItems[index];
        }
        return null;
    }

    /// <summary>
    /// 清空所有物品（上交基地时调用）
    /// </summary>
    public void ClearAllItems()
    {
        backpackItems.Clear();
        Debug.Log("背包已清空");
    }
    #endregion
}
