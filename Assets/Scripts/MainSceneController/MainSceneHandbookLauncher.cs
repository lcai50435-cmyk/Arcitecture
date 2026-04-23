using System;
using UnityEngine;

public sealed class MainSceneHandbookLauncher : MonoBehaviour
{
    public static MainSceneHandbookLauncher Instance { get; private set; }

    [SerializeField] private GameObject handbookPrefab;

    private GameObject handbookInstance;
    private UIManager handbookManager;

    public bool IsHandbookOpen => handbookManager != null && handbookManager.IsHandbookOpen;

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
        if (handbookPrefab == null)
        {
            Debug.LogWarning("MainSceneHandbookLauncher 未绑定图鉴 prefab。");
            return false;
        }

        EnsureRuntimeDependencies();
        EnsureHandbookInstance();
        if (handbookManager == null)
        {
            return false;
        }

        GameObject[] hideTargets = hideTarget != null
            ? new[] { hideTarget }
            : Array.Empty<GameObject>();

        handbookManager.ConfigureForRuntime(
            handbookManager.illustratedHandbook,
            handbookManager.detailedInformation,
            hideTargets,
            null,
            null);
        handbookManager.OpenIllustratedHandbook();
        return true;
    }

    private void EnsureHandbookInstance()
    {
        if (handbookInstance != null && handbookManager != null)
        {
            return;
        }

        handbookInstance = Instantiate(handbookPrefab);
        handbookInstance.name = "MainSceneHandbookRuntime";
        handbookManager = handbookInstance.GetComponentInChildren<UIManager>(true);
        if (handbookManager == null)
        {
            Debug.LogError("MainScene 图鉴实例缺少 UIManager。");
            return;
        }

        IllustratedHandbookTabsController.EnsureInstalled(handbookManager);
        SetInitialCanvasState("DialogCanvas", false);
        SetInitialCanvasState("PackBagCanvas", false);
        SetInitialCanvasState("InteractionCanvas", false);
        SetInitialCanvasState("DetailedInformationCanvas", false);
        handbookManager.illustratedHandbook?.SetActive(false);
    }

    private void EnsureRuntimeDependencies()
    {
        RuntimeProgressState.EnsureInstance();
        CatalogueUnlockSelectionManager.EnsureInstance();
    }

    private void SetInitialCanvasState(string childName, bool active)
    {
        if (handbookInstance == null)
        {
            return;
        }

        Transform target = handbookInstance.transform.Find(childName);
        if (target == null)
        {
            Transform[] children = handbookInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    target = children[i];
                    break;
                }
            }
        }

        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }
}
