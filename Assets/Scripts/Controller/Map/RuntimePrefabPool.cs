using System.Collections.Generic;
using UnityEngine;

public enum PlaceableCategoryKind
{
    Building, // ½¨Öþ
    Decoration // ×°ÊÎÆ·
}

[System.Serializable]
public class PlaceablePrefabEntry
{
    public GameObject prefab;
    [Min(1)] public int weight = 1;
    public bool enabled = true;

    public bool IsUsable()
    {
        return enabled && prefab != null && weight > 0;
    }
}

[System.Serializable]
public class PlaceableCategoryConfig
{
    public string categoryName = "New Category";
    public PlaceableCategoryKind categoryKind = PlaceableCategoryKind.Decoration;
    [Min(0)] public int placementPriority = 0;
    [Min(0)] public int spawnCount = 0;
    public List<PlaceablePrefabEntry> prefabs = new List<PlaceablePrefabEntry>();
    public List<TerrainType> allowedTerrainTypes = new List<TerrainType> { TerrainType.Grass };
    [Min(1)] public int minimumClusterSize = 1;
    [Min(0)] public int footprintRadius = 0;
    [Min(0)] public int clearanceRadius = 0;
    [Min(0)] public int minDistanceFromBorder = 0;
    public Vector3 worldOffset = Vector3.zero;
    [Min(0f)] public float borderScoreWeight = 4f;
    [Min(0f)] public float clusterCenterPenaltyWeight = 1f;
    [Min(0f)] public float terrainSupportWeight = 1f;
    [Min(0f)] public float randomJitterWeight = 0.35f;

    public string GetDisplayName()
    {
        return string.IsNullOrWhiteSpace(categoryName) ? categoryKind.ToString() : categoryName.Trim();
    }

    public bool HasUsablePrefabs()
    {
        if (prefabs == null)
        {
            return false;
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] != null && prefabs[i].IsUsable())
            {
                return true;
            }
        }

        return false;
    }

    public bool AllowsTerrain(TerrainType terrainType)
    {
        if (allowedTerrainTypes == null)
        {
            return false;
        }

        for (int i = 0; i < allowedTerrainTypes.Count; i++)
        {
            if (allowedTerrainTypes[i] == terrainType)
            {
                return true;
            }
        }

        return false;
    }

    public int GetReservationRadius()
    {
        return Mathf.Max(footprintRadius, clearanceRadius);
    }
}

public sealed class RuntimePrefabPool
{
    private readonly Dictionary<GameObject, Queue<GameObject>> inactiveInstances =
        new Dictionary<GameObject, Queue<GameObject>>();

    private readonly List<PooledRuntimeInstance> activeInstances = new List<PooledRuntimeInstance>();

    private Transform activeRoot;
    private Transform poolRoot;

    public RuntimePrefabPool(Transform activeRoot, Transform poolRoot)
    {
        this.activeRoot = activeRoot;
        this.poolRoot = poolRoot;
    }

    public void SetRoots(Transform activeRoot, Transform poolRoot)
    {
        this.activeRoot = activeRoot;
        this.poolRoot = poolRoot;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = GetOrCreateInstance(prefab);
        Transform targetParent = parent != null ? parent : activeRoot;

        instance.transform.SetParent(targetParent, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = prefab.transform.localScale;
        instance.SetActive(true);

        activeInstances.Add(new PooledRuntimeInstance(prefab, instance));
        return instance;
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < activeInstances.Count; i++)
        {
            Release(activeInstances[i]);
        }

        activeInstances.Clear();
    }

    private GameObject GetOrCreateInstance(GameObject prefab)
    {
        if (inactiveInstances.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            while (queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }
        }

        GameObject created = Object.Instantiate(prefab);
        created.name = prefab.name;
        return created;
    }

    private void Release(PooledRuntimeInstance runtimeInstance)
    {
        GameObject instance = runtimeInstance.instance;
        if (instance == null)
        {
            return;
        }

        if (runtimeInstance.prefab == null || poolRoot == null)
        {
            Object.Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(poolRoot, false);

        if (!inactiveInstances.TryGetValue(runtimeInstance.prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            inactiveInstances.Add(runtimeInstance.prefab, queue);
        }

        queue.Enqueue(instance);
    }

    private readonly struct PooledRuntimeInstance
    {
        public readonly GameObject prefab;
        public readonly GameObject instance;

        public PooledRuntimeInstance(GameObject prefab, GameObject instance)
        {
            this.prefab = prefab;
            this.instance = instance;
        }
    }
}
