using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public delegate void FirstPickTipEvent(ArchitecturalCrystal crystal); // 第一次捡起某类型物品触发事件
    public event FirstPickTipEvent OnFirstTimePickItemType;

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

    #region 对外API：物品操作（拾取/删除/获取）
    /// <summary>
    /// 拾取物品加入背包
    /// 添加对应属性加成
    /// </summary>
    /// <returns>是否拾取成功（满了则失败）</returns>
    public bool PickItem(ArchitecturalCrystal crystal)
    {
        if (backpackItems.Count >= maxCapacity)
        {
            Debug.LogWarning("背包已满，无法拾取！");
            return false;
        }

        bool isFirstPick = !alreadyPickedTypes.Contains(crystal.type);

        // 第一次捡起物品触发
        if (isFirstPick)
        {
            alreadyPickedTypes.Add(crystal.type);
            Debug.Log("玩家第一次捡起该物品");
            // 发送第一次捡起物品的信息 // 尤其调用该物品的简介   
            OnFirstTimePickItemType?.Invoke(crystal);
        }

        // 深拷贝物品数据，避免原物体销毁导致数据丢失
        ArchitecturalCrystal newItem = new ArchitecturalCrystal(
            crystal.type,
            crystal.expValue,
            crystal.icon,
            crystal.backIcon,
            crystal.textDescription,
            crystal.bonusType, // 传递属性加成类型
            crystal.bonusValue // 传递属性加成数值
        );
        backpackItems.Add(newItem);
        Debug.Log($"拾取{newItem.type}，当前背包数量：{backpackItems.Count}");

        // 拾取道具时，根据格子索引添加属性加成
        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.AddBonus(
                newItem.bonusType,
                newItem.bonusValue
            );
        }
       
        return true;
    }

    /// <summary>
    /// 删除指定索引的物品
    /// 扣除对应属性加成
    /// </summary>
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {

            // 移除道具时扣除相应属性加成
            if (PlayerAttributeManager.Instance != null)
            {
                PlayerAttributeManager.Instance.RemoveBonus(backpackItems[index].bonusType, backpackItems[index].bonusValue);
            }

            backpackItems.RemoveAt(index);
            Debug.Log($"移除索引{index}道具，剩余数量{backpackItems.Count}");
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
        //// 扣除所有道具的属性加成
        //if (PlayerAttributeManager.Instance != null)
        //{
        //    PlayerAttributeManager.Instance.ClearAllBonus();
        //}

        backpackItems.Clear();
        Debug.Log("背包已清空");
    }
    #endregion
}

