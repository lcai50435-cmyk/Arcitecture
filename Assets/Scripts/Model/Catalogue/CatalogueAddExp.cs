using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建筑图录
/// </summary>
public class CatalogueAddExp : MonoBehaviour
{
    private Dictionary<ArchitecturalType, int> expDict = new Dictionary<ArchitecturalType, int>();  // 每个建筑结构物品的构建度

    /// <summary>
    /// 对应建筑结构增加构建度
    /// </summary>
    /// <param name="type"></param>
    /// <param name="newExperience"></param>
    private void HandleExperienceChange(ArchitecturalType type, int newExperience)
    {
        if (expDict.ContainsKey(type))
        {
            expDict[type] += newExperience;
            Debug.Log($"基地收到：{type} +{newExperience}，当前总量：{expDict[type]}");
        }
    }

    private void Start()
    {    
        // 初始化所有类型的经验为0
        foreach (ArchitecturalType type in System.Enum.GetValues(typeof(ArchitecturalType)))
        {
            expDict[type] = 0;
        }

        ExperienceManager.Instance.OnExperienceChange += HandleExperienceChange;
    } 
}
