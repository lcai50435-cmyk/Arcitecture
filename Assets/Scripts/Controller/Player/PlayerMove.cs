using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Character movement
/// </summary>
public class PlayerMove : MonoBehaviour
{
    private const float PlayerBoxColliderWidthFactor = 0.82f;
    private const float PlayerBoxColliderHeightFactor = 0.56f;
    private const float PlayerCircleColliderFactor = 0.34f;
    private const float PlayerColliderLiftFactor = 0.05f;
    private const float MinimumBodyColliderWidth = 0.18f;
    private const float MinimumBodyColliderHeight = 0.16f;
    private const float MinimumBodyColliderRadius = 0.08f;
    
    public Rigidbody2D rb;
    public Animator animator;
    
    protected CharacterCore core;

    // Remember the last direction
    private float lastInputX;
    private float lastInputY;
    private Vector2 pendingMoveInput;

    private float moveSpeed; // Speed
    private float externalMoveSpeedMultiplier = 1f;

    [HideInInspector] public bool canMove = true;

    // Attach the facing direction tracker component
    private DirectionTracker directionTracker;
    private SpriteRenderer playerRenderer;
    private Collider2D bodyCollider;
    private Sprite colliderSourceSprite;

    private void Awake()
    {
        core = GetComponent<CharacterCore>();
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null)
        {
            ConfigureMovementDamping(rb);
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Get the facing direction tracker component
        directionTracker = GetComponent<DirectionTracker>();
        playerRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = ResolveBodyCollider();
        SyncBodyCollision();

        if (GetComponent<PlayerTreeOcclusionFader>() == null)
        {
            gameObject.AddComponent<PlayerTreeOcclusionFader>();
        }

        SpriteCompanionRuntime.EnsureForPlayer(gameObject);
        RuntimeCameraController.EnsureInstance().BindFollowTarget(transform);
    }

    void Update()
    {
        if (core == null || core.stats == null) return;

        SyncBodyCollision();
        moveSpeed = core.stats.moveSpeed * Mathf.Max(0f, externalMoveSpeedMultiplier);

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            pendingMoveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            if (AnimatorParameterUtility.CanDrive(animator))
            {
                AnimatorParameterUtility.SetBoolIfPresent(animator, "IsMoving", false);
            }

            return;
        }

        if (!canMove)
        {
            pendingMoveInput = Vector2.zero;
            return;
        }

        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputY = Input.GetAxisRaw("Vertical");

        // Force four-direction character movement
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            // If two keys are pressed at the same time, clear one axis to force four-direction movement
            inputX = 0;
        }

        Vector2 currentMoveDir = new Vector2(inputX, inputY);
        // Check whether the character is moving
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // Important: update facing direction in the helper
        if (isMoving)
        {
            directionTracker?.UpdateMoveDirection(currentMoveDir);
            if (core != null)
            {
                core.lastFacingDirection = currentMoveDir.normalized;
            }
        }

        // Get the last facing direction from the helper and update animation
        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.down;
        if (AnimatorParameterUtility.CanDrive(animator))
        {
            AnimatorParameterUtility.SetFloatIfPresent(animator, "InputX", lastDir.x);
            AnimatorParameterUtility.SetFloatIfPresent(animator, "InputY", lastDir.y);
            AnimatorParameterUtility.SetBoolIfPresent(animator, "IsMoving", isMoving);
        }

        // Move
        pendingMoveInput = new Vector2(inputX, inputY);
    }

    private void FixedUpdate()
    {
        if (core == null || core.stats == null) return;

        moveSpeed = core.stats.moveSpeed * Mathf.Max(0f, externalMoveSpeedMultiplier);

        if (rb != null)
        {
            rb.velocity = pendingMoveInput * moveSpeed;
        }
    }

    public void SetExternalMoveSpeedMultiplier(float multiplier)
    {
        externalMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    private static void ConfigureMovementDamping(Rigidbody2D rigidbody)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool disableMovementDamping = sceneName == "FirstPass_1" ||
                                      sceneName == "FirstPass_V2" ||
                                      sceneName == "SecondPassSence";

        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (disableMovementDamping)
        {
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
        }
    }

    private Collider2D ResolveBodyCollider()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D candidate = colliders[i];
            if (candidate != null && !candidate.isTrigger)
            {
                return candidate;
            }
        }

        return gameObject.AddComponent<BoxCollider2D>();
    }

    private void SyncBodyCollision()
    {
        bodyCollider ??= ResolveBodyCollider();
        playerRenderer ??= GetComponent<SpriteRenderer>();
        if (bodyCollider == null)
        {
            return;
        }

        bodyCollider.isTrigger = false;
        bodyCollider.enabled = true;

        Sprite currentSprite = playerRenderer != null ? playerRenderer.sprite : null;
        if (currentSprite == colliderSourceSprite && bodyCollider != null)
        {
            return;
        }

        ConfigureBodyCollider(bodyCollider, playerRenderer);
        colliderSourceSprite = currentSprite;
    }

    private static void ConfigureBodyCollider(Collider2D collider, SpriteRenderer renderer)
    {
        if (collider == null)
        {
            return;
        }

        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Bounds bounds = renderer.sprite.bounds;
        float width = Mathf.Max(MinimumBodyColliderWidth, bounds.size.x * PlayerBoxColliderWidthFactor);
        float height = Mathf.Max(MinimumBodyColliderHeight, bounds.size.y * PlayerBoxColliderHeightFactor);
        float centerX = bounds.center.x;
        float centerY = bounds.min.y + height * 0.5f + bounds.size.y * PlayerColliderLiftFactor;

        switch (collider)
        {
            case BoxCollider2D box:
                box.size = new Vector2(width, height);
                box.offset = new Vector2(centerX, centerY);
                break;
            case CapsuleCollider2D capsule:
                capsule.size = new Vector2(width, height);
                capsule.offset = new Vector2(centerX, centerY);
                capsule.direction = CapsuleDirection2D.Vertical;
                break;
            case CircleCollider2D circle:
                circle.radius = Mathf.Max(
                    MinimumBodyColliderRadius,
                    Mathf.Min(bounds.size.x * PlayerCircleColliderFactor, bounds.size.y * PlayerCircleColliderFactor));
                circle.offset = new Vector2(centerX, bounds.min.y + circle.radius + bounds.size.y * PlayerColliderLiftFactor);
                break;
        }
    }
}

[DisallowMultipleComponent]
public sealed class PlayerTreeOcclusionFader : MonoBehaviour
{
    private const float RefreshInterval = 0.05f;
    private const float FadedAlpha = 0.34f;
    private const int HorizontalScanRadius = 2;
    private const int VerticalScanRadius = 2;

    private readonly Dictionary<FadedTileKey, Color> fadedTiles = new Dictionary<FadedTileKey, Color>();
    private readonly HashSet<FadedTileKey> visibleThisFrame = new HashSet<FadedTileKey>();
    private readonly List<FadedTileKey> restoreBuffer = new List<FadedTileKey>();

    private Tilemap[] candidateTilemaps;
    private float nextRefreshTime;

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        RefreshOcclusion();
    }

    private void OnDisable()
    {
        RestoreAllTiles();
    }

    private void OnDestroy()
    {
        RestoreAllTiles();
    }

    private void RefreshOcclusion()
    {
        EnsureCandidateTilemaps();
        visibleThisFrame.Clear();

        Vector3 playerPosition = transform.position;
        for (int i = 0; i < candidateTilemaps.Length; i++)
        {
            Tilemap tilemap = candidateTilemaps[i];
            if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3Int centerCell = tilemap.WorldToCell(playerPosition);
            for (int x = -HorizontalScanRadius; x <= HorizontalScanRadius; x++)
            {
                for (int y = -VerticalScanRadius; y <= VerticalScanRadius; y++)
                {
                    Vector3Int cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                    TileBase tile = tilemap.GetTile(cell);
                    if (!IsTreeTile(tile))
                    {
                        continue;
                    }

                    if (!ShouldFadeTile(tilemap, cell, playerPosition))
                    {
                        continue;
                    }

                    FadedTileKey key = new FadedTileKey(tilemap, cell);
                    visibleThisFrame.Add(key);
                    ApplyFade(key);
                }
            }
        }

        RestoreInactiveTiles();
    }

    private void EnsureCandidateTilemaps()
    {
        if (candidateTilemaps != null && candidateTilemaps.Length > 0)
        {
            return;
        }

        Tilemap[] discovered = FindObjectsOfType<Tilemap>(true);
        List<Tilemap> matches = new List<Tilemap>(discovered.Length);
        for (int i = 0; i < discovered.Length; i++)
        {
            Tilemap tilemap = discovered[i];
            if (tilemap == null)
            {
                continue;
            }

            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                continue;
            }

            matches.Add(tilemap);
        }

        candidateTilemaps = matches.ToArray();
    }

    private static bool IsTreeTile(TileBase tile)
    {
        return tile != null &&
               !string.IsNullOrEmpty(tile.name) &&
               tile.name.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ShouldFadeTile(Tilemap tilemap, Vector3Int cell, Vector3 playerPosition)
    {
        Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);
        Sprite sprite = tilemap.GetSprite(cell);
        Vector2 spriteSize = sprite != null
            ? sprite.bounds.size
            : tilemap.layoutGrid.cellSize;

        float halfWidth = Mathf.Max(0.65f, spriteSize.x * 0.36f);
        float topY = cellCenter.y + Mathf.Max(0.12f, spriteSize.y * 0.06f);
        float bottomY = cellCenter.y - Mathf.Max(1.15f, spriteSize.y * 0.58f);

        return Mathf.Abs(playerPosition.x - cellCenter.x) <= halfWidth &&
               playerPosition.y >= bottomY &&
               playerPosition.y <= topY;
    }

    private void ApplyFade(FadedTileKey key)
    {
        if (key.tilemap == null)
        {
            return;
        }

        if (!fadedTiles.TryGetValue(key, out Color originalColor))
        {
            key.tilemap.RemoveTileFlags(key.cell, TileFlags.LockColor);
            originalColor = key.tilemap.GetColor(key.cell);
            fadedTiles[key] = originalColor;
        }

        Color fadedColor = originalColor;
        fadedColor.a = Mathf.Min(originalColor.a, FadedAlpha);
        key.tilemap.SetColor(key.cell, fadedColor);
    }

    private void RestoreInactiveTiles()
    {
        restoreBuffer.Clear();

        foreach (KeyValuePair<FadedTileKey, Color> entry in fadedTiles)
        {
            if (!visibleThisFrame.Contains(entry.Key))
            {
                restoreBuffer.Add(entry.Key);
            }
        }

        for (int i = 0; i < restoreBuffer.Count; i++)
        {
            RestoreTile(restoreBuffer[i]);
        }
    }

    private void RestoreAllTiles()
    {
        restoreBuffer.Clear();

        foreach (KeyValuePair<FadedTileKey, Color> entry in fadedTiles)
        {
            restoreBuffer.Add(entry.Key);
        }

        for (int i = 0; i < restoreBuffer.Count; i++)
        {
            RestoreTile(restoreBuffer[i]);
        }
    }

    private void RestoreTile(FadedTileKey key)
    {
        if (!fadedTiles.TryGetValue(key, out Color originalColor))
        {
            return;
        }

        if (key.tilemap != null)
        {
            key.tilemap.SetColor(key.cell, originalColor);
        }

        fadedTiles.Remove(key);
    }

    private struct FadedTileKey
    {
        public readonly Tilemap tilemap;
        public readonly Vector3Int cell;

        public FadedTileKey(Tilemap tilemap, Vector3Int cell)
        {
            this.tilemap = tilemap;
            this.cell = cell;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FadedTileKey other))
            {
                return false;
            }

            return tilemap == other.tilemap && cell.Equals(other.cell);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (tilemap != null ? tilemap.GetHashCode() : 0);
            hash = hash * 31 + cell.GetHashCode();
            return hash;
        }
    }
}
