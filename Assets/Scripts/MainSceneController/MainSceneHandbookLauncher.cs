using System;
using UnityEngine;

public sealed class MainSceneHandbookLauncher : MonoBehaviour
{
    public static MainSceneHandbookLauncher Instance { get; private set; }

    [SerializeField] private GameObject handbookPrefab;
    public bool IsHandbookOpen
    {
        get
        {
            if (IllustratedUISceneLoader.TryGetUIManager(out UIManager manager))
            {
                return manager.IsHandbookOpen;
            }

            return UIManager.Instance != null && UIManager.Instance.IsHandbookOpen;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static bool IsAnyHandbookOpen()
    {
        return Instance != null && Instance.IsHandbookOpen;
    }

    public bool TryOpen(GameObject hideTarget)
    {
        EnsureRuntimeDependencies();

        GameObject[] hideTargets = hideTarget != null
            ? new[] { hideTarget }
            : Array.Empty<GameObject>();

        return IllustratedUISceneLoader.Open(
            RuntimeModalOpenSource.None,
            IllustratedHandbookPage.IllustratedHandbook,
            hideTargets,
            null,
            null,
            false);
    }

    private void EnsureRuntimeDependencies()
    {
        RuntimeProgressState.EnsureInstance();
        CatalogueUnlockSelectionManager.EnsureInstance();
    }
}
