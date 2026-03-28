using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 经验管理
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance;

    public delegate void ExperienceChangeHandler(ArchitecturalType type, int value);
    public event ExperienceChangeHandler OnExperienceChange;

    // 单例模式
    private void Awake()
    {      
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

    public void AddExperience(ArchitecturalType type, int value)
    {
        // 防止经验值为0
        if (value < 0)
        {
            Debug.LogError("经验值不能为负");
            return;
        }
        OnExperienceChange?.Invoke(type, value);
    }

}
