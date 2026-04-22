using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 人物移动
/// </summary>
public class PlayerMove : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public Animator animator;
    
    protected CharacterCore core;

    // 记住最后一次的方向
    private float lastInputX;
    private float lastInputY;

    private float moveSpeed; // 速度
    private float externalMoveSpeedMultiplier = 1f;

    [HideInInspector] public bool canMove = true;

    // 挂载朝向跟踪组件
    private DirectionTracker directionTracker;

    private void Awake()
    {
        core = GetComponent<CharacterCore>();
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // 获取朝向跟踪组件
        directionTracker = GetComponent<DirectionTracker>();

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

        moveSpeed = core.stats.moveSpeed * Mathf.Max(0f, externalMoveSpeedMultiplier);

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
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

        if (!canMove) return;

        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputY = Input.GetAxisRaw("Vertical");

        // 人物强制四方向行走
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            // 同时按了两个键 则 清空一个轴，强制四方向
            inputX = 0;
        }

        Vector2 currentMoveDir = new Vector2(inputX, inputY);
        // 判断是否移动
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // 关键：更新朝向到工具类
        if (isMoving)
        {
            directionTracker?.UpdateMoveDirection(currentMoveDir);
            if (core != null)
            {
                core.lastFacingDirection = currentMoveDir.normalized;
            }
        }

        // 从工具类获取最后朝向，更新动画
        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.down;
        if (AnimatorParameterUtility.CanDrive(animator))
        {
            AnimatorParameterUtility.SetFloatIfPresent(animator, "InputX", lastDir.x);
            AnimatorParameterUtility.SetFloatIfPresent(animator, "InputY", lastDir.y);
            AnimatorParameterUtility.SetBoolIfPresent(animator, "IsMoving", isMoving);
        }

        // 移动
        if (rb != null)
        {
            rb.velocity = new Vector2(inputX, inputY) * moveSpeed;
        }
    }

    public void SetExternalMoveSpeedMultiplier(float multiplier)
    {
        externalMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
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
