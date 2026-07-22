using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_SubMeshUI))]
public class CloseRaycast : MonoBehaviour
{
    void Awake()
    {
        // Disable Raycast Target directly
        var subMesh = GetComponent<TMP_SubMeshUI>();
        if (subMesh != null)
        {
            subMesh.raycastTarget = false;
        }
    }
}