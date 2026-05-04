using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeUiRaycastCleanup
{
    private static readonly string[] RuntimeBlockingCanvasNames =
    {
        "RuntimeModalShellCanvas",
        "BeaverAssistantPanelCanvas"
    };

    public static void CleanupAfterBaseModalClosed()
    {
        EventSystem.current?.SetSelectedGameObject(null);
        DisableHiddenCanvasGroups();
        DisableClosedBasePanelRaycasters();
        DisableInactiveRuntimeBlockingCanvases();
    }

    public static void CleanupForSceneTransition()
    {
        CleanupAfterBaseModalClosed();
    }

    private static void DisableHiddenCanvasGroups()
    {
        CanvasGroup[] groups = Object.FindObjectsOfType<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];
            if (group == null)
            {
                continue;
            }

            RuntimeSettingsPanel settingsPanel = group.GetComponentInParent<RuntimeSettingsPanel>(true);
            if (settingsPanel != null && settingsPanel.ShouldPreserveCanvasGroup(group))
            {
                continue;
            }

            if (!group.gameObject.activeInHierarchy || group.alpha <= 0.01f)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
    }

    private static void DisableClosedBasePanelRaycasters()
    {
        if (!string.Equals(SceneManager.GetActiveScene().name, "NewBase", System.StringComparison.Ordinal))
        {
            return;
        }

        DisableClosedPanelRaycasters<StageSelectionPanelUI>();
        DisableClosedPanelRaycasters<SpiritPanelUI>();
        DisableClosedPanelRaycasters<BaseHubAlbumPanel>();
    }

    private static void DisableClosedPanelRaycasters<T>()
        where T : Component
    {
        T[] panels = Object.FindObjectsOfType<T>(true);
        for (int i = 0; i < panels.Length; i++)
        {
            T panel = panels[i];
            if (panel == null || panel.gameObject.activeInHierarchy)
            {
                continue;
            }

            DisableRaycasters(panel.gameObject);
        }
    }

    private static void DisableInactiveRuntimeBlockingCanvases()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsRuntimeBlockingCanvas(canvas.name))
            {
                DisableRaycasters(canvas.gameObject);
            }
        }
    }

    private static bool IsRuntimeBlockingCanvas(string canvasName)
    {
        for (int i = 0; i < RuntimeBlockingCanvasNames.Length; i++)
        {
            if (string.Equals(canvasName, RuntimeBlockingCanvasNames[i], System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void DisableRaycasters(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        GraphicRaycaster[] raycasters = root.GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
        {
            if (raycasters[i] != null)
            {
                raycasters[i].enabled = false;
            }
        }
    }
}
