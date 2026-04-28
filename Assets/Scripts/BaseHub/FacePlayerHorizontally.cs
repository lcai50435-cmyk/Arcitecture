using UnityEngine;

[DisallowMultipleComponent]
public sealed class FacePlayerHorizontally : MonoBehaviour
{
    [SerializeField] private Transform faceRoot;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float horizontalDeadZone = 0.05f;

    private Transform playerTarget;
    private Vector3 initialScale;
    private bool initialized;

    private void Awake()
    {
        faceRoot = faceRoot != null ? faceRoot : transform;
        initialScale = faceRoot.localScale;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Awake();
        }

        if (!TryResolvePlayer())
        {
            return;
        }

        float deltaX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= horizontalDeadZone)
        {
            return;
        }

        Vector3 nextScale = initialScale;
        nextScale.x = Mathf.Abs(initialScale.x) * (deltaX >= 0f ? 1f : -1f);
        faceRoot.localScale = nextScale;
    }

    private bool TryResolvePlayer()
    {
        if (playerTarget != null && playerTarget.gameObject.activeInHierarchy)
        {
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        playerTarget = playerObject != null ? playerObject.transform : null;
        return playerTarget != null;
    }
}
