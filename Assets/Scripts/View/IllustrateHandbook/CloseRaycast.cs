using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_SubMeshUI))]
public class CloseRaycast : MonoBehaviour
{
    void Awake()
    {
        // 直接把Raycast Target关掉
        var subMesh = GetComponent<TMP_SubMeshUI>();
        if (subMesh != null)
        {
            subMesh.raycastTarget = false;
        }
    }
}