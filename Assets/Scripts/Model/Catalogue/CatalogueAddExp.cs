using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Building catalogue progress data
/// Accumulates build progress by building type and exposes total progress for UI reads
/// </summary>
public class CatalogueAddExp : MonoBehaviour
{
    [Header("总进度上限")]
    public int totalMaxProgress = 100;

    // Build progress for each building structure item
    private Dictionary<ArchitecturalType, int> expDict = new Dictionary<ArchitecturalType, int>();

    /// <summary>
    /// Notifies UI refresh when progress changes
    /// </summary>
    public event Action OnProgressChanged;

    private void Start()
    {
        // Initialize every type's experience to 0
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
    /// Adds build progress for the matching building structure
    /// </summary>
    private void HandleExperienceChange(ArchitecturalType type, int newExperience)
    {
        if (!expDict.ContainsKey(type))
        {
            expDict[type] = 0;
        }

        expDict[type] += newExperience;

        Debug.Log($"基地收到：{type} +{newExperience}，当前总量：{expDict[type]}");

        // Notify UI to refresh
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Gets the current build progress for a type
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
    /// Gets total build progress (Gold + Green + White)
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
    /// Gets total progress clamped to the maximum cap
    /// </summary>
    public int GetClampedTotalProgress()
    {
        return Mathf.Clamp(GetTotalProgress(), 0, totalMaxProgress);
    }
}
