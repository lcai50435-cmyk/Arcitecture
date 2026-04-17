using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建筑图录进度数据
/// 负责累计不同建筑类型的构建度，并提供总进度给UI读取
/// </summary>
public class CatalogueAddExp : MonoBehaviour
{
    [Header("总进度上限")]
    public int totalMaxProgress = 100;

    // 每个建筑结构物品的构建度
    private Dictionary<ArchitecturalType, int> expDict = new Dictionary<ArchitecturalType, int>();

    /// <summary>
    /// 当进度变化时，通知UI刷新
    /// </summary>
    public event Action OnProgressChanged;

    private void Start()
    {
        // 初始化所有类型的经验为0
        foreach (ArchitecturalType type in Enum.GetValues(typeof(ArchitecturalType)))
        {
            expDict[type] = 0;
        }

        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChange += HandleExperienceChange;
        }
        else
        {
            Debug.LogError("ExperienceManager.Instance 不存在，无法监听经验变化！");
        }
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnExperienceChange -= HandleExperienceChange;
        }
    }

    /// <summary>
    /// 对应建筑结构增加构建度
    /// </summary>
    private void HandleExperienceChange(ArchitecturalType type, int newExperience)
    {
        if (!expDict.ContainsKey(type))
        {
            expDict[type] = 0;
        }

        expDict[type] += newExperience;

        Debug.Log($"基地收到：{type} +{newExperience}，当前总量：{expDict[type]}");

        // 通知UI刷新
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// 获取某个类型当前的构建度
    /// </summary>
    public int GetProgress(ArchitecturalType type)
    {
        if (expDict.TryGetValue(type, out int value))
        {
            return value;
        }

        return 0;
    }

    /// <summary>
    /// 获取总构建度（Gold + Green + White）
    /// </summary>
    public int GetTotalProgress()
    {
        int total = 0;

        foreach (var kv in expDict)
        {
            total += kv.Value;
        }

        return total;
    }

    /// <summary>
    /// 获取限制在总上限内的总进度
    /// </summary>
    public int GetClampedTotalProgress()
    {
        return Mathf.Clamp(GetTotalProgress(), 0, totalMaxProgress);
    }
}