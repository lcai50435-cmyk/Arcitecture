using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public enum TerrainType
{
    Water,
    Soil,
    Grass
}

[Serializable]
public struct MapCellData
{
    public TerrainType terrainType;
    public float heightNoise;
    public float moistureNoise;
    public bool isReservedForBuilding;
}

[Serializable]
public class TileAssetReference
{
    public TileBase tile;
    public Sprite sprite;

    [NonSerialized] private Tile runtimeTile;

    public bool IsEmpty => tile == null && sprite == null;

    public TileBase ResolveTile()
    {
        if (tile != null)
        {
            return tile;
        }

        if (sprite == null)
        {
            return null;
        }

        if (runtimeTile == null)
        {
            runtimeTile = ScriptableObject.CreateInstance<Tile>();
            runtimeTile.sprite = sprite;
            runtimeTile.name = $"{sprite.name}_RuntimeTile";
        }

        return runtimeTile;
    }
}

[Serializable]
public class MaskedTileVariants
{
    public string description;
    [Range(1, 15)] public int mask = 1;
    [Range(-1, 15)] public int diagonalMask = -1;
    public List<TileAssetReference> variants = new List<TileAssetReference>();

    public int GetResolvedVariantCount()
    {
        int count = 0;

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i] != null && variants[i].ResolveTile() != null)
            {
                count++;
            }
        }

        return count;
    }

    public bool TryGetResolvedVariantAt(int index, out TileBase resolvedTile)
    {
        int cursor = 0;

        for (int i = 0; i < variants.Count; i++)
        {
            TileBase tile = variants[i] == null ? null : variants[i].ResolveTile();
            if (tile == null)
            {
                continue;
            }

            if (cursor == index)
            {
                resolvedTile = tile;
                return true;
            }

            cursor++;
        }

        resolvedTile = null;
        return false;
    }
}

[Serializable]
public class TransitionTileLookup
{
    public List<MaskedTileVariants> entries = new List<MaskedTileVariants>();

    public bool HasConfiguredEntries()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].GetResolvedVariantCount() > 0)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryResolve(int mask, int diagonalMask, int variantSeed, out TileBase resolvedTile)
    {
        if (TryResolve(mask, diagonalMask, variantSeed, true, out resolvedTile))
        {
            return true;
        }

        return TryResolve(mask, diagonalMask, variantSeed, false, out resolvedTile);
    }

    private bool TryResolve(int mask, int diagonalMask, int variantSeed, bool exactDiagonal, out TileBase resolvedTile)
    {
        int totalVariants = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            MaskedTileVariants entry = entries[i];
            if (!IsMatch(entry, mask, diagonalMask, exactDiagonal))
            {
                continue;
            }

            totalVariants += entry.GetResolvedVariantCount();
        }

        if (totalVariants == 0)
        {
            resolvedTile = null;
            return false;
        }

        int remainingIndex = Mathf.Abs(variantSeed) % totalVariants;

        for (int i = 0; i < entries.Count; i++)
        {
            MaskedTileVariants entry = entries[i];
            if (!IsMatch(entry, mask, diagonalMask, exactDiagonal))
            {
                continue;
            }

            int variantCount = entry.GetResolvedVariantCount();
            if (remainingIndex >= variantCount)
            {
                remainingIndex -= variantCount;
                continue;
            }

            if (entry.TryGetResolvedVariantAt(remainingIndex, out resolvedTile))
            {
                return true;
            }
        }

        resolvedTile = null;
        return false;
    }

    private static bool IsMatch(MaskedTileVariants entry, int mask, int diagonalMask, bool exactDiagonal)
    {
        if (entry == null || entry.mask != mask)
        {
            return false;
        }

        if (exactDiagonal)
        {
            return entry.diagonalMask >= 0 && entry.diagonalMask == diagonalMask;
        }

        return entry.diagonalMask < 0;
    }
}

[Serializable]
public class TerrainTileCatalog
{
    public TileAssetReference grass = new TileAssetReference();
    public TileAssetReference soil = new TileAssetReference();
    public TileAssetReference water = new TileAssetReference();

    [Header("草土过渡")]
    public TransitionTileLookup soilGrassTransitions = new TransitionTileLookup();

    [Header("水边过渡")]
    public TransitionTileLookup waterEdgeTransitions = new TransitionTileLookup();

    [Header("草土水混合")]
    public TransitionTileLookup mixedTransitions = new TransitionTileLookup();

    public bool TryValidateBaseTiles(out string errorMessage)
    {
        List<string> missing = new List<string>();

        if (grass.ResolveTile() == null)
        {
            missing.Add("grass");
        }

        if (soil.ResolveTile() == null)
        {
            missing.Add("soil");
        }

        if (water.ResolveTile() == null)
        {
            missing.Add("water");
        }

        if (missing.Count == 0)
        {
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = $"基础地形资源缺失: {string.Join(", ", missing)}";
        return false;
    }

    public TileBase ResolveBaseTile(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Water:
                return water.ResolveTile();
            case TerrainType.Soil:
                return soil.ResolveTile();
            default:
                return grass.ResolveTile();
        }
    }
}

public class PerlinMapWithSingleBuilding : MonoBehaviour
{
    [Header("Tilemap 引用")]
    public Tilemap terrainTilemap;
    public Tilemap buildingTilemap;

    [Header("基础地形资源")]
    public TerrainTileCatalog terrainTiles = new TerrainTileCatalog();
    public TileAssetReference buildingTileAsset = new TileAssetReference();

    [Header("地图尺寸")]
    public int width = 80;
    public int height = 80;

    [Header("噪声参数")]
    public bool useRandomSeed = true;
    public int seed = 12345;
    [FormerlySerializedAs("offset")] public Vector2 manualOffset;
    [FormerlySerializedAs("scale")] public float heightScale = 18f;
    public float moistureScale = 12f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("阈值参数")]
    [Range(0f, 1f)] public float waterThreshold = 0.32f;
    [FormerlySerializedAs("dirtThreshold"), Range(0f, 1f)] public float soilThreshold = 0.52f;
    [Min(1)] public int edgeFalloffWidth = 6;

    [Header("聚集参数")]
    [Range(0, 3)] public int smoothingPasses = 1;
    [Min(1)] public int minWaterClusterSize = 12;
    [Min(1)] public int minSoilClusterSize = 10;

    [Header("建筑参数")]
    [FormerlySerializedAs("buildingOnDirt")] public bool buildingOnSoil = true;
    public bool useFixedPosition = false;
    public Vector2Int fixedPosition;
    [FormerlySerializedAs("randomPosition")] public bool allowGeneratedBuildingPosition = true;
    [Min(1)] public int buildingMinClusterSize = 18;
    [Min(0)] public int buildingPadding = 1;
    [Min(0)] public int minDistanceFromBorder = 3;

    [Header("调试参数")]
    public bool showDebugInfo = true;
    public bool clearBeforeGenerate = true;

    [FormerlySerializedAs("grassTile"), SerializeField, HideInInspector]
    private TileBase legacyGrassTile;

    [FormerlySerializedAs("dirtTile"), SerializeField, HideInInspector]
    private TileBase legacySoilTile;

    [FormerlySerializedAs("buildingTile"), SerializeField, HideInInspector]
    private TileBase legacyBuildingTile;

    private Vector2 lastHeightOffset;
    private Vector2 lastMoistureOffset;
    private GenerationStats lastStats;

    private static readonly Vector2Int[] CardinalDirections =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    private static readonly Vector2Int[] DiagonalDirections =
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 1)
    };

    private void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        ApplyLegacyDefaults();

        if (!ResolveSceneReferences())
        {
            return;
        }

        if (!ValidateConfiguration())
        {
            return;
        }

        if (clearBeforeGenerate)
        {
            terrainTilemap.ClearAllTiles();
            if (buildingTilemap != null)
            {
                buildingTilemap.ClearAllTiles();
            }
        }

        PrepareSeedForGeneration();

        TerrainGenerator generator = new TerrainGenerator(this, lastHeightOffset, lastMoistureOffset);
        MapCellData[,] map = generator.Generate();

        TerrainPostProcessor postProcessor = new TerrainPostProcessor(this);
        lastStats = postProcessor.Process(map);

        BuildingPlacementResult buildingPlacement = TryResolveBuildingPlacement(map);
        if (buildingPlacement.hasBuilding)
        {
            ReserveBuildingArea(map, buildingPlacement.position);
        }

        TerrainRenderer renderer = new TerrainRenderer(this);
        renderer.Render(map);
        RenderBuilding(buildingPlacement);

        if (showDebugInfo)
        {
            LogGenerationSummary(map, buildingPlacement);
        }
    }

    public void RegenerateMap()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RegenerateMap();
        }
    }

    private void ApplyLegacyDefaults()
    {
        if (terrainTiles.grass.IsEmpty && legacyGrassTile != null)
        {
            terrainTiles.grass.tile = legacyGrassTile;
        }

        if (terrainTiles.soil.IsEmpty && legacySoilTile != null)
        {
            terrainTiles.soil.tile = legacySoilTile;
        }

        if (buildingTileAsset.IsEmpty && legacyBuildingTile != null)
        {
            buildingTileAsset.tile = legacyBuildingTile;
        }
    }

    private bool ResolveSceneReferences()
    {
        if (terrainTilemap == null)
        {
            Tilemap[] childTilemaps = GetComponentsInChildren<Tilemap>();
            for (int i = 0; i < childTilemaps.Length; i++)
            {
                if (childTilemaps[i] == null)
                {
                    continue;
                }

                if (childTilemaps[i].name.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                terrainTilemap = childTilemaps[i];
                break;
            }

            if (terrainTilemap == null)
            {
                Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
                if (tilemaps.Length == 1)
                {
                    terrainTilemap = tilemaps[0];
                }
                else
                {
                    for (int i = 0; i < tilemaps.Length; i++)
                    {
                        if (tilemaps[i] == null)
                        {
                            continue;
                        }

                        if (tilemaps[i].name.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        terrainTilemap = tilemaps[i];
                        break;
                    }
                }
            }

            if (terrainTilemap != null && showDebugInfo)
            {
                Debug.Log($"ℹ️ 自动绑定 terrainTilemap: {terrainTilemap.name}");
            }
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("❌ 未找到可用的 terrainTilemap，地图生成已终止。");
            return false;
        }

        if (buildingTilemap == null && buildingTileAsset.ResolveTile() != null)
        {
            buildingTilemap = CreateRuntimeBuildingTilemap();
        }

        return true;
    }

    private Tilemap CreateRuntimeBuildingTilemap()
    {
        Transform parent = terrainTilemap.transform.parent != null
            ? terrainTilemap.transform.parent
            : terrainTilemap.transform;

        Transform existing = parent.Find("Building Tilemap");
        if (existing != null)
        {
            Tilemap existingTilemap = existing.GetComponent<Tilemap>();
            if (existingTilemap != null)
            {
                return existingTilemap;
            }
        }

        GameObject go = new GameObject("Building Tilemap");
        go.transform.SetParent(parent, false);

        Tilemap tilemap = go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;

        TilemapRenderer terrainRenderer = terrainTilemap.GetComponent<TilemapRenderer>();
        if (terrainRenderer != null)
        {
            renderer.sortingLayerID = terrainRenderer.sortingLayerID;
            renderer.sortingOrder = terrainRenderer.sortingOrder + 1;
            renderer.mode = terrainRenderer.mode;
        }

        if (showDebugInfo)
        {
            Debug.Log("ℹ️ 未配置 buildingTilemap，已在运行时创建 Building Tilemap。");
        }

        return tilemap;
    }

    private bool ValidateConfiguration()
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("❌ 地图尺寸必须大于 0。");
            return false;
        }

        if (!terrainTiles.TryValidateBaseTiles(out string terrainError))
        {
            Debug.LogError($"❌ {terrainError}");
            return false;
        }

        bool hasAnyTransition =
            terrainTiles.soilGrassTransitions.HasConfiguredEntries() ||
            terrainTiles.waterEdgeTransitions.HasConfiguredEntries() ||
            terrainTiles.mixedTransitions.HasConfiguredEntries();

        if (!hasAnyTransition)
        {
            Debug.LogWarning("⚠️ 当前未配置任何过渡瓦片，将退回基础瓦片渲染。");
        }

        if (buildingTileAsset.ResolveTile() == null)
        {
            Debug.LogWarning("⚠️ 未配置建筑瓦片，建筑生成将被跳过。");
        }

        return true;
    }

    private void PrepareSeedForGeneration()
    {
        if (useRandomSeed)
        {
            seed = UnityEngine.Random.Range(-999999, 999999);
        }

        System.Random random = new System.Random(seed);

        lastHeightOffset = manualOffset + new Vector2(
            (float)random.NextDouble() * 10000f,
            (float)random.NextDouble() * 10000f);

        lastMoistureOffset = manualOffset + new Vector2(
            (float)random.NextDouble() * 10000f,
            (float)random.NextDouble() * 10000f);
    }

    private BuildingPlacementResult TryResolveBuildingPlacement(MapCellData[,] map)
    {
        if (buildingTileAsset.ResolveTile() == null)
        {
            return BuildingPlacementResult.None;
        }

        bool shouldTryFixedPosition = useFixedPosition || fixedPosition != Vector2Int.zero;
        if (shouldTryFixedPosition)
        {
            if (IsValidBuildingCandidate(map, fixedPosition))
            {
                return new BuildingPlacementResult(true, fixedPosition, 0f, 0);
            }

            Debug.LogWarning($"⚠️ 固定建筑位置 ({fixedPosition.x}, {fixedPosition.y}) 不满足当前地形规则，改为自动寻点。");
        }

        if (!allowGeneratedBuildingPosition)
        {
            return BuildingPlacementResult.None;
        }

        List<TerrainCluster> soilClusters = CollectClusters(map, TerrainType.Soil);
        TerrainCluster bestCluster = null;
        Vector2Int bestPosition = default;
        float bestScore = float.MinValue;

        for (int i = 0; i < soilClusters.Count; i++)
        {
            TerrainCluster cluster = soilClusters[i];
            if (cluster.cells.Count < buildingMinClusterSize)
            {
                continue;
            }

            for (int j = 0; j < cluster.cells.Count; j++)
            {
                Vector2Int candidate = cluster.cells[j];
                if (!IsValidBuildingCandidate(map, candidate))
                {
                    continue;
                }

                float score = ScoreBuildingCandidate(map, candidate, cluster);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate;
                    bestCluster = cluster;
                }
            }
        }

        if (bestCluster == null)
        {
            Debug.LogWarning("⚠️ 未找到满足条件的建筑落点，当前地图将不放置建筑。");
            return BuildingPlacementResult.None;
        }

        return new BuildingPlacementResult(true, bestPosition, bestScore, bestCluster.cells.Count);
    }

    private bool IsValidBuildingCandidate(MapCellData[,] map, Vector2Int position)
    {
        if (!IsInside(position.x, position.y))
        {
            return false;
        }

        if (position.x < minDistanceFromBorder ||
            position.x >= width - minDistanceFromBorder ||
            position.y < minDistanceFromBorder ||
            position.y >= height - minDistanceFromBorder)
        {
            return false;
        }

        TerrainType centerTerrain = map[position.x, position.y].terrainType;
        if (buildingOnSoil && centerTerrain != TerrainType.Soil)
        {
            return false;
        }

        if (!buildingOnSoil && centerTerrain == TerrainType.Water)
        {
            return false;
        }

        for (int dx = -buildingPadding; dx <= buildingPadding; dx++)
        {
            for (int dy = -buildingPadding; dy <= buildingPadding; dy++)
            {
                int checkX = position.x + dx;
                int checkY = position.y + dy;

                if (!IsInside(checkX, checkY))
                {
                    return false;
                }

                if (map[checkX, checkY].terrainType == TerrainType.Water)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float ScoreBuildingCandidate(MapCellData[,] map, Vector2Int candidate, TerrainCluster cluster)
    {
        float borderDistance = Mathf.Min(
            candidate.x,
            candidate.y,
            width - 1 - candidate.x,
            height - 1 - candidate.y);

        float distanceToClusterCenter = Vector2.Distance(candidate, cluster.center);
        int dryNeighborCount = CountTerrainInRadius(map, candidate, buildingPadding + 1, TerrainType.Soil) +
                               CountTerrainInRadius(map, candidate, buildingPadding + 1, TerrainType.Grass);

        return borderDistance * 6f - distanceToClusterCenter * 2f + dryNeighborCount;
    }

    private void ReserveBuildingArea(MapCellData[,] map, Vector2Int center)
    {
        for (int dx = -buildingPadding; dx <= buildingPadding; dx++)
        {
            for (int dy = -buildingPadding; dy <= buildingPadding; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;
                if (!IsInside(x, y))
                {
                    continue;
                }

                MapCellData cell = map[x, y];
                cell.terrainType = TerrainType.Soil;
                cell.isReservedForBuilding = true;
                map[x, y] = cell;
            }
        }
    }

    private void RenderBuilding(BuildingPlacementResult placement)
    {
        if (buildingTilemap == null)
        {
            return;
        }

        buildingTilemap.ClearAllTiles();

        if (!placement.hasBuilding)
        {
            return;
        }

        TileBase resolvedBuildingTile = buildingTileAsset.ResolveTile();
        if (resolvedBuildingTile == null)
        {
            return;
        }

        buildingTilemap.SetTile(new Vector3Int(placement.position.x, placement.position.y, 0), resolvedBuildingTile);
    }

    private void LogGenerationSummary(MapCellData[,] map, BuildingPlacementResult buildingPlacement)
    {
        int waterCount = 0;
        int soilCount = 0;
        int grassCount = 0;

        float minHeight = 1f;
        float maxHeight = 0f;
        float minMoisture = 1f;
        float maxMoisture = 0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MapCellData cell = map[x, y];

                switch (cell.terrainType)
                {
                    case TerrainType.Water:
                        waterCount++;
                        break;
                    case TerrainType.Soil:
                        soilCount++;
                        break;
                    default:
                        grassCount++;
                        break;
                }

                minHeight = Mathf.Min(minHeight, cell.heightNoise);
                maxHeight = Mathf.Max(maxHeight, cell.heightNoise);
                minMoisture = Mathf.Min(minMoisture, cell.moistureNoise);
                maxMoisture = Mathf.Max(maxMoisture, cell.moistureNoise);
            }
        }

        int total = width * height;

        Debug.Log("📊 地图生成统计:");
        Debug.Log($"   seed: {seed}");
        Debug.Log($"   heightOffset: {lastHeightOffset}");
        Debug.Log($"   moistureOffset: {lastMoistureOffset}");
        Debug.Log($"   水域: {waterCount} ({waterCount * 100f / total:F1}%)");
        Debug.Log($"   土地: {soilCount} ({soilCount * 100f / total:F1}%)");
        Debug.Log($"   草地: {grassCount} ({grassCount * 100f / total:F1}%)");
        Debug.Log($"   高度噪声范围: {minHeight:F3} ~ {maxHeight:F3}");
        Debug.Log($"   湿度噪声范围: {minMoisture:F3} ~ {maxMoisture:F3}");
        Debug.Log($"   Water 连通域: {lastStats.waterClusterCount}，清理 {lastStats.removedWaterClusters} 个小簇");
        Debug.Log($"   Soil 连通域: {lastStats.soilClusterCount}，清理 {lastStats.removedSoilClusters} 个小簇");

        if (buildingPlacement.hasBuilding)
        {
            Debug.Log($"   建筑位置: ({buildingPlacement.position.x}, {buildingPlacement.position.y})");
            Debug.Log($"   建筑簇面积: {buildingPlacement.clusterSize}");
            Debug.Log($"   建筑评分: {buildingPlacement.score:F2}");
        }
        else
        {
            Debug.Log("   建筑位置: 未放置");
        }
    }

    private int CountTerrainInRadius(MapCellData[,] map, Vector2Int center, int radius, TerrainType terrainType)
    {
        int count = 0;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;

                if (!IsInside(x, y))
                {
                    continue;
                }

                if (map[x, y].terrainType == terrainType)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private List<TerrainCluster> CollectClusters(MapCellData[,] map, TerrainType targetTerrain)
    {
        bool[,] visited = new bool[width, height];
        List<TerrainCluster> clusters = new List<TerrainCluster>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (visited[x, y] || map[x, y].terrainType != targetTerrain)
                {
                    continue;
                }

                TerrainCluster cluster = FloodFillCluster(map, x, y, targetTerrain, visited);
                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    private TerrainCluster FloodFillCluster(MapCellData[,] map, int startX, int startY, TerrainType targetTerrain, bool[,] visited)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        TerrainCluster cluster = new TerrainCluster(targetTerrain);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            cluster.cells.Add(current);
            cluster.centerAccumulator += current;

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int next = current + CardinalDirections[i];
                if (!IsInside(next.x, next.y) || visited[next.x, next.y])
                {
                    continue;
                }

                if (map[next.x, next.y].terrainType != targetTerrain)
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
            }
        }

        cluster.FinalizeCenter();
        return cluster;
    }

    private sealed class TerrainGenerator
    {
        private readonly PerlinMapWithSingleBuilding owner;
        private readonly Vector2 heightOffset;
        private readonly Vector2 moistureOffset;

        public TerrainGenerator(PerlinMapWithSingleBuilding owner, Vector2 heightOffset, Vector2 moistureOffset)
        {
            this.owner = owner;
            this.heightOffset = heightOffset;
            this.moistureOffset = moistureOffset;
        }

        public MapCellData[,] Generate()
        {
            MapCellData[,] map = new MapCellData[owner.width, owner.height];

            for (int x = 0; x < owner.width; x++)
            {
                for (int y = 0; y < owner.height; y++)
                {
                    float heightNoise = SampleNoise(x, y, owner.heightScale, heightOffset);
                    float moistureNoise = SampleNoise(x, y, owner.moistureScale, moistureOffset);

                    float edgeFactor = CalculateEdgeFactor(x, y);
                    heightNoise = Mathf.Lerp(0f, heightNoise, edgeFactor);

                    MapCellData cell = new MapCellData
                    {
                        heightNoise = heightNoise,
                        moistureNoise = moistureNoise,
                        terrainType = ResolveTerrainType(heightNoise, moistureNoise),
                        isReservedForBuilding = false
                    };

                    map[x, y] = cell;
                }
            }

            return map;
        }

        private float SampleNoise(int x, int y, float scale, Vector2 offset)
        {
            float safeScale = Mathf.Max(0.0001f, scale);
            float amplitude = 1f;
            float frequency = 1f;
            float value = 0f;
            float amplitudeSum = 0f;

            for (int octave = 0; octave < owner.octaves; octave++)
            {
                float sampleX = (x + offset.x) / safeScale * frequency;
                float sampleY = (y + offset.y) / safeScale * frequency;
                float perlin = Mathf.PerlinNoise(sampleX, sampleY);

                value += perlin * amplitude;
                amplitudeSum += amplitude;
                amplitude *= owner.persistence;
                frequency *= owner.lacunarity;
            }

            return amplitudeSum <= 0f ? 0f : value / amplitudeSum;
        }

        private float CalculateEdgeFactor(int x, int y)
        {
            int distanceToBorder = Mathf.Min(x, y, owner.width - 1 - x, owner.height - 1 - y);
            if (distanceToBorder >= owner.edgeFalloffWidth)
            {
                return 1f;
            }

            return Mathf.Clamp01(distanceToBorder / Mathf.Max(1f, owner.edgeFalloffWidth));
        }

        private TerrainType ResolveTerrainType(float heightNoise, float moistureNoise)
        {
            if (heightNoise <= owner.waterThreshold)
            {
                return TerrainType.Water;
            }

            if (moistureNoise <= owner.soilThreshold)
            {
                return TerrainType.Soil;
            }

            return TerrainType.Grass;
        }
    }

    private sealed class TerrainPostProcessor
    {
        private readonly PerlinMapWithSingleBuilding owner;

        public TerrainPostProcessor(PerlinMapWithSingleBuilding owner)
        {
            this.owner = owner;
        }

        public GenerationStats Process(MapCellData[,] map)
        {
            for (int pass = 0; pass < owner.smoothingPasses; pass++)
            {
                Smooth(map);
            }

            GenerationStats stats = new GenerationStats();
            CleanupSmallClusters(map, TerrainType.Water, owner.minWaterClusterSize, ref stats);
            CleanupSmallClusters(map, TerrainType.Soil, owner.minSoilClusterSize, ref stats);
            return stats;
        }

        private void Smooth(MapCellData[,] map)
        {
            TerrainType[,] nextTerrain = new TerrainType[owner.width, owner.height];

            for (int x = 0; x < owner.width; x++)
            {
                for (int y = 0; y < owner.height; y++)
                {
                    int water = 0;
                    int soil = 0;
                    int grass = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int neighborX = x + dx;
                            int neighborY = y + dy;

                            TerrainType terrain = TerrainType.Water;
                            if (owner.IsInside(neighborX, neighborY))
                            {
                                terrain = map[neighborX, neighborY].terrainType;
                            }

                            switch (terrain)
                            {
                                case TerrainType.Water:
                                    water++;
                                    break;
                                case TerrainType.Soil:
                                    soil++;
                                    break;
                                default:
                                    grass++;
                                    break;
                            }
                        }
                    }

                    TerrainType current = map[x, y].terrainType;
                    TerrainType dominant = current;
                    int dominantCount = GetDominantCount(current, water, soil, grass);

                    if (water > dominantCount)
                    {
                        dominant = TerrainType.Water;
                        dominantCount = water;
                    }

                    if (soil > dominantCount)
                    {
                        dominant = TerrainType.Soil;
                        dominantCount = soil;
                    }

                    if (grass > dominantCount)
                    {
                        dominant = TerrainType.Grass;
                    }

                    nextTerrain[x, y] = dominant;
                }
            }

            for (int x = 0; x < owner.width; x++)
            {
                for (int y = 0; y < owner.height; y++)
                {
                    MapCellData cell = map[x, y];
                    cell.terrainType = nextTerrain[x, y];
                    map[x, y] = cell;
                }
            }
        }

        private void CleanupSmallClusters(MapCellData[,] map, TerrainType targetTerrain, int minimumClusterSize, ref GenerationStats stats)
        {
            bool[,] visited = new bool[owner.width, owner.height];

            for (int x = 0; x < owner.width; x++)
            {
                for (int y = 0; y < owner.height; y++)
                {
                    if (visited[x, y] || map[x, y].terrainType != targetTerrain)
                    {
                        continue;
                    }

                    TerrainCluster cluster = owner.FloodFillCluster(map, x, y, targetTerrain, visited);

                    if (targetTerrain == TerrainType.Water)
                    {
                        stats.waterClusterCount++;
                    }
                    else if (targetTerrain == TerrainType.Soil)
                    {
                        stats.soilClusterCount++;
                    }

                    if (cluster.cells.Count >= minimumClusterSize)
                    {
                        continue;
                    }

                    TerrainType replacement = ResolveReplacementTerrain(map, cluster);
                    for (int i = 0; i < cluster.cells.Count; i++)
                    {
                        Vector2Int cellPosition = cluster.cells[i];
                        MapCellData cell = map[cellPosition.x, cellPosition.y];
                        cell.terrainType = replacement;
                        map[cellPosition.x, cellPosition.y] = cell;
                    }

                    if (targetTerrain == TerrainType.Water)
                    {
                        stats.removedWaterClusters++;
                    }
                    else if (targetTerrain == TerrainType.Soil)
                    {
                        stats.removedSoilClusters++;
                    }
                }
            }
        }

        private TerrainType ResolveReplacementTerrain(MapCellData[,] map, TerrainCluster cluster)
        {
            int waterNeighbors = 0;
            int soilNeighbors = 0;
            int grassNeighbors = 0;

            for (int i = 0; i < cluster.cells.Count; i++)
            {
                Vector2Int cell = cluster.cells[i];

                for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
                {
                    Vector2Int neighbor = cell + CardinalDirections[directionIndex];
                    if (!owner.IsInside(neighbor.x, neighbor.y))
                    {
                        continue;
                    }

                    TerrainType terrain = map[neighbor.x, neighbor.y].terrainType;
                    if (terrain == cluster.terrainType)
                    {
                        continue;
                    }

                    switch (terrain)
                    {
                        case TerrainType.Water:
                            waterNeighbors++;
                            break;
                        case TerrainType.Soil:
                            soilNeighbors++;
                            break;
                        default:
                            grassNeighbors++;
                            break;
                    }
                }
            }

            if (cluster.terrainType == TerrainType.Water)
            {
                return soilNeighbors > grassNeighbors ? TerrainType.Soil : TerrainType.Grass;
            }

            if (cluster.terrainType == TerrainType.Soil)
            {
                return waterNeighbors > grassNeighbors ? TerrainType.Water : TerrainType.Grass;
            }

            return TerrainType.Grass;
        }

        private static int GetDominantCount(TerrainType terrain, int water, int soil, int grass)
        {
            switch (terrain)
            {
                case TerrainType.Water:
                    return water;
                case TerrainType.Soil:
                    return soil;
                default:
                    return grass;
            }
        }
    }

    private sealed class TerrainRenderer
    {
        private readonly PerlinMapWithSingleBuilding owner;

        public TerrainRenderer(PerlinMapWithSingleBuilding owner)
        {
            this.owner = owner;
        }

        public void Render(MapCellData[,] map)
        {
            TileBase[] tiles = new TileBase[owner.width * owner.height];

            for (int y = 0; y < owner.height; y++)
            {
                for (int x = 0; x < owner.width; x++)
                {
                    int index = x + y * owner.width;
                    tiles[index] = ResolveTile(map, x, y);
                }
            }

            BoundsInt bounds = new BoundsInt(0, 0, 0, owner.width, owner.height, 1);
            owner.terrainTilemap.ClearAllTiles();
            owner.terrainTilemap.SetTilesBlock(bounds, tiles);
        }

        private TileBase ResolveTile(MapCellData[,] map, int x, int y)
        {
            TerrainType terrain = map[x, y].terrainType;
            int variantSeed = GetVariantSeed(x, y, terrain);

            if (terrain == TerrainType.Water)
            {
                return owner.terrainTiles.ResolveBaseTile(TerrainType.Water);
            }

            int waterMask = BuildMask(map, x, y, neighborTerrain => neighborTerrain == TerrainType.Water);
            int waterDiagonalMask = BuildDiagonalMask(map, x, y, neighborTerrain => neighborTerrain == TerrainType.Water);

            if (terrain == TerrainType.Soil)
            {
                int grassMask = BuildMask(map, x, y, neighborTerrain => neighborTerrain == TerrainType.Grass);
                int grassDiagonalMask = BuildDiagonalMask(map, x, y, neighborTerrain => neighborTerrain == TerrainType.Grass);

                if (waterMask != 0 && grassMask != 0 &&
                    owner.terrainTiles.mixedTransitions.TryResolve(waterMask, waterDiagonalMask, variantSeed, out TileBase mixedTile))
                {
                    return mixedTile;
                }

                if (waterMask != 0 &&
                    owner.terrainTiles.waterEdgeTransitions.TryResolve(waterMask, waterDiagonalMask, variantSeed, out TileBase waterEdgeTile))
                {
                    return waterEdgeTile;
                }

                if (grassMask != 0 &&
                    owner.terrainTiles.soilGrassTransitions.TryResolve(grassMask, grassDiagonalMask, variantSeed, out TileBase soilGrassTile))
                {
                    return soilGrassTile;
                }

                return owner.terrainTiles.ResolveBaseTile(TerrainType.Soil);
            }

            if (waterMask != 0 &&
                owner.terrainTiles.waterEdgeTransitions.TryResolve(waterMask, waterDiagonalMask, variantSeed, out TileBase grassWaterTile))
            {
                return grassWaterTile;
            }

            return owner.terrainTiles.ResolveBaseTile(TerrainType.Grass);
        }

        private int BuildMask(MapCellData[,] map, int x, int y, Func<TerrainType, bool> isExposedTerrain)
        {
            int mask = 0;

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int direction = CardinalDirections[i];
                int neighborX = x + direction.x;
                int neighborY = y + direction.y;

                TerrainType terrain = owner.IsInside(neighborX, neighborY)
                    ? map[neighborX, neighborY].terrainType
                    : TerrainType.Water;

                if (isExposedTerrain(terrain))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private int BuildDiagonalMask(MapCellData[,] map, int x, int y, Func<TerrainType, bool> isExposedTerrain)
        {
            int mask = 0;

            for (int i = 0; i < DiagonalDirections.Length; i++)
            {
                Vector2Int direction = DiagonalDirections[i];
                int neighborX = x + direction.x;
                int neighborY = y + direction.y;

                TerrainType terrain = owner.IsInside(neighborX, neighborY)
                    ? map[neighborX, neighborY].terrainType
                    : TerrainType.Water;

                if (isExposedTerrain(terrain))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static int GetVariantSeed(int x, int y, TerrainType terrain)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + (int)terrain;
                return hash;
            }
        }
    }

    private readonly struct BuildingPlacementResult
    {
        public static readonly BuildingPlacementResult None = new BuildingPlacementResult(false, default, 0f, 0);

        public readonly bool hasBuilding;
        public readonly Vector2Int position;
        public readonly float score;
        public readonly int clusterSize;

        public BuildingPlacementResult(bool hasBuilding, Vector2Int position, float score, int clusterSize)
        {
            this.hasBuilding = hasBuilding;
            this.position = position;
            this.score = score;
            this.clusterSize = clusterSize;
        }
    }

    private sealed class TerrainCluster
    {
        public readonly TerrainType terrainType;
        public readonly List<Vector2Int> cells = new List<Vector2Int>();
        public Vector2 centerAccumulator;
        public Vector2 center;

        public TerrainCluster(TerrainType terrainType)
        {
            this.terrainType = terrainType;
        }

        public void FinalizeCenter()
        {
            center = cells.Count == 0 ? Vector2.zero : centerAccumulator / cells.Count;
        }
    }

    private struct GenerationStats
    {
        public int waterClusterCount;
        public int soilClusterCount;
        public int removedWaterClusters;
        public int removedSoilClusters;
    }
}
