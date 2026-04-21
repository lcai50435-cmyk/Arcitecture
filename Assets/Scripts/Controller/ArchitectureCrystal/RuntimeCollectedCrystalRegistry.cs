using System.Collections.Generic;
using UnityEngine;

public class RuntimeCollectedCrystalRegistry : MonoBehaviour
{
    public static RuntimeCollectedCrystalRegistry Instance { get; private set; }

    private readonly HashSet<string> collectedCrystalIds = new HashSet<string>();

    public static RuntimeCollectedCrystalRegistry EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimeCollectedCrystalRegistry existing = FindObjectOfType<RuntimeCollectedCrystalRegistry>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimeCollectedCrystalRegistry");
        Instance = runtimeObject.AddComponent<RuntimeCollectedCrystalRegistry>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsCollected(string crystalId)
    {
        return !string.IsNullOrEmpty(crystalId) && collectedCrystalIds.Contains(crystalId);
    }

    public void MarkCollected(string crystalId)
    {
        if (string.IsNullOrEmpty(crystalId))
        {
            return;
        }

        collectedCrystalIds.Add(crystalId);
    }

    public void Clear()
    {
        collectedCrystalIds.Clear();
    }
}
