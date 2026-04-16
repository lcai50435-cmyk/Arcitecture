using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a stable A* path that is aligned to the active Tilemap grid.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMove))]
public class EnemyAvoidObstacle : MonoBehaviour
{
    [Header("Tilemap Grid")]
    public GridLayout navigationGrid;
    public bool autoFindNavigationGrid = true;
    public bool autoFindObstacleTilemaps = true;
    public bool autoFindObstacleColliders = true;
    public List<Tilemap> obstacleTilemaps = new List<Tilemap>();
    public List<Collider2D> obstacleColliders = new List<Collider2D>();
    public string[] obstacleTilemapNames =
    {
        "TilemapCollision",
        "DecorationCollision",
        "BuildingCollision",
        "Water Collision Tilemap",
        "WaterCollisionTilemap"
    };

    [Header("Obstacle Probe")]
    public Vector2 obstacleProbeSize = new Vector2(0.55f, 0.55f);

    [Header("A*")]
    public float fallbackCellWidth = 1f;
    public float fallbackCellHeight = 0.5f;
    public float waypointThreshold = 0.08f;
    public float repathInterval = 0.15f;
    public float repathMoveThreshold = 0.2f;
    public float reverseDirectionLockTime = 0.18f;
    public float turnDirectionLockTime = 0.1f;
    public int searchPadding = 10;
    public int maxSearchNodes = 4096;
    public int goalSearchRadius = 8;
    public int snapSearchRadius = 2;
    public string[] obstacleNames = { "Water", "Obstacle", "Building" };

    private EnemyMove move;
    private readonly List<Vector2Int> currentPath = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
    private readonly Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();
    private readonly List<Vector2Int> openSet = new List<Vector2Int>();

    private Vector2Int cachedGoalCell;
    private Vector2 cachedDestination;
    private float nextRepathTime;
    private bool hasPath;
    private int currentPathIndex;
    private Vector2Int pathStartCell;
    private Vector2 lastResolvedDirection;
    private float lastResolvedDirectionTime;

    private static readonly Vector2[] WorldDirections =
    {
        Vector2.right,
        Vector2.left,
        Vector2.up,
        Vector2.down
    };

    private void Awake()
    {
        move = GetComponent<EnemyMove>();
        ResolveNavigationReferences();
    }

    private void Start()
    {
        ResolveNavigationReferences();
    }

    private void OnDisable()
    {
        ResetAvoidance();
    }

    private void OnValidate()
    {
        if (obstacleProbeSize.x < 0.05f) obstacleProbeSize.x = 0.05f;
        if (obstacleProbeSize.y < 0.05f) obstacleProbeSize.y = 0.05f;
        if (fallbackCellWidth < 0.05f) fallbackCellWidth = 0.05f;
        if (fallbackCellHeight < 0.05f) fallbackCellHeight = 0.05f;
        if (waypointThreshold < 0.01f) waypointThreshold = 0.01f;
        if (repathInterval < 0f) repathInterval = 0f;
        if (repathMoveThreshold < 0f) repathMoveThreshold = 0f;
        if (reverseDirectionLockTime < 0f) reverseDirectionLockTime = 0f;
        if (turnDirectionLockTime < 0f) turnDirectionLockTime = 0f;
        if (searchPadding < 1) searchPadding = 1;
        if (maxSearchNodes < 64) maxSearchNodes = 64;
        if (goalSearchRadius < 1) goalSearchRadius = 1;
        if (snapSearchRadius < 1) snapSearchRadius = 1;
    }

    public void ResetAvoidance()
    {
        currentPath.Clear();
        cameFrom.Clear();
        gScore.Clear();
        openSet.Clear();
        cachedGoalCell = default;
        cachedDestination = Vector2.zero;
        nextRepathTime = 0f;
        hasPath = false;
        currentPathIndex = 0;
        pathStartCell = default;
        lastResolvedDirection = Vector2.zero;
        lastResolvedDirectionTime = float.NegativeInfinity;
    }

    public Vector2 ResolveDirection(Vector2 currentPos, Vector2 destination, Vector2 preferredDirection)
    {
        ResolveNavigationReferences();

        if (preferredDirection == Vector2.zero)
        {
            ResetAvoidance();
            return Vector2.zero;
        }

        Vector2Int startCell = FindNearestNavigationCell(currentPos);
        Vector2Int goalCell = ResolveGoalCell(startCell, destination, preferredDirection);
        bool preferredBlocked = IsDirectionBlocked(currentPos, preferredDirection);

        if (!hasPath && !preferredBlocked)
        {
            return GetStableResolvedDirection(currentPos, preferredDirection);
        }

        bool destinationMoved =
            (destination - cachedDestination).sqrMagnitude >= GetLargestWorldStep() * GetLargestWorldStep();

        bool needRepath =
            !hasPath ||
            preferredBlocked ||
            Time.time >= nextRepathTime ||
            goalCell != cachedGoalCell ||
            destinationMoved ||
            CurrentWaypointBlocked();

        if (needRepath)
        {
            BuildPath(startCell, goalCell, preferredDirection);
            cachedGoalCell = goalCell;
            cachedDestination = destination;
            nextRepathTime = Time.time + repathInterval;
        }

        AdvancePathIndex(currentPos);

        if (!hasPath || currentPathIndex >= currentPath.Count)
        {
            hasPath = false;
            currentPathIndex = 0;
            return GetStableResolvedDirection(currentPos, preferredBlocked ? Vector2.zero : preferredDirection);
        }

        Vector2 pathDirection = GetCurrentPathDirection(currentPos);

        if (pathDirection == Vector2.zero)
        {
            return GetStableResolvedDirection(currentPos, preferredBlocked ? Vector2.zero : preferredDirection);
        }

        return GetStableResolvedDirection(currentPos, pathDirection);
    }

    public Vector2 SnapWorldPositionToGrid(Vector2 worldPosition)
    {
        return GetCellCenterWorld(FindNearestNavigationCell(worldPosition));
    }

    public Vector2 SnapWorldPositionToReachableGrid(Vector2 worldPosition, Vector2 referenceWorldPosition)
    {
        Vector2Int referenceCell = FindNearestNavigationCell(referenceWorldPosition);
        int movementGroup = GetMovementGroup(referenceCell);
        Vector2Int snappedCell = FindNearestNavigationCell(worldPosition, movementGroup, snapSearchRadius, false);
        return GetCellCenterWorld(snappedCell);
    }

    public bool IsPointBlocked(Vector2 point)
    {
        ResolveNavigationReferences();

        Vector2Int cell = FindNearestNavigationCell(point);
        if (IsCellBlocked(cell))
        {
            return true;
        }

        return obstacleTilemaps.Count == 0 && IsBlockedByObstacleColliders(point);
    }

    public bool IsDirectionBlocked(Vector2 currentPos, Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return false;
        }

        Vector2Int currentCell = FindNearestNavigationCell(currentPos);
        Vector2Int nextCell = currentCell + WorldDirectionToCellDelta(direction);
        return IsCellBlocked(nextCell);
    }

    private void ResolveNavigationReferences()
    {
        if (autoFindObstacleTilemaps)
        {
            AutoFindObstacleTilemaps();
        }

        if (autoFindObstacleColliders)
        {
            AutoFindObstacleColliders();
        }

        if (navigationGrid == null && autoFindNavigationGrid)
        {
            AutoFindNavigationGrid();
        }
    }

    private void AutoFindObstacleTilemaps()
    {
        obstacleTilemaps.RemoveAll(tilemap => tilemap == null);
        if (obstacleTilemaps.Count > 0)
        {
            return;
        }

        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || !IsObstacleTilemapCandidate(tilemap))
            {
                continue;
            }

            if (!obstacleTilemaps.Contains(tilemap))
            {
                obstacleTilemaps.Add(tilemap);
            }
        }
    }

    private void AutoFindNavigationGrid()
    {
        for (int i = 0; i < obstacleTilemaps.Count; i++)
        {
            Tilemap tilemap = obstacleTilemaps[i];
            if (tilemap == null)
            {
                continue;
            }

            GridLayout candidate = tilemap.layoutGrid != null
                ? tilemap.layoutGrid
                : tilemap.GetComponentInParent<GridLayout>();

            if (candidate != null)
            {
                navigationGrid = candidate;
                return;
            }
        }

        GridLayout[] grids = FindObjectsOfType<GridLayout>(true);
        if (grids.Length > 0)
        {
            navigationGrid = grids[0];
        }
    }

    private void AutoFindObstacleColliders()
    {
        obstacleColliders.RemoveAll(collider => collider == null);
        if (obstacleColliders.Count > 0)
        {
            return;
        }

        TilemapCollider2D[] tilemapColliders = FindObjectsOfType<TilemapCollider2D>(true);
        for (int i = 0; i < tilemapColliders.Length; i++)
        {
            TilemapCollider2D tilemapCollider = tilemapColliders[i];
            if (tilemapCollider == null || tilemapCollider.isTrigger)
            {
                continue;
            }

            Tilemap tilemap = tilemapCollider.GetComponent<Tilemap>();
            if (tilemap == null || !IsObstacleTilemapCandidate(tilemap))
            {
                continue;
            }

            AddObstacleCollider(tilemapCollider);

            CompositeCollider2D composite = tilemapCollider.GetComponent<CompositeCollider2D>();
            if (composite != null && !composite.isTrigger)
            {
                AddObstacleCollider(composite);
            }
        }

        Collider2D[] colliders = FindObjectsOfType<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || collider.isTrigger || collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (IsObstacleCollider(collider))
            {
                AddObstacleCollider(collider);
            }
        }
    }

    private bool IsObstacleTilemapCandidate(Tilemap tilemap)
    {
        string normalizedName = NormalizeName(tilemap.name);
        if (normalizedName.Contains("noncollision") || normalizedName.Contains("terrain"))
        {
            return false;
        }

        if (MatchesObstacleTilemapName(normalizedName))
        {
            return true;
        }

        bool hasCollider = tilemap.GetComponent<TilemapCollider2D>() != null;
        if (hasCollider && normalizedName.Contains("collision"))
        {
            return true;
        }

        if (hasCollider && (normalizedName.Contains("building") || normalizedName.Contains("water")))
        {
            return true;
        }

        if (MatchesObstacleName(tilemap.tag) || MatchesObstacleName(LayerMask.LayerToName(tilemap.gameObject.layer)))
        {
            return true;
        }

        return false;
    }

    private Vector2Int ResolveGoalCell(Vector2Int startCell, Vector2 destination, Vector2 preferredDirection)
    {
        int movementGroup = GetMovementGroup(startCell);
        Vector2Int targetCell = FindNearestNavigationCell(destination, movementGroup, goalSearchRadius, false);

        if (!IsCellBlocked(targetCell))
        {
            return targetCell;
        }

        Vector2Int fallbackTarget = startCell + WorldDirectionToCellDelta(preferredDirection);
        if (!IsCellBlocked(fallbackTarget))
        {
            return fallbackTarget;
        }

        Vector2Int bestCell = targetCell;
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int radius = 1; radius <= goalSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = targetCell + new Vector2Int(x, y);
                    if (!IsCellInMovementGroup(candidate, movementGroup) || IsCellBlocked(candidate))
                    {
                        continue;
                    }

                    float distance = (GetCellCenterWorld(candidate) - destination).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCell = candidate;
                        found = true;
                    }
                }
            }

            if (found)
            {
                return bestCell;
            }
        }

        return startCell;
    }

    private void BuildPath(Vector2Int startCell, Vector2Int goalCell, Vector2 preferredDirection)
    {
        currentPath.Clear();
        cameFrom.Clear();
        gScore.Clear();
        openSet.Clear();

        openSet.Add(startCell);
        gScore[startCell] = 0;
        pathStartCell = startCell;

        Vector2Int bestNode = startCell;
        int exploredNodes = 0;
        RectInt searchBounds = BuildSearchBounds(startCell, goalCell);

        while (openSet.Count > 0 && exploredNodes < maxSearchNodes)
        {
            int currentIndex = GetBestOpenSetIndex(goalCell);
            Vector2Int current = openSet[currentIndex];
            openSet.RemoveAt(currentIndex);
            exploredNodes++;

            if (Heuristic(current, goalCell) < Heuristic(bestNode, goalCell))
            {
                bestNode = current;
            }

            if (current == goalCell)
            {
                CreatePath(startCell, goalCell);
                hasPath = currentPath.Count > 0;
                currentPathIndex = 0;
                return;
            }

            List<Vector2Int> orderedNeighbors = GetOrderedNeighbors(current, goalCell, preferredDirection);
            for (int i = 0; i < orderedNeighbors.Count; i++)
            {
                Vector2Int neighbor = orderedNeighbors[i];
                if (!searchBounds.Contains(neighbor))
                {
                    continue;
                }

                if (neighbor != goalCell && IsCellBlocked(neighbor))
                {
                    continue;
                }

                int tentativeGScore = gScore[current] + 1;
                if (!gScore.TryGetValue(neighbor, out int existingScore) || tentativeGScore < existingScore)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        if (bestNode != startCell)
        {
            CreatePath(startCell, bestNode);
            hasPath = currentPath.Count > 0;
            currentPathIndex = 0;
            return;
        }

        hasPath = false;
        currentPathIndex = 0;
    }

    private void CreatePath(Vector2Int startCell, Vector2Int endCell)
    {
        currentPath.Clear();

        List<Vector2Int> cellPath = new List<Vector2Int>();
        Vector2Int current = endCell;
        cellPath.Add(current);

        while (current != startCell && cameFrom.TryGetValue(current, out Vector2Int parent))
        {
            current = parent;
            cellPath.Add(current);
        }

        cellPath.Reverse();

        for (int i = 1; i < cellPath.Count; i++)
        {
            currentPath.Add(cellPath[i]);
        }
    }

    private void AdvancePathIndex(Vector2 currentPos)
    {
        if (!hasPath)
        {
            return;
        }

        Vector2Int currentCell = FindNearestNavigationCell(currentPos);
        float threshold = GetWaypointThreshold();
        while (currentPathIndex < currentPath.Count)
        {
            Vector2 waypoint = GetCellCenterWorld(currentPath[currentPathIndex]);
            if (currentCell == currentPath[currentPathIndex] || Vector2.Distance(currentPos, waypoint) <= threshold)
            {
                currentPathIndex++;
                continue;
            }

            break;
        }
    }

    private bool CurrentWaypointBlocked()
    {
        if (!hasPath || currentPathIndex >= currentPath.Count)
        {
            return false;
        }

        return IsCellBlocked(currentPath[currentPathIndex]);
    }

    private Vector2 GetCurrentPathDirection(Vector2 currentPos)
    {
        if (!hasPath || currentPathIndex >= currentPath.Count)
        {
            return Vector2.zero;
        }

        Vector2Int segmentStart = currentPathIndex == 0 ? pathStartCell : currentPath[currentPathIndex - 1];
        Vector2Int segmentEnd = currentPath[currentPathIndex];
        Vector2 pathDirection = CellDeltaToWorldDirection(segmentEnd - segmentStart);

        if (pathDirection != Vector2.zero)
        {
            return pathDirection;
        }

        Vector2 waypoint = GetCellCenterWorld(segmentEnd);
        return move != null
            ? move.GetFourWayDirection(waypoint - currentPos)
            : FilterToFourWay(waypoint - currentPos);
    }

    private RectInt BuildSearchBounds(Vector2Int startCell, Vector2Int goalCell)
    {
        int minX = Mathf.Min(startCell.x, goalCell.x) - searchPadding;
        int minY = Mathf.Min(startCell.y, goalCell.y) - searchPadding;
        int maxX = Mathf.Max(startCell.x, goalCell.x) + searchPadding;
        int maxY = Mathf.Max(startCell.y, goalCell.y) + searchPadding;
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private int GetBestOpenSetIndex(Vector2Int goalCell)
    {
        int bestIndex = 0;
        int bestScore = int.MaxValue;

        for (int i = 0; i < openSet.Count; i++)
        {
            Vector2Int node = openSet[i];
            int nodeScore = gScore[node] + Heuristic(node, goalCell);

            if (nodeScore < bestScore)
            {
                bestScore = nodeScore;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private List<Vector2Int> GetOrderedNeighbors(Vector2Int current, Vector2Int goal, Vector2 preferredDirection)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(4);
        Vector2 goalWorldDelta = GetCellCenterWorld(goal) - GetCellCenterWorld(current);

        Vector2 primaryDirection = preferredDirection != Vector2.zero
            ? FilterToFourWay(preferredDirection)
            : FilterToFourWay(goalWorldDelta);

        if (primaryDirection == Vector2.zero)
        {
            primaryDirection = Vector2.right;
        }

        AddNeighborIfMissing(neighbors, current + WorldDirectionToCellDelta(primaryDirection));

        Vector2 secondaryDirection;
        if (Mathf.Abs(primaryDirection.x) > 0.1f)
        {
            secondaryDirection = goalWorldDelta.y >= 0f ? Vector2.up : Vector2.down;
        }
        else
        {
            secondaryDirection = goalWorldDelta.x >= 0f ? Vector2.right : Vector2.left;
        }

        AddNeighborIfMissing(neighbors, current + WorldDirectionToCellDelta(secondaryDirection));

        for (int i = 0; i < WorldDirections.Length; i++)
        {
            AddNeighborIfMissing(neighbors, current + WorldDirectionToCellDelta(WorldDirections[i]));
        }

        return neighbors;
    }

    private void AddNeighborIfMissing(List<Vector2Int> neighbors, Vector2Int candidate)
    {
        if (!neighbors.Contains(candidate))
        {
            neighbors.Add(candidate);
        }
    }

    private int Heuristic(Vector2Int from, Vector2Int to)
    {
        if (UsesIsometricMovementMapping())
        {
            return Mathf.Max(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y));
        }

        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    private Vector2Int FindNearestNavigationCell(
        Vector2 worldPosition,
        int requiredMovementGroup = -1,
        int searchRadius = -1,
        bool requireUnblocked = false)
    {
        if (navigationGrid == null)
        {
            return FallbackWorldToCell(worldPosition);
        }

        int radius = searchRadius >= 0 ? searchRadius : snapSearchRadius;
        Vector3Int baseCell3 = navigationGrid.WorldToCell(worldPosition);
        Vector2Int baseCell = new Vector2Int(baseCell3.x, baseCell3.y);
        Vector2Int bestCell = baseCell;
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int currentRadius = 0; currentRadius <= radius; currentRadius++)
        {
            for (int x = -currentRadius; x <= currentRadius; x++)
            {
                for (int y = -currentRadius; y <= currentRadius; y++)
                {
                    if (currentRadius > 0 && Mathf.Abs(x) != currentRadius && Mathf.Abs(y) != currentRadius)
                    {
                        continue;
                    }

                    Vector2Int candidate = baseCell + new Vector2Int(x, y);
                    if (requiredMovementGroup >= 0 && !IsCellInMovementGroup(candidate, requiredMovementGroup))
                    {
                        continue;
                    }

                    if (requireUnblocked && IsCellBlocked(candidate))
                    {
                        continue;
                    }

                    float distance = (GetCellCenterWorld(candidate) - worldPosition).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCell = candidate;
                        found = true;
                    }
                }
            }

            if (found)
            {
                return bestCell;
            }
        }

        return bestCell;
    }

    private bool IsCellBlocked(Vector2Int cell)
    {
        ResolveNavigationReferences();

        if (IsBlockedByObstacleTilemaps(cell))
        {
            return true;
        }

        return IsBlockedByObstacleColliders(GetCellCenterWorld(cell));
    }

    private bool IsBlockedByObstacleTilemaps(Vector2Int cell)
    {
        for (int i = 0; i < obstacleTilemaps.Count; i++)
        {
            Tilemap tilemap = obstacleTilemaps[i];
            if (tilemap == null)
            {
                continue;
            }

            if (tilemap.HasTile(new Vector3Int(cell.x, cell.y, 0)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlockedByObstacleColliders(Vector2 point)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(point, GetEffectiveProbeSize(), 0f);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsKnownObstacleCollider(overlaps[i]))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetStableResolvedDirection(Vector2 currentPos, Vector2 desiredDirection)
    {
        desiredDirection = FilterToFourWay(desiredDirection);
        if (desiredDirection == Vector2.zero)
        {
            lastResolvedDirection = Vector2.zero;
            return Vector2.zero;
        }

        if (lastResolvedDirection == Vector2.zero || desiredDirection == lastResolvedDirection)
        {
            RememberResolvedDirection(desiredDirection);
            return desiredDirection;
        }

        float elapsed = Time.time - lastResolvedDirectionTime;
        bool lastDirectionBlocked = IsDirectionBlocked(currentPos, lastResolvedDirection);

        if (!lastDirectionBlocked)
        {
            bool reversedDirection = desiredDirection == -lastResolvedDirection;
            bool changedAxis = Mathf.Abs(desiredDirection.x - lastResolvedDirection.x) > 0.1f &&
                               Mathf.Abs(desiredDirection.y - lastResolvedDirection.y) > 0.1f;

            if (reversedDirection && elapsed < reverseDirectionLockTime)
            {
                return lastResolvedDirection;
            }

            if (changedAxis && elapsed < turnDirectionLockTime)
            {
                return lastResolvedDirection;
            }
        }

        RememberResolvedDirection(desiredDirection);
        return desiredDirection;
    }

    private void RememberResolvedDirection(Vector2 direction)
    {
        lastResolvedDirection = direction;
        lastResolvedDirectionTime = Time.time;
    }

    private Vector2Int WorldDirectionToCellDelta(Vector2 direction)
    {
        Vector2 fourWayDirection = FilterToFourWay(direction);
        if (fourWayDirection == Vector2.zero)
        {
            return Vector2Int.zero;
        }

        if (UsesIsometricMovementMapping())
        {
            if (fourWayDirection == Vector2.right) return new Vector2Int(1, -1);
            if (fourWayDirection == Vector2.left) return new Vector2Int(-1, 1);
            if (fourWayDirection == Vector2.up) return new Vector2Int(1, 1);
            return new Vector2Int(-1, -1);
        }

        if (fourWayDirection == Vector2.right) return Vector2Int.right;
        if (fourWayDirection == Vector2.left) return Vector2Int.left;
        if (fourWayDirection == Vector2.up) return Vector2Int.up;
        return Vector2Int.down;
    }

    private Vector2 CellDeltaToWorldDirection(Vector2Int cellDelta)
    {
        if (cellDelta == Vector2Int.zero)
        {
            return Vector2.zero;
        }

        if (UsesIsometricMovementMapping())
        {
            if (cellDelta == new Vector2Int(1, -1)) return Vector2.right;
            if (cellDelta == new Vector2Int(-1, 1)) return Vector2.left;
            if (cellDelta == new Vector2Int(1, 1)) return Vector2.up;
            if (cellDelta == new Vector2Int(-1, -1)) return Vector2.down;
            return Vector2.zero;
        }

        if (cellDelta == Vector2Int.right) return Vector2.right;
        if (cellDelta == Vector2Int.left) return Vector2.left;
        if (cellDelta == Vector2Int.up) return Vector2.up;
        if (cellDelta == Vector2Int.down) return Vector2.down;
        return Vector2.zero;
    }

    private int GetMovementGroup(Vector2Int cell)
    {
        return UsesIsometricMovementMapping() ? Mathf.Abs(cell.x + cell.y) % 2 : 0;
    }

    private bool IsCellInMovementGroup(Vector2Int cell, int requiredGroup)
    {
        return requiredGroup < 0 || GetMovementGroup(cell) == requiredGroup;
    }

    private bool UsesIsometricMovementMapping()
    {
        return navigationGrid != null &&
               (navigationGrid.cellLayout == GridLayout.CellLayout.Isometric ||
                navigationGrid.cellLayout == GridLayout.CellLayout.IsometricZAsY);
    }

    private Vector2 GetCellCenterWorld(Vector2Int cell)
    {
        Vector3Int cell3 = new Vector3Int(cell.x, cell.y, 0);

        if (navigationGrid is Grid grid)
        {
            return grid.GetCellCenterWorld(cell3);
        }

        for (int i = 0; i < obstacleTilemaps.Count; i++)
        {
            Tilemap tilemap = obstacleTilemaps[i];
            if (tilemap != null)
            {
                return tilemap.GetCellCenterWorld(cell3);
            }
        }

        if (navigationGrid != null)
        {
            Vector3 origin = navigationGrid.CellToWorld(cell3);
            Vector2 size = GetWorldStepSize();
            return origin + new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
        }

        return new Vector2(
            Mathf.Round(cell.x * fallbackCellWidth),
            Mathf.Round(cell.y * fallbackCellHeight));
    }

    private Vector2Int FallbackWorldToCell(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / fallbackCellWidth),
            Mathf.RoundToInt(worldPosition.y / fallbackCellHeight));
    }

    private float GetLargestWorldStep()
    {
        Vector2 step = GetWorldStepSize();
        return Mathf.Max(step.x, step.y);
    }

    private Vector2 GetWorldStepSize()
    {
        if (navigationGrid is Grid grid)
        {
            return new Vector2(Mathf.Abs(grid.cellSize.x), Mathf.Abs(grid.cellSize.y));
        }

        return new Vector2(fallbackCellWidth, fallbackCellHeight);
    }

    private float GetWaypointThreshold()
    {
        Vector2 step = GetWorldStepSize();
        return Mathf.Max(waypointThreshold, Mathf.Min(step.x, step.y) * 0.2f);
    }

    private Vector2 FilterToFourWay(Vector2 rawDirection)
    {
        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector2.zero;
        }

        if (move != null)
        {
            return move.GetFourWayDirection(rawDirection);
        }

        if (Mathf.Abs(rawDirection.x) >= Mathf.Abs(rawDirection.y))
        {
            return rawDirection.x >= 0f ? Vector2.right : Vector2.left;
        }

        return rawDirection.y >= 0f ? Vector2.up : Vector2.down;
    }

    private bool IsObstacleCollider(Collider2D col)
    {
        if (col == null || col.isTrigger)
        {
            return false;
        }

        if (col.transform.IsChildOf(transform))
        {
            return false;
        }

        Transform current = col.transform;
        while (current != null)
        {
            if (MatchesObstacleName(current.tag) || MatchesObstacleName(LayerMask.LayerToName(current.gameObject.layer)))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsKnownObstacleCollider(Collider2D col)
    {
        if (col == null || col.isTrigger)
        {
            return false;
        }

        if (obstacleColliders.Contains(col))
        {
            return true;
        }

        return IsObstacleCollider(col);
    }

    private void AddObstacleCollider(Collider2D collider)
    {
        if (collider == null || obstacleColliders.Contains(collider))
        {
            return;
        }

        obstacleColliders.Add(collider);
    }

    private bool MatchesObstacleName(string value)
    {
        if (string.IsNullOrEmpty(value) || obstacleNames == null)
        {
            return false;
        }

        for (int i = 0; i < obstacleNames.Length; i++)
        {
            if (obstacleNames[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesObstacleTilemapName(string normalizedName)
    {
        if (string.IsNullOrEmpty(normalizedName) || obstacleTilemapNames == null)
        {
            return false;
        }

        for (int i = 0; i < obstacleTilemapNames.Length; i++)
        {
            if (NormalizeName(obstacleTilemapNames[i]) == normalizedName)
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).ToLowerInvariant();
    }

    private Vector2 GetEffectiveProbeSize()
    {
        Vector2 step = GetWorldStepSize();
        float width = Mathf.Max(obstacleProbeSize.x, step.x * 0.35f);
        float height = Mathf.Max(obstacleProbeSize.y, step.y * 0.35f);
        return new Vector2(width, height);
    }
}
