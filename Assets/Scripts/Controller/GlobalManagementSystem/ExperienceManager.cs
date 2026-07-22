using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Experience management
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public static ExperienceManager Instance;

    public delegate void ExperienceChangeHandler(ArchitecturalType type, int value);
    public event ExperienceChangeHandler OnExperienceChange;

    // Singleton pattern
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
        // Prevent experience from being 0
        if (value < 0)
        {
            Debug.LogError("经验值不能为负");
            return;
        }
        OnExperienceChange?.Invoke(type, value);
    }

}
