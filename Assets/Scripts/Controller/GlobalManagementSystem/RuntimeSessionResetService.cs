using UnityEngine;
using UnityEngine.EventSystems;

public static class RuntimeSessionResetService
{
    public static void ResetGameplayTransientState()
    {
        Time.timeScale = 1f;
        RuntimeCollectedCrystalRegistry.EnsureInstance().Clear();
        BackpackMananger.Instance?.ClearAllItems();
    }

    public static void ResetRuntimeUiForSceneTransition()
    {
        RuntimePauseMenu.CloseForSceneTransition();
        BeaverAssistantPanel.HideForSceneTransition();
        UIRootManager.HideAllRuntimeUiForSceneTransition();
        RuntimeUiRaycastCleanup.CleanupForSceneTransition();

        Time.timeScale = 1f;
        EventSystem.current?.SetSelectedGameObject(null);
    }
}
