using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMove))]
[RequireComponent(typeof(EnemyAvoidObstacle))]
[RequireComponent(typeof(CharacterCore))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SpriteCompanionFollowController : MonoBehaviour
{
    private const float CompanionMoveSpeedMultiplier = 0.7f;

    [Header("跟随")]
    [SerializeField] private float followStartDistance = 1.05f;
    [SerializeField] private float followStopDistance = 0.78f;
    [SerializeField] private float spawnPadding = 0.18f;
    [SerializeField] private float minimumSpawnDistance = 0.55f;
    [SerializeField] private float slotRefreshInterval = 0.2f;
    [SerializeField] private float blockedSlotRetryDelay = 0.45f;
    [SerializeField] private float moveCatchupDistance = 2.8f;

    private EnemyMove move;
    private EnemyAvoidObstacle avoidObstacle;
    private CharacterCore companionCore;
    private SpriteRenderer companionRenderer;
    private Collider2D companionCollider;
    private BoxCollider2D companionBoxCollider;
    private Sprite colliderSourceSprite;

    private Transform playerTransform;
    private CharacterCore playerCore;
    private SpriteRenderer playerRenderer;
    private Vector2 currentSlotDirection = Vector2.right;
    private float nextSlotRefreshTime;
    private float blockedSince = -1f;
    private bool hasSpawnedNearPlayer;

    public void Bind(Transform targetPlayerTransform, CharacterCore targetPlayerCore = null, Collider2D targetCompanionCollider = null)
    {
        playerTransform = targetPlayerTransform;
        playerCore = targetPlayerCore != null
            ? targetPlayerCore
            : (playerTransform != null ? playerTransform.GetComponent<CharacterCore>() : null);
        playerRenderer = playerTransform != null ? playerTransform.GetComponent<SpriteRenderer>() : null;
        companionRenderer = GetComponent<SpriteRenderer>();
        companionBoxCollider = targetCompanionCollider as BoxCollider2D ?? GetComponent<BoxCollider2D>();
        companionCollider = targetCompanionCollider != null
            ? targetCompanionCollider
            : companionBoxCollider != null
                ? companionBoxCollider
                : GetComponent<Collider2D>();

        if (playerTransform == null)
        {
            return;
        }

        SyncCompanionCollider();
        IgnorePlayerCollisions();
        SyncSortingFromPlayer();
        float initialDistanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        SyncMoveSpeed(initialDistanceToPlayer);
        if (!hasSpawnedNearPlayer)
        {
            InitializeFollowSlot();
        }
    }

    public bool IsBoundTo(Transform target)
    {
        return playerTransform == target;
    }

    private void Awake()
    {
        move = GetComponent<EnemyMove>();
        avoidObstacle = GetComponent<EnemyAvoidObstacle>();
        companionCore = GetComponent<CharacterCore>();
        companionRenderer = GetComponent<SpriteRenderer>();
        companionBoxCollider = GetComponent<BoxCollider2D>();
        companionCollider = companionBoxCollider != null ? companionBoxCollider : GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!HasValidPlayer())
        {
            SelfDestruct();
            return;
        }

        SyncSortingFromPlayer();
        SyncCompanionCollider();
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        SyncMoveSpeed(distanceToPlayer);
    }

    private void FixedUpdate()
    {
        if (!HasValidPlayer())
        {
            return;
        }

        if (!hasSpawnedNearPlayer)
        {
            InitializeFollowSlot();
        }

        Vector2 playerPosition = playerTransform.position;
        Vector2 currentPosition = transform.position;
        float distanceToPlayer = Vector2.Distance(currentPosition, playerPosition);
        Vector2 desiredAnchor = ResolveDesiredAnchor(playerPosition, currentPosition);
        float distanceToAnchor = Vector2.Distance(currentPosition, desiredAnchor);
        SyncMoveSpeed(distanceToPlayer);

        if (distanceToPlayer <= followStopDistance && distanceToAnchor <= followStopDistance)
        {
            blockedSince = -1f;
            avoidObstacle?.ResetAvoidance();
            move?.StopMovement();
            return;
        }

        if (distanceToAnchor < followStartDistance)
        {
            move?.StopMovement();
            return;
        }

        Vector2 preferredDirection = move != null ? move.GetDirectionTo(desiredAnchor) : ResolveFourWayDirection(desiredAnchor - currentPosition);
        Vector2 finalDirection = avoidObstacle != null
            ? avoidObstacle.ResolveDirection(currentPosition, desiredAnchor, preferredDirection)
            : preferredDirection;

        if (finalDirection == Vector2.zero)
        {
            if (blockedSince < 0f)
            {
                blockedSince = Time.time;
            }

            move?.StopMovement();
            if (Time.time - blockedSince >= blockedSlotRetryDelay)
            {
                nextSlotRefreshTime = 0f;
                ResolveFollowSlot(playerPosition, currentPosition, true);
                blockedSince = -1f;
            }

            return;
        }

        blockedSince = -1f;
        if (companionCore != null)
        {
            companionCore.lastFacingDirection = finalDirection;
        }

        move?.SetMoveDirection(finalDirection);
    }

    private void OnDestroy()
    {
        SpriteCompanionRuntime.NotifyDestroyed(this);
    }

    private bool HasValidPlayer()
    {
        if (playerTransform == null)
        {
            return false;
        }

        GameObject playerObject = playerTransform.gameObject;
        if (!playerObject.activeInHierarchy)
        {
            return false;
        }

        if (!playerObject.scene.IsValid() || !playerObject.scene.isLoaded)
        {
            return false;
        }

        playerCore ??= playerObject.GetComponent<CharacterCore>();
        if (playerCore != null && playerCore.IsDead)
        {
            return false;
        }

        playerRenderer ??= playerObject.GetComponent<SpriteRenderer>();
        return true;
    }

    private void InitializeFollowSlot()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector3 playerPosition = playerTransform.position;
        currentSlotDirection = ResolveFollowSlot(playerPosition, playerPosition, true);
        Vector3 targetPosition = ResolveSlotWorldPosition(playerPosition, currentSlotDirection);

        blockedSince = -1f;
        hasSpawnedNearPlayer = true;
        nextSlotRefreshTime = Time.time + slotRefreshInterval;

        avoidObstacle?.ResetAvoidance();
        move?.StopMovement();

        // 只初始化目标槽位，不直接改 transform.position，避免精灵运行时瞬移。
        Vector2 faceDirection = ResolveFourWayDirection((Vector2)(playerPosition - targetPosition));
        if (faceDirection != Vector2.zero && companionCore != null)
        {
            companionCore.lastFacingDirection = faceDirection;
        }

        SyncSortingFromPlayer();
    }

    private Vector2 ResolveDesiredAnchor(Vector2 playerPosition, Vector2 currentPosition)
    {
        Vector2 slotDirection = ResolveFollowSlot(playerPosition, currentPosition, false);
        return ResolveSlotWorldPosition(playerPosition, slotDirection);
    }

    private Vector2 ResolveFollowSlot(Vector2 playerPosition, Vector2 currentPosition, bool forceRefresh)
    {
        if (!forceRefresh && Time.time < nextSlotRefreshTime && IsSlotDirectionStillValid(playerPosition, currentSlotDirection))
        {
            return currentSlotDirection;
        }

        Vector2[] priorities = BuildSlotPriority(playerPosition, currentPosition);
        for (int i = 0; i < priorities.Length; i++)
        {
            Vector2 candidateDirection = priorities[i];
            Vector2 candidatePosition = ResolveSlotWorldPosition(playerPosition, candidateDirection);
            if (IsSpawnCandidateValid(playerPosition, candidatePosition, candidateDirection))
            {
                currentSlotDirection = candidateDirection;
                nextSlotRefreshTime = Time.time + slotRefreshInterval;
                return currentSlotDirection;
            }
        }

        nextSlotRefreshTime = Time.time + slotRefreshInterval;
        return currentSlotDirection;
    }

    private Vector2[] BuildSlotPriority(Vector2 playerPosition, Vector2 currentPosition)
    {
        Vector2 facingDirection = ResolvePlayerFacingDirection();
        Vector2 preferredBehind = facingDirection != Vector2.zero ? -facingDirection : currentSlotDirection;
        Vector2 lateralA = Mathf.Abs(preferredBehind.x) > 0.1f ? Vector2.up : Vector2.right;
        Vector2 lateralB = -lateralA;
        Vector2 forward = -preferredBehind;

        if ((ResolveSlotWorldPosition(playerPosition, lateralA) - currentPosition).sqrMagnitude >
            (ResolveSlotWorldPosition(playerPosition, lateralB) - currentPosition).sqrMagnitude)
        {
            Vector2 swap = lateralA;
            lateralA = lateralB;
            lateralB = swap;
        }

        return new[]
        {
            preferredBehind,
            lateralA,
            lateralB,
            forward
        };
    }

    private bool IsSlotDirectionStillValid(Vector2 playerPosition, Vector2 slotDirection)
    {
        Vector2 candidatePosition = ResolveSlotWorldPosition(playerPosition, slotDirection);
        return IsSpawnCandidateValid(playerPosition, candidatePosition, slotDirection);
    }

    private Vector2 ResolveSlotWorldPosition(Vector2 playerPosition, Vector2 direction)
    {
        float probeDistance = ResolveSpawnProbeDistance();
        Vector2 rawCandidate = playerPosition + direction * probeDistance;
        return avoidObstacle != null
            ? avoidObstacle.SnapWorldPositionToReachableGrid(rawCandidate, playerPosition)
            : rawCandidate;
    }

    private Vector2 ResolvePlayerFacingDirection()
    {
        if (playerCore != null)
        {
            Vector2 facing = playerCore.lastFacingDirection;
            if (facing != Vector2.zero)
            {
                return ResolveFourWayDirection(facing);
            }
        }

        return currentSlotDirection != Vector2.zero ? -currentSlotDirection : Vector2.left;
    }

    private bool IsSpawnCandidateValid(Vector2 playerPosition, Vector2 candidatePosition, Vector2 expectedDirection)
    {
        Vector2 delta = candidatePosition - playerPosition;
        if (delta.sqrMagnitude < minimumSpawnDistance * minimumSpawnDistance)
        {
            return false;
        }

        if (avoidObstacle != null && avoidObstacle.IsPointBlocked(candidatePosition))
        {
            return false;
        }

        float directionAlignment = Vector2.Dot(delta.normalized, expectedDirection);
        return directionAlignment > 0.45f;
    }

    private float ResolveSpawnProbeDistance()
    {
        float playerExtent = playerRenderer != null
            ? Mathf.Max(playerRenderer.bounds.extents.x, playerRenderer.bounds.extents.y * 0.45f)
            : 0.22f;
        float companionExtent = companionRenderer != null && companionRenderer.sprite != null
            ? Mathf.Max(companionRenderer.bounds.extents.x, companionRenderer.bounds.extents.y * 0.45f)
            : 0.2f;

        return Mathf.Max(minimumSpawnDistance, playerExtent + companionExtent + spawnPadding);
    }

    private void SyncSortingFromPlayer()
    {
        if (companionRenderer == null || playerRenderer == null)
        {
            return;
        }

        companionRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        companionRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
    }

    private void SyncCompanionCollider()
    {
        if (companionBoxCollider == null || companionRenderer == null)
        {
            return;
        }

        Sprite currentSprite = companionRenderer.sprite;
        if (currentSprite == colliderSourceSprite)
        {
            return;
        }

        SpriteCompanionRuntime.ConfigureCompanionCollider(companionBoxCollider, companionRenderer);
        colliderSourceSprite = currentSprite;
    }

    private void SyncMoveSpeed(float distanceToPlayer = 0f)
    {
        if (companionCore == null || companionCore.stats == null)
        {
            return;
        }

        float playerSpeed = playerCore != null && playerCore.stats != null
            ? Mathf.Max(0f, playerCore.stats.moveSpeed)
            : 4.5f;
        float catchupProgress = Mathf.InverseLerp(followStartDistance, moveCatchupDistance, distanceToPlayer);
        float targetSpeed = Mathf.Lerp(playerSpeed * 0.72f, playerSpeed * 1.02f, catchupProgress);
        targetSpeed = Mathf.Max(2.6f * CompanionMoveSpeedMultiplier, targetSpeed * CompanionMoveSpeedMultiplier);

        companionCore.stats.moveSpeed = targetSpeed;
        if (companionCore.baseStats != null)
        {
            companionCore.baseStats.moveSpeed = targetSpeed;
        }
    }

    private void IgnorePlayerCollisions()
    {
        if (companionCollider == null || playerTransform == null)
        {
            return;
        }

        Collider2D[] playerColliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(companionCollider, playerCollider, true);
        }
    }

    private void SelfDestruct()
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        Destroy(gameObject);
    }

    private static Vector2 ResolveFourWayDirection(Vector2 delta)
    {
        if (delta.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x >= 0f ? Vector2.right : Vector2.left;
        }

        return delta.y >= 0f ? Vector2.up : Vector2.down;
    }
}
