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
        // Only run while the game is playing
        if (!Application.isPlaying) return;

        Canvas[] allCanvas = Resources.FindObjectsOfTypeAll<Canvas>();

        foreach (Canvas canvas in allCanvas)
        {
            try
            {
                // Only process dynamically created Overlay UI objects that show the white wireframe.
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                }
            }
            catch { }
        }
    }
}