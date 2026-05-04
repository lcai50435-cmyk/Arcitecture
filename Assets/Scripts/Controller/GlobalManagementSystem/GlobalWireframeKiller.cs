using UnityEngine;
using UnityEngine.UI;

public class GlobalWireframeKiller : MonoBehaviour
{
    void Start()
    {
        HideAllCanvasWireframe();
    }

    void LateUpdate()
    {
        HideAllCanvasWireframe();
    }

    void HideAllCanvasWireframe()
    {
        // 只在游戏运行时执行
        if (!Application.isPlaying) return;

        Canvas[] allCanvas = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in allCanvas)
        {
            try
            {
                // 只处理动态创建、带Overlay的UI（就是你那些白线框物体）
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                }
            }
            catch { }
        }
    }
}