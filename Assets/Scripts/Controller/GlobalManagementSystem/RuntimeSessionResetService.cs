using UnityEngine;

public static class RuntimeSessionResetService
{
    public static void ResetGameplayTransientState()
    {
        Time.timeScale = 1f;
        RuntimeCollectedCrystalRegistry.EnsureInstance().Clear();
        BackpackMananger.Instance?.ClearAllItems();
    }
}
