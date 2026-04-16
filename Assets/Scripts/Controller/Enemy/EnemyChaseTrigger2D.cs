using UnityEngine;

/// <summary>
/// Relays trigger events from child chase colliders to EnemyChase.
/// </summary>
[DisallowMultipleComponent]
public class EnemyChaseTrigger2D : MonoBehaviour
{
    [SerializeField] private EnemyChase owner;

    [Header("Trigger Range")]
    public bool autoExpandTriggerCollider = true;
    public bool onlyExpandIfSmaller = true;
    public float minCircleTriggerRadius = 2f;
    public Vector2 minBoxTriggerSize = new Vector2(3f, 2.2f);

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<EnemyChase>();
        }

        ConfigureTriggerCollider();
    }

    private void OnValidate()
    {
        if (minCircleTriggerRadius < 0f) minCircleTriggerRadius = 0f;
        if (minBoxTriggerSize.x < 0f) minBoxTriggerSize.x = 0f;
        if (minBoxTriggerSize.y < 0f) minBoxTriggerSize.y = 0f;

        ConfigureTriggerCollider();
    }

    public void SetOwner(EnemyChase enemyChase)
    {
        owner = enemyChase;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.NotifyPlayerEnteredRange(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        owner?.NotifyPlayerExitedRange(other);
    }

    private void ConfigureTriggerCollider()
    {
        if (!autoExpandTriggerCollider)
        {
            return;
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null && circle.isTrigger)
        {
            circle.radius = onlyExpandIfSmaller
                ? Mathf.Max(circle.radius, minCircleTriggerRadius)
                : minCircleTriggerRadius;
            return;
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null && box.isTrigger)
        {
            box.size = onlyExpandIfSmaller
                ? new Vector2(
                    Mathf.Max(box.size.x, minBoxTriggerSize.x),
                    Mathf.Max(box.size.y, minBoxTriggerSize.y))
                : minBoxTriggerSize;
        }
    }
}
