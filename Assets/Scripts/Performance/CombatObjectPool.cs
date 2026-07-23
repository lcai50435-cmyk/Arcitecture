using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public static class CombatObjectPool
{
    private sealed class PoolEntry
    {
        public ObjectPool<GameObject> Pool;
    }

    private static readonly Dictionary<string, PoolEntry> POOLS = new Dictionary<string, PoolEntry>();
    private static bool mbSceneHookRegistered;

    public static GameObject RentPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        if (!Application.isPlaying)
        {
            return UnityEngine.Object.Instantiate(prefab, position, rotation);
        }

        string key = $"prefab:{prefab.GetInstanceID()}";
        PoolEntry entry = getOrCreatePool(
            key,
            () => UnityEngine.Object.Instantiate(prefab),
            GameplayPerformanceSettings.Profile.PrefabPoolDefaultCapacity,
            GameplayPerformanceSettings.Profile.PrefabPoolMaxSize);

        return rent(entry, position, rotation);
    }

    public static GameObject RentRuntime(
        string key,
        Func<GameObject> createFunction,
        Vector3 position,
        Quaternion rotation)
    {
        if (string.IsNullOrWhiteSpace(key) || createFunction == null)
        {
            return null;
        }

        if (!Application.isPlaying)
        {
            GameObject editorInstance = createFunction();
            if (editorInstance != null)
            {
                editorInstance.transform.SetPositionAndRotation(position, rotation);
                editorInstance.SetActive(true);
            }

            return editorInstance;
        }

        string resolvedKey = $"runtime:{key}";
        PoolEntry entry = getOrCreatePool(
            resolvedKey,
            createFunction,
            GameplayPerformanceSettings.Profile.RuntimeFxPoolDefaultCapacity,
            GameplayPerformanceSettings.Profile.RuntimeFxPoolMaxSize);

        return rent(entry, position, rotation);
    }

    public static void ReleaseOrDestroy(GameObject target, float delay = 0f)
    {
        if (target == null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            if (delay <= 0f)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }

            return;
        }

        PooledRuntimeObject pooledObject;
        if (target.TryGetComponent(out pooledObject) && pooledObject.HasPool)
        {
            pooledObject.ReleaseAfter(delay);
            return;
        }

        UnityEngine.Object.Destroy(target, Mathf.Max(0f, delay));
    }

    internal static void ReleaseNow(PooledRuntimeObject pooledObject)
    {
        if (pooledObject == null)
        {
            return;
        }

        PoolEntry entry;
        if (!POOLS.TryGetValue(pooledObject.PoolKey, out entry) || entry == null || entry.Pool == null)
        {
            UnityEngine.Object.Destroy(pooledObject.gameObject);
            return;
        }

        entry.Pool.Release(pooledObject.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetRuntimeState()
    {
        clearAllPools();
        mbSceneHookRegistered = false;
        ensureSceneHook();
    }

    private static PoolEntry getOrCreatePool(
        string key,
        Func<GameObject> createFunction,
        int defaultCapacity,
        int maxSize)
    {
        ensureSceneHook();

        PoolEntry existing;
        if (POOLS.TryGetValue(key, out existing))
        {
            return existing;
        }

        PoolEntry entry = new PoolEntry();
        entry.Pool = new ObjectPool<GameObject>(
            () => createPooledObject(key, createFunction),
            activatePooledObject,
            deactivatePooledObject,
            destroyPooledObject,
            false,
            Mathf.Max(1, defaultCapacity),
            Mathf.Max(defaultCapacity, maxSize));

        POOLS.Add(key, entry);
        return entry;
    }

    private static GameObject createPooledObject(string key, Func<GameObject> createFunction)
    {
        GameObject instance = createFunction();
        if (instance == null)
        {
            return null;
        }

        PooledRuntimeObject pooledObject;
        if (!instance.TryGetComponent(out pooledObject))
        {
            pooledObject = instance.AddComponent<PooledRuntimeObject>();
        }

        pooledObject.Configure(key);
        instance.SetActive(false);
        return instance;
    }

    private static GameObject rent(PoolEntry entry, Vector3 position, Quaternion rotation)
    {
        if (entry == null || entry.Pool == null)
        {
            return null;
        }

        GameObject instance = entry.Pool.Get();
        if (instance == null)
        {
            return null;
        }

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    private static void activatePooledObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        PooledRuntimeObject pooledObject;
        if (target.TryGetComponent(out pooledObject))
        {
            pooledObject.HandleRented();
        }

    }

    private static void deactivatePooledObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(false);
    }

    private static void destroyPooledObject(GameObject target)
    {
        if (target != null)
        {
            UnityEngine.Object.Destroy(target);
        }
    }

    private static void ensureSceneHook()
    {
        if (mbSceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneUnloaded -= handleSceneUnloaded;
        SceneManager.sceneUnloaded += handleSceneUnloaded;
        mbSceneHookRegistered = true;
    }

    private static void handleSceneUnloaded(Scene scene)
    {
        clearAllPools();
    }

    private static void clearAllPools()
    {
        foreach (KeyValuePair<string, PoolEntry> pair in POOLS)
        {
            if (pair.Value != null && pair.Value.Pool != null)
            {
                pair.Value.Pool.Clear();
            }
        }

        POOLS.Clear();
    }
}

[DisallowMultipleComponent]
public sealed class PooledRuntimeObject : MonoBehaviour
{
    private string mPoolKey;
    private Coroutine mReleaseCoroutine;

    public string PoolKey => mPoolKey;
    public bool HasPool => !string.IsNullOrWhiteSpace(mPoolKey);

    internal void Configure(string poolKey)
    {
        mPoolKey = poolKey;
    }

    internal void HandleRented()
    {
        cancelRelease();
    }

    public void ReleaseAfter(float delay)
    {
        cancelRelease();
        if (delay <= 0f)
        {
            CombatObjectPool.ReleaseNow(this);
            return;
        }

        mReleaseCoroutine = StartCoroutine(releaseAfterRoutine(delay));
    }

    private void OnDisable()
    {
        cancelRelease();
    }

    private IEnumerator releaseAfterRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        mReleaseCoroutine = null;
        CombatObjectPool.ReleaseNow(this);
    }

    private void cancelRelease()
    {
        if (mReleaseCoroutine == null)
        {
            return;
        }

        StopCoroutine(mReleaseCoroutine);
        mReleaseCoroutine = null;
    }
}
