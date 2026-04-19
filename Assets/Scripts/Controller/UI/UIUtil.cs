using UnityEngine;

public static class UIUtil
{
    public static void SetUIActive(CanvasGroup cg, bool active)
    {
        if (cg == null) return;

        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }
}