using UnityEngine;
using UnityEngine.EventSystems;

public static class RuntimeUiInputGuard
{
    public static bool ShouldBlockGameplayAttack(KeyCode attackKey)
    {
        if (IsBlockingGameplayUiOpen())
        {
            return true;
        }

        if (!BackpackUI.IsRuntimeAttackSlotSelected())
        {
            return true;
        }

        return IsMouseKey(attackKey) && IsPointerOverUi();
    }

    public static bool IsBlockingGameplayUiOpen()
    {
        UIRootManager rootManager = UIRootManager.Instance ?? Object.FindObjectOfType<UIRootManager>(true);
        if (rootManager != null && rootManager.IsAnyGameplayBlockingUIOpen())
        {
            return true;
        }

        return RuntimePauseMenu.IsPauseOpen ||
               (RuntimeSettingsPanel.Instance != null && RuntimeSettingsPanel.Instance.IsShown) ||
               (UIManager.Instance != null && UIManager.Instance.IsHandbookOpen) ||
               RuntimePhotoCaptureManager.IsCaptureInProgress;
    }

    private static bool IsPointerOverUi()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (eventSystem.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMouseKey(KeyCode key)
    {
        return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
    }
}
