using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// 玩家捡建筑结构物品
/// </summary>
public class PlayerGetArchitectural : MonoBehaviour
{
    private Dictionary<ArchitecturalType, int> cachedExpDict = new Dictionary<ArchitecturalType, int>();   // 存储建筑结构构建度

    [Header("背包设置")]
    private int maxBackpackSize = 6; // 背包容量
    public List<ArchitecturalCrystal> backpackItems = new List<ArchitecturalCrystal>();  // 背包物品

    public event System.Action OnBackpackChanged; // 背包事件通知

    /// <summary>
    /// 缓存经验
    /// </summary>
    /// <param name="type"></param>
    /// <param name="value"></param>
    public void CacheExp(ArchitecturalType type, int value)
    {
        // 针对相关建筑物品类型增加对应构建度
        if (cachedExpDict.ContainsKey(type))
            cachedExpDict[type] += value;
        else
            cachedExpDict[type] = value;
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="crystal"></param>
    public void PickCrystal(ArchitecturalCrystal crystal)
    {
        if (backpackItems.Count >= maxBackpackSize)
        {
            Debug.Log("背包满啦！");
            return;
        }

        // 创建数据类
        ArchitecturalCrystal newCrystal = new ArchitecturalCrystal(
            crystal.type,
            crystal.expValue,
            crystal.icon,
            crystal.backIcon,           
            crystal.textDescription
        );

        backpackItems.Add(newCrystal);  // 加入物品至列表
        CacheExp(crystal.type, crystal.expValue);  // 暂时记录经验

        // 通知UI刷新
        OnBackpackChanged?.Invoke();
    }

    /// <summary>
    /// 提交所有构建度给基地
    /// </summary>
    public void SubmitAllCachedExp()
    {
        if (cachedExpDict.Count == 0) return;

        foreach (var pair in cachedExpDict)
        {
            ExperienceManager.Instance.AddExperience(pair.Key, pair.Value);
        }

        cachedExpDict.Clear(); // 提交完清空
        backpackItems.Clear(); // 提交完以后清空背包数据 // 目前提交是一次性提交，所以才是一次性清空数据

        OnBackpackChanged?.Invoke(); // 通知UI图片没有了
    }
   
    /// <summary> 获得背包物品数据 </summary>
    public List<ArchitecturalCrystal> GetBackpackItems() => backpackItems;
}
