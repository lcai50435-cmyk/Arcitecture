using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles only four-direction enemy movement, facing, and movement animation output.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMove : MonoBehaviour
{
    private const string DefaultAnimatorControllerPath = "Assets/Animation/EnemyMove.controller";

<<<<<<< HEAD
    [Header("Movement")]    
=======
    [Header("Movement")]
    public float moveSpeed = 2f;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    public Rigidbody2D rb;
    public float axisSwitchThreshold = 0.15f;
    public float reverseDirectionLockTime = 0.12f;
    public float turnLockTime = 0.05f;

    [Header("Animation")]
    public Animator animator;
    public bool autoAssignMovementController = true;
    public RuntimeAnimatorController movementAnimatorController;

    [HideInInspector] public Vector2 moveDirection;

    private float lastInputX;
    private float lastInputY = -1f;
    private float lastDirectionChangeTime = float.NegativeInfinity;
<<<<<<< HEAD
    private CharacterCore character;
    private float moveSpeed;
=======
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58

    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

    private void Reset()
    {
        ResolveComponents();
        ResolveAnimationController();
        ApplyAnimatorController();
    }
<<<<<<< HEAD

    private void Start()
    {
        character = GetComponent<CharacterCore>();
        // 初始化怪物速度
        moveSpeed = character.stats.moveSpeed;
    }
=======
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58

    private void Awake()
    {
        ResolveComponents();
        ApplyAnimatorController();
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void OnValidate()
    {
        if (moveSpeed < 0f) moveSpeed = 0f;
        if (axisSwitchThreshold < 0f) axisSwitchThreshold = 0f;
        if (reverseDirectionLockTime < 0f) reverseDirectionLockTime = 0f;
        if (turnLockTime < 0f) turnLockTime = 0f;

        ResolveComponents();
        ResolveAnimationController();
        ApplyAnimatorController();
    }

    public void StopMovement()
    {
        ApplyDirection(Vector2.zero);
    }

    public void SetMoveDirection(Vector2 rawDirection)
    {
        if (!enabled)
        {
            StopMovement();
            return;
        }

        Vector2 filteredDirection = FilterToFourWay(rawDirection);
        Vector2 stableDirection = GetStableDirection(filteredDirection);
        ApplyDirection(stableDirection);
    }

    public Vector2 GetDirectionTo(Vector2 destination)
    {
        return GetFourWayDirection(destination - Position);
    }

    public Vector2 GetFourWayDirection(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (moveDirection != Vector2.zero)
        {
            bool movingHorizontally = Mathf.Abs(moveDirection.x) > 0.1f;

            if (movingHorizontally && absX + axisSwitchThreshold >= absY)
            {
                return delta.x >= 0f ? Vector2.right : Vector2.left;
            }

            if (!movingHorizontally && absY + axisSwitchThreshold >= absX)
            {
                return delta.y >= 0f ? Vector2.up : Vector2.down;
            }
        }

        if (absX >= absY)
        {
            return delta.x >= 0f ? Vector2.right : Vector2.left;
        }

        return delta.y >= 0f ? Vector2.up : Vector2.down;
    }

    public bool HasArrived(Vector2 destination, float threshold)
    {
        return Vector2.Distance(Position, destination) <= threshold;
    }

    private void ResolveComponents()
    {
        if (rb == null || rb.gameObject != gameObject)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (animator == null || animator.gameObject != gameObject)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void ResolveAnimationController()
    {
#if UNITY_EDITOR
        if (!autoAssignMovementController || movementAnimatorController != null)
        {
            return;
        }

        movementAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            DefaultAnimatorControllerPath);
#endif
    }

    private void ApplyAnimatorController()
    {
        if (animator == null)
        {
            return;
        }

        if (movementAnimatorController == null && animator.runtimeAnimatorController != null)
        {
            movementAnimatorController = animator.runtimeAnimatorController;
        }

        if (movementAnimatorController == null)
        {
            return;
        }

        if (animator.runtimeAnimatorController != movementAnimatorController)
        {
            animator.runtimeAnimatorController = movementAnimatorController;
        }
    }

    private Vector2 GetStableDirection(Vector2 desiredDirection)
    {
        if (desiredDirection == Vector2.zero || moveDirection == Vector2.zero || desiredDirection == moveDirection)
        {
            return desiredDirection;
        }

        float elapsed = Time.time - lastDirectionChangeTime;

        if (desiredDirection == -moveDirection && elapsed < reverseDirectionLockTime)
        {
            return moveDirection;
        }

        if (desiredDirection != -moveDirection && elapsed < turnLockTime)
        {
            return moveDirection;
        }

        return desiredDirection;
    }

    private void ApplyDirection(Vector2 direction)
    {
        bool changedDirection = direction != Vector2.zero && direction != moveDirection;
        moveDirection = direction;

        if (changedDirection)
        {
            lastDirectionChangeTime = Time.time;
        }

        bool isMoving = direction != Vector2.zero;
        if (isMoving)
        {
            lastInputX = direction.x;
            lastInputY = direction.y;
        }

        if (animator != null)
        {
            animator.SetFloat("InputX", lastInputX);
            animator.SetFloat("InputY", lastInputY);
            animator.SetBool("IsMoving", isMoving);
        }

        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;

        if (!isMoving)
        {
            return;
        }

        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private Vector2 FilterToFourWay(Vector2 rawDirection)
    {
        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        float inputX = rawDirection.x;
        float inputY = rawDirection.y;

        if (Mathf.Abs(inputX) >= Mathf.Abs(inputY))
        {
            return new Vector2(Mathf.Sign(inputX), 0f);
        }

        return new Vector2(0f, Mathf.Sign(inputY));
    }
}
