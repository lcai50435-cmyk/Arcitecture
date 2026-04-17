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

    [Header("鑽夊湡杩囨浮")]
    public TransitionTileLookup soilGrassTransitions = new TransitionTileLookup();

    [Header("姘磋竟杩囨浮")]
    public TransitionTileLookup waterEdgeTransitions = new TransitionTileLookup();

    [Header("Mixed Terrain Transitions")]
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

        errorMessage = $"鍩虹鍦板舰璧勬簮缂哄け: {string.Join(", ", missing)}";
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
    private const string DefaultPlaceableRootName = "Generated Placeables";
    private const string PoolRootName = "_Pooled Placeables";
    private const string DefaultWaterCollisionTilemapName = "Water Collision Tilemap";

    [Header("Tilemap 寮曠敤")]
    public Tilemap terrainTilemap;
    public Tilemap waterCollisionTilemap;
    public Transform generatedPlaceablesParent;

    [Header("鍩虹鍦板舰璧勬簮")]
    public TerrainTileCatalog terrainTiles = new TerrainTileCatalog();
    public List<PlaceableCategoryConfig> placeableCategories = new List<PlaceableCategoryConfig>();

    [Header("鍦板浘灏哄")]
    public int width = 80;
    public int height = 80;

    [Header("鍣０鍙傛暟")]
    public bool useRandomSeed = true;
    public int seed = 12345;
    [FormerlySerializedAs("offset")] public Vector2 manualOffset;
    [FormerlySerializedAs("scale")] public float heightScale = 18f;
    public float moistureScale = 12f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Threshold Settings")]
    [Range(0f, 1f)] public float waterThreshold = 0.32f;
    [FormerlySerializedAs("dirtThreshold"), Range(0f, 1f)] public float soilThreshold = 0.52f;
    [Min(1)] public int edgeFalloffWidth = 6;

    [Header("Terrain Distribution")]
    public bool useTargetTerrainDistribution = true;
    [Range(0.05f, 0.95f)] public float targetGrassRatio = 0.6f;
    [Range(0.01f, 0.8f)] public float targetSoilRatio = 0.3f;
    [Range(0.01f, 0.5f)] public float targetWaterRatio = 0.1f;
    public bool preferInlandWater = true;
    [Min(0)] public int inlandWaterBorderPadding = 6;
    [Range(0f, 1f)] public float inlandWaterBorderPenalty = 0.2f;
    public bool applyEdgeFalloffToHeightNoise = false;
    [Min(1)] public int inlandWaterMinClusterSize = 4;

    [Header("Water Collision")]
    public bool generateWaterCollision = true;
    [Min(0)] public int waterCollisionPadding = 0;
    public bool hideWaterCollisionTilemapRenderer = true;

    [Header("鑱氶泦鍙傛暟")]
    [Range(0, 3)] public int smoothingPasses = 1;
    [Min(1)] public int minWaterClusterSize = 12;
    [Min(1)] public int minSoilClusterSize = 10;

    [Header("寤虹瓚鍙傛暟")]

    [Header("璋冭瘯鍙傛暟")]
    public bool showDebugInfo = true;
    public bool clearBeforeGenerate = true;

    [FormerlySerializedAs("grassTile"), SerializeField, HideInInspector]
    private TileBase legacyGrassTile;

    [FormerlySerializedAs("dirtTile"), SerializeField, HideInInspector]
    private TileBase legacySoilTile;

    [FormerlySerializedAs("buildingTilemap"), SerializeField, HideInInspector]
    private Tilemap legacyBuildingTilemap;

    [FormerlySerializedAs("buildingTileAsset"), SerializeField, HideInInspector]
    private TileAssetReference legacyBuildingTileAsset = new TileAssetReference();

    [FormerlySerializedAs("buildingTile"), SerializeField, HideInInspector]
    private TileBase legacyBuildingTile;

    // Legacy serialized fields are kept for scene migration compatibility.
    #pragma warning disable CS0414
    [FormerlySerializedAs("buildingOnSoil")]
    [FormerlySerializedAs("buildingOnDirt")]
    [SerializeField, HideInInspector]
    private bool legacyBuildingOnSoil = true;

    [FormerlySerializedAs("useFixedPosition"), SerializeField, HideInInspector]
    private bool legacyUseFixedPosition;

    [FormerlySerializedAs("fixedPosition"), SerializeField, HideInInspector]
    private Vector2Int legacyFixedPosition;

    [FormerlySerializedAs("allowGeneratedBuildingPosition")]
    [FormerlySerializedAs("randomPosition")]
    [SerializeField, HideInInspector]
    private bool legacyAllowGeneratedBuildingPosition = true;

    [FormerlySerializedAs("buildingMinClusterSize"), SerializeField, HideInInspector]
    private int legacyBuildingMinClusterSize = 18;

    [FormerlySerializedAs("buildingPadding"), SerializeField, HideInInspector]
    private int legacyBuildingPadding = 1;

    [FormerlySerializedAs("minDistanceFromBorder"), SerializeField, HideInInspector]
    private int legacyMinDistanceFromBorder = 3;
    #pragma warning restore CS0414

    private Vector2 lastHeightOffset;
    private Vector2 lastMoistureOffset;
    private GenerationStats lastStats;
    private Tile runtimeWaterCollisionTile;
    private RuntimePrefabPool runtimePrefabPool;
    private Transform runtimePoolRoot;
    private readonly Dictionary<string, Transform> categoryParentCache = new Dictionary<string, Transform>();

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

        ReleaseGeneratedPlaceables();

        if (clearBeforeGenerate)
        {
            terrainTilemap.ClearAllTiles();
            ClearWaterCollisionTilemap();
        }

        PrepareSeedForGeneration();

        TerrainGenerator generator = new TerrainGenerator(this, lastHeightOffset, lastMoistureOffset);
        MapCellData[,] map = generator.Generate();

        TerrainPostProcessor postProcessor = new TerrainPostProcessor(this);
        lastStats = postProcessor.Process(map);

        List<ResolvedPlaceablePlacement> placeablePlacements = ResolvePlaceablePlacements(map);

        TerrainRenderer renderer = new TerrainRenderer(this);
        renderer.Render(map);
        RenderWaterCollision(map);
        RenderPlaceables(placeablePlacements);

        if (showDebugInfo)
        {
            LogGenerationSummary(map, placeablePlacements);
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

        if (legacyBuildingTileAsset == null)
        {
            legacyBuildingTileAsset = new TileAssetReference();
        }

        if (legacyBuildingTileAsset.IsEmpty && legacyBuildingTile != null)
        {
            legacyBuildingTileAsset.tile = legacyBuildingTile;
        }

        if (placeableCategories == null)
        {
            placeableCategories = new List<PlaceableCategoryConfig>();
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

                if (!IsValidTerrainTilemapCandidate(childTilemaps[i]))
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

                        if (!IsValidTerrainTilemapCandidate(tilemaps[i]))
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
                Debug.Log($"鈩癸笍 鑷姩缁戝畾 terrainTilemap: {terrainTilemap.name}");
            }
        }

        if (terrainTilemap == null)
        {
            Debug.LogError("No usable terrainTilemap was found. Map generation has been aborted.");
            return false;
        }

        if (generateWaterCollision)
        {
            if (waterCollisionTilemap == null)
            {
                waterCollisionTilemap = TryFindExistingWaterCollisionTilemap();
            }

            if (waterCollisionTilemap == null)
            {
                waterCollisionTilemap = CreateRuntimeWaterCollisionTilemap();

                if (showDebugInfo && waterCollisionTilemap != null)
                {
                    Debug.Log($"Created runtime water collision tilemap: {waterCollisionTilemap.name}");
                }
            }

            EnsureWaterCollisionTilemapSetup();
        }

        if (generatedPlaceablesParent == null)
        {
            generatedPlaceablesParent = TryFindExistingPlaceableRoot();
        }

        bool hasConfiguredPlaceables = false;
        if (placeableCategories != null)
        {
            for (int i = 0; i < placeableCategories.Count; i++)
            {
                PlaceableCategoryConfig category = placeableCategories[i];
                if (category == null)
                {
                    Debug.LogWarning($"Placeable category at index {i} is null and will be skipped.");
                    continue;
                }

                if (category.spawnCount <= 0)
                {
                    continue;
                }

                if (category.allowedTerrainTypes == null || category.allowedTerrainTypes.Count == 0)
                {
                    Debug.LogWarning($"Placeable category '{category.GetDisplayName()}' has no allowed terrain types and will be skipped.");
                    continue;
                }

                if (!category.HasUsablePrefabs())
                {
                    Debug.LogWarning($"Placeable category '{category.GetDisplayName()}' has no usable prefab entries and will be skipped.");
                    continue;
                }

                hasConfiguredPlaceables = true;
            }
        }

        if (!hasConfiguredPlaceables)
        {
            Debug.LogWarning("No prefab-based placeable categories are configured. Only tile terrain will be generated.");
        }

        if (HasLegacyBuildingTileSetup())
        {
            Debug.LogWarning("Legacy building tile settings are deprecated. Configure prefab-based placeable categories instead.");
        }

        return true;
    }

    private Transform TryFindExistingPlaceableRoot()
    {
        Transform parent = terrainTilemap.transform.parent != null
            ? terrainTilemap.transform.parent
            : terrainTilemap.transform;

        return parent.Find(DefaultPlaceableRootName);
    }

    private Tilemap TryFindExistingWaterCollisionTilemap()
    {
        Transform parent = terrainTilemap.transform.parent != null
            ? terrainTilemap.transform.parent
            : terrainTilemap.transform;

        Transform existingChild = parent.Find(DefaultWaterCollisionTilemapName);
        if (existingChild != null)
        {
            Tilemap childTilemap = existingChild.GetComponent<Tilemap>();
            if (childTilemap != null)
            {
                return childTilemap;
            }
        }

        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i] == null || tilemaps[i] == terrainTilemap)
            {
                continue;
            }

            if (!IsWaterCollisionTilemapCandidate(tilemaps[i]))
            {
                continue;
            }

            return tilemaps[i];
        }

        return null;
    }

    private Tilemap CreateRuntimeWaterCollisionTilemap()
    {
        Transform parent = terrainTilemap.transform.parent != null
            ? terrainTilemap.transform.parent
            : terrainTilemap.transform;

        Transform existing = parent.Find(DefaultWaterCollisionTilemapName);
        if (existing != null)
        {
            Tilemap existingTilemap = existing.GetComponent<Tilemap>();
            if (existingTilemap != null)
            {
                return existingTilemap;
            }
        }

        GameObject go = new GameObject(DefaultWaterCollisionTilemapName);
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(terrainTilemap.transform.GetSiblingIndex() + 1);

        Tilemap tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return tilemap;
    }

    private void EnsureWaterCollisionTilemapSetup()
    {
        if (waterCollisionTilemap == null)
        {
            return;
        }

        if (waterCollisionTilemap == terrainTilemap)
        {
            return;
        }

        TilemapRenderer renderer = waterCollisionTilemap.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = waterCollisionTilemap.gameObject.AddComponent<TilemapRenderer>();
        }

        renderer.enabled = !hideWaterCollisionTilemapRenderer;

        TilemapCollider2D tilemapCollider = waterCollisionTilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = waterCollisionTilemap.gameObject.AddComponent<TilemapCollider2D>();
        }

        CompositeCollider2D compositeCollider = waterCollisionTilemap.GetComponent<CompositeCollider2D>();
        if (compositeCollider == null)
        {
            compositeCollider = waterCollisionTilemap.gameObject.AddComponent<CompositeCollider2D>();
        }

        Rigidbody2D body = waterCollisionTilemap.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = waterCollisionTilemap.gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Static;
        tilemapCollider.usedByComposite = true;
    }

    private void ClearWaterCollisionTilemap()
    {
        if (waterCollisionTilemap != null)
        {
            waterCollisionTilemap.ClearAllTiles();
        }
    }

    private void RenderWaterCollision(MapCellData[,] map)
    {
        if (!generateWaterCollision || waterCollisionTilemap == null)
        {
            return;
        }

        TileBase collisionTile = ResolveWaterCollisionTile();
        if (collisionTile == null)
        {
            Debug.LogError("Water collision tile could not be created.");
            return;
        }

        TileBase[] collisionTiles = new TileBase[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                collisionTiles[index] = ShouldPlaceWaterCollisionAt(map, x, y) ? collisionTile : null;
            }
        }

        BoundsInt bounds = new BoundsInt(0, 0, 0, width, height, 1);
        waterCollisionTilemap.ClearAllTiles();
        waterCollisionTilemap.SetTilesBlock(bounds, collisionTiles);
    }

    private bool ShouldPlaceWaterCollisionAt(MapCellData[,] map, int x, int y)
    {
        if (map[x, y].terrainType == TerrainType.Water)
        {
            return true;
        }

        if (waterCollisionPadding <= 0)
        {
            return false;
        }

        for (int dx = -waterCollisionPadding; dx <= waterCollisionPadding; dx++)
        {
            for (int dy = -waterCollisionPadding; dy <= waterCollisionPadding; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;
                if (!IsInside(checkX, checkY))
                {
                    continue;
                }

                if (map[checkX, checkY].terrainType == TerrainType.Water)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private TileBase ResolveWaterCollisionTile()
    {
        if (runtimeWaterCollisionTile == null)
        {
            runtimeWaterCollisionTile = ScriptableObject.CreateInstance<Tile>();
            runtimeWaterCollisionTile.name = "RuntimeWaterCollisionTile";
            runtimeWaterCollisionTile.colliderType = Tile.ColliderType.Grid;
            runtimeWaterCollisionTile.color = new Color(1f, 1f, 1f, 0f);
        }

        return runtimeWaterCollisionTile;
    }

    private static bool IsValidTerrainTilemapCandidate(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            return false;
        }

        string normalized = NormalizeTilemapName(tilemap.name);
        return !normalized.Contains("building") &&
               !normalized.Contains("collision") &&
               !normalized.Contains("collider");
    }

    private static bool IsWaterCollisionTilemapCandidate(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            return false;
        }

        string normalized = NormalizeTilemapName(tilemap.name);
        return normalized.Contains("water") &&
               (normalized.Contains("collision") || normalized.Contains("collider") || normalized.Contains("block"));
    }

    private static string NormalizeTilemapName(string tilemapName)
    {
        return string.IsNullOrWhiteSpace(tilemapName)
            ? string.Empty
            : tilemapName.Replace(" ", string.Empty).ToLowerInvariant();
    }

    private bool ValidateConfiguration()
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Map width and height must both be greater than 0.");
            return false;
        }
        if (!terrainTiles.TryValidateBaseTiles(out string terrainError))
        {
            Debug.LogError(terrainError);
            return false;
        }
        bool hasAnyTransition =
            terrainTiles.soilGrassTransitions.HasConfiguredEntries() ||
            terrainTiles.waterEdgeTransitions.HasConfiguredEntries() ||
            terrainTiles.mixedTransitions.HasConfiguredEntries();
        if (!hasAnyTransition)
        {
            Debug.LogWarning("No transition tiles are configured. Terrain rendering will fall back to base tiles.");
        }
        if (useTargetTerrainDistribution && targetGrassRatio + targetSoilRatio + targetWaterRatio <= 0f)
        {
            Debug.LogError("Terrain distribution ratios must add up to a positive value.");
            return false;
        }
        if (generateWaterCollision && waterCollisionTilemap != null && waterCollisionTilemap == terrainTilemap)
        {
            Debug.LogError("terrainTilemap and waterCollisionTilemap cannot reference the same Tilemap.");
            return false;
        }
        return true;
    }
    private bool HasLegacyBuildingTileSetup()
    {
        return legacyBuildingTilemap != null ||
               legacyBuildingTile != null ||
               (legacyBuildingTileAsset != null && !legacyBuildingTileAsset.IsEmpty);
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

    private void ResolveTerrainDistribution(MapCellData[,] map)
    {
        if (useTargetTerrainDistribution)
        {
            ApplyTargetTerrainDistribution(map);
            return;
        }

        ApplyThresholdTerrainDistribution(map);
    }

    private void ApplyThresholdTerrainDistribution(MapCellData[,] map)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MapCellData cell = map[x, y];
                cell.terrainType = ResolveTerrainType(cell.heightNoise, cell.moistureNoise);
                map[x, y] = cell;
            }
        }
    }

    private void ApplyTargetTerrainDistribution(MapCellData[,] map)
    {
        TerrainDistributionTargets targets = GetNormalizedTerrainDistributionTargets();
        int totalCells = width * height;
        int desiredWaterCount = Mathf.Clamp(Mathf.RoundToInt(totalCells * targets.waterRatio), 0, totalCells);
        int desiredSoilCount = Mathf.Clamp(Mathf.RoundToInt(totalCells * targets.soilRatio), 0, totalCells - desiredWaterCount);

        List<TerrainAssignmentCandidate> waterCandidates = new List<TerrainAssignmentCandidate>(totalCells);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MapCellData cell = map[x, y];
                waterCandidates.Add(new TerrainAssignmentCandidate(
                    x,
                    y,
                    GetWaterSuitability(x, y, cell.heightNoise),
                    GetDeterministicTieBreaker(x, y, 1)));
            }
        }

        waterCandidates.Sort(TerrainAssignmentCandidateComparer.Instance);

        bool[,] isWater = new bool[width, height];
        for (int i = 0; i < desiredWaterCount && i < waterCandidates.Count; i++)
        {
            TerrainAssignmentCandidate candidate = waterCandidates[i];
            isWater[candidate.x, candidate.y] = true;
        }

        List<TerrainAssignmentCandidate> soilCandidates = new List<TerrainAssignmentCandidate>(totalCells - desiredWaterCount);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (isWater[x, y])
                {
                    continue;
                }

                soilCandidates.Add(new TerrainAssignmentCandidate(
                    x,
                    y,
                    map[x, y].moistureNoise,
                    GetDeterministicTieBreaker(x, y, 2)));
            }
        }

        soilCandidates.Sort(TerrainAssignmentCandidateComparer.Instance);

        bool[,] isSoil = new bool[width, height];
        for (int i = 0; i < desiredSoilCount && i < soilCandidates.Count; i++)
        {
            TerrainAssignmentCandidate candidate = soilCandidates[i];
            isSoil[candidate.x, candidate.y] = true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MapCellData cell = map[x, y];
                if (isWater[x, y])
                {
                    cell.terrainType = TerrainType.Water;
                }
                else if (isSoil[x, y])
                {
                    cell.terrainType = TerrainType.Soil;
                }
                else
                {
                    cell.terrainType = TerrainType.Grass;
                }

                map[x, y] = cell;
            }
        }
    }

    private TerrainDistributionTargets GetNormalizedTerrainDistributionTargets()
    {
        float grass = Mathf.Max(0f, targetGrassRatio);
        float soil = Mathf.Max(0f, targetSoilRatio);
        float water = Mathf.Max(0f, targetWaterRatio);
        float total = grass + soil + water;

        if (total <= 0f)
        {
            return new TerrainDistributionTargets(0.6f, 0.3f, 0.1f);
        }

        return new TerrainDistributionTargets(grass / total, soil / total, water / total);
    }

    private float GetWaterSuitability(int x, int y, float heightNoise)
    {
        float suitability = heightNoise;

        if (!preferInlandWater || inlandWaterBorderPadding <= 0)
        {
            return suitability;
        }

        int distanceToBorder = Mathf.Min(x, y, width - 1 - x, height - 1 - y);
        float borderFactor = 1f - Mathf.Clamp01(distanceToBorder / Mathf.Max(1f, inlandWaterBorderPadding));
        suitability += borderFactor * inlandWaterBorderPenalty;
        return suitability;
    }

    private int GetDeterministicTieBreaker(int x, int y, int salt)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + seed;
            hash = hash * 31 + salt;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            return hash;
        }
    }

    private int GetMinimumClusterSize(TerrainType terrainType)
    {
        if (terrainType == TerrainType.Water)
        {
            if (preferInlandWater)
            {
                return Mathf.Max(1, Mathf.Min(minWaterClusterSize, inlandWaterMinClusterSize));
            }

            return minWaterClusterSize;
        }

        return minSoilClusterSize;
    }

    private TerrainType ResolveTerrainType(float heightNoise, float moistureNoise)
    {
        if (heightNoise <= waterThreshold)
        {
            return TerrainType.Water;
        }

        if (moistureNoise <= soilThreshold)
        {
            return TerrainType.Soil;
        }

        return TerrainType.Grass;
    }

    #if false
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

            Debug.LogWarning($"鈿狅笍 鍥哄畾寤虹瓚浣嶇疆 ({fixedPosition.x}, {fixedPosition.y}) 涓嶆弧瓒冲綋鍓嶅湴褰㈣鍒欙紝鏀逛负鑷姩瀵荤偣銆?);
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
            Debug.LogWarning("鈿狅笍 鏈壘鍒版弧瓒虫潯浠剁殑寤虹瓚钀界偣锛屽綋鍓嶅湴鍥惧皢涓嶆斁缃缓绛戙€?);
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

        Debug.Log("馃搳 鍦板浘鐢熸垚缁熻:");
        Debug.Log($"   seed: {seed}");
        Debug.Log($"   heightOffset: {lastHeightOffset}");
        Debug.Log($"   moistureOffset: {lastMoistureOffset}");
        Debug.Log($"   姘村煙: {waterCount} ({waterCount * 100f / total:F1}%)");
        Debug.Log($"   鍦熷湴: {soilCount} ({soilCount * 100f / total:F1}%)");
        Debug.Log($"   鑽夊湴: {grassCount} ({grassCount * 100f / total:F1}%)");
        Debug.Log($"   楂樺害鍣０鑼冨洿: {minHeight:F3} ~ {maxHeight:F3}");
        Debug.Log($"   婀垮害鍣０鑼冨洿: {minMoisture:F3} ~ {maxMoisture:F3}");
        Debug.Log($"   Water 杩為€氬煙: {lastStats.waterClusterCount}锛屾竻鐞?{lastStats.removedWaterClusters} 涓皬绨?);
        Debug.Log($"   Soil 杩為€氬煙: {lastStats.soilClusterCount}锛屾竻鐞?{lastStats.removedSoilClusters} 涓皬绨?);

        if (buildingPlacement.hasBuilding)
        {
            Debug.Log($"   寤虹瓚浣嶇疆: ({buildingPlacement.position.x}, {buildingPlacement.position.y})");
            Debug.Log($"   寤虹瓚绨囬潰绉? {buildingPlacement.clusterSize}");
            Debug.Log($"   寤虹瓚璇勫垎: {buildingPlacement.score:F2}");
        }
        else
        {
            Debug.Log("   寤虹瓚浣嶇疆: 鏈斁缃?);
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

    #endif

    private List<ResolvedPlaceablePlacement> ResolvePlaceablePlacements(MapCellData[,] map)
    {
        List<ResolvedPlaceablePlacement> placements = new List<ResolvedPlaceablePlacement>();
        List<PlaceableCategoryConfig> categories = GetConfiguredPlaceableCategories();
        if (categories.Count == 0)
        {
            return placements;
        }

        PlaceableOccupancyGrid occupancyGrid = new PlaceableOccupancyGrid(width, height);
        System.Random prefabRandom = CreatePlaceableRandom();

        for (int categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            PlaceableCategoryConfig category = categories[categoryIndex];
            List<TerrainCluster> eligibleClusters = CollectPlaceableClusters(map, category);

            if (eligibleClusters.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Category '{category.GetDisplayName()}' has no eligible terrain clusters.");
                }

                continue;
            }

            for (int spawnIndex = 0; spawnIndex < category.spawnCount; spawnIndex++)
            {
                PlaceableCandidate candidate;
                if (!TryResolvePlaceableCandidate(map, occupancyGrid, category, eligibleClusters, categoryIndex, spawnIndex, out candidate))
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning(
                            $"Category '{category.GetDisplayName()}' could only place {spawnIndex} of {category.spawnCount} requested instances.");
                    }

                    break;
                }

                GameObject prefab = ResolveRandomPrefab(category, prefabRandom);
                if (prefab == null)
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"Category '{category.GetDisplayName()}' has no weighted prefab available.");
                    }

                    break;
                }

                ReservePlaceableArea(map, occupancyGrid, candidate.position, category.GetReservationRadius());
                placements.Add(new ResolvedPlaceablePlacement(category, prefab, candidate.position, candidate.score, candidate.clusterSize));
            }
        }

        return placements;
    }

    private List<PlaceableCategoryConfig> GetConfiguredPlaceableCategories()
    {
        List<PlaceableCategoryConfig> categories = new List<PlaceableCategoryConfig>();

        if (placeableCategories == null)
        {
            return categories;
        }

        for (int i = 0; i < placeableCategories.Count; i++)
        {
            PlaceableCategoryConfig category = placeableCategories[i];
            if (category == null ||
                category.spawnCount <= 0 ||
                category.allowedTerrainTypes == null ||
                category.allowedTerrainTypes.Count == 0 ||
                !category.HasUsablePrefabs())
            {
                continue;
            }

            categories.Add(category);
        }

        categories.Sort(ComparePlaceableCategories);
        return categories;
    }

    private static int ComparePlaceableCategories(PlaceableCategoryConfig left, PlaceableCategoryConfig right)
    {
        int priorityComparison = right.placementPriority.CompareTo(left.placementPriority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        if (left.categoryKind != right.categoryKind)
        {
            return left.categoryKind == PlaceableCategoryKind.Building ? -1 : 1;
        }

        return string.CompareOrdinal(left.GetDisplayName(), right.GetDisplayName());
    }

    private List<TerrainCluster> CollectPlaceableClusters(MapCellData[,] map, PlaceableCategoryConfig category)
    {
        List<TerrainCluster> clusters = new List<TerrainCluster>();
        List<TerrainType> uniqueTerrains = new List<TerrainType>();

        for (int i = 0; i < category.allowedTerrainTypes.Count; i++)
        {
            TerrainType terrain = category.allowedTerrainTypes[i];
            if (!uniqueTerrains.Contains(terrain))
            {
                uniqueTerrains.Add(terrain);
            }
        }

        for (int i = 0; i < uniqueTerrains.Count; i++)
        {
            clusters.AddRange(CollectClusters(map, uniqueTerrains[i]));
        }

        return clusters;
    }

    private bool TryResolvePlaceableCandidate(
        MapCellData[,] map,
        PlaceableOccupancyGrid occupancyGrid,
        PlaceableCategoryConfig category,
        List<TerrainCluster> eligibleClusters,
        int categoryIndex,
        int spawnIndex,
        out PlaceableCandidate bestCandidate)
    {
        bestCandidate = default;
        bool hasCandidate = false;

        for (int i = 0; i < eligibleClusters.Count; i++)
        {
            TerrainCluster cluster = eligibleClusters[i];
            if (cluster.cells.Count < category.minimumClusterSize)
            {
                continue;
            }

            for (int cellIndex = 0; cellIndex < cluster.cells.Count; cellIndex++)
            {
                Vector2Int candidate = cluster.cells[cellIndex];
                if (!IsValidPlaceableCandidate(map, occupancyGrid, category, candidate))
                {
                    continue;
                }

                float score = ScorePlaceableCandidate(map, category, candidate, cluster, categoryIndex, spawnIndex);
                if (!hasCandidate || score > bestCandidate.score)
                {
                    bestCandidate = new PlaceableCandidate(candidate, score, cluster.cells.Count);
                    hasCandidate = true;
                }
            }
        }

        return hasCandidate;
    }

    private bool IsValidPlaceableCandidate(
        MapCellData[,] map,
        PlaceableOccupancyGrid occupancyGrid,
        PlaceableCategoryConfig category,
        Vector2Int position)
    {
        if (!IsInside(position.x, position.y))
        {
            return false;
        }

        if (position.x < category.minDistanceFromBorder ||
            position.x >= width - category.minDistanceFromBorder ||
            position.y < category.minDistanceFromBorder ||
            position.y >= height - category.minDistanceFromBorder)
        {
            return false;
        }

        if (!category.AllowsTerrain(map[position.x, position.y].terrainType))
        {
            return false;
        }

        int reservationRadius = category.GetReservationRadius();
        for (int dx = -reservationRadius; dx <= reservationRadius; dx++)
        {
            for (int dy = -reservationRadius; dy <= reservationRadius; dy++)
            {
                int checkX = position.x + dx;
                int checkY = position.y + dy;

                if (!IsInside(checkX, checkY))
                {
                    return false;
                }

                if (occupancyGrid.IsOccupied(checkX, checkY))
                {
                    return false;
                }

                if (Mathf.Abs(dx) <= category.footprintRadius &&
                    Mathf.Abs(dy) <= category.footprintRadius &&
                    !category.AllowsTerrain(map[checkX, checkY].terrainType))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float ScorePlaceableCandidate(
        MapCellData[,] map,
        PlaceableCategoryConfig category,
        Vector2Int candidate,
        TerrainCluster cluster,
        int categoryIndex,
        int spawnIndex)
    {
        float borderDistance = Mathf.Min(
            candidate.x,
            candidate.y,
            width - 1 - candidate.x,
            height - 1 - candidate.y);

        float distanceToClusterCenter = Vector2.Distance(candidate, cluster.center);
        int supportCount = CountAllowedTerrainInRadius(map, candidate, Mathf.Max(1, category.footprintRadius + 1), category);
        float jitter = GetPlacementJitter(candidate, categoryIndex, spawnIndex);

        return borderDistance * category.borderScoreWeight -
               distanceToClusterCenter * category.clusterCenterPenaltyWeight +
               supportCount * category.terrainSupportWeight +
               jitter * category.randomJitterWeight;
    }

    private int CountAllowedTerrainInRadius(MapCellData[,] map, Vector2Int center, int radius, PlaceableCategoryConfig category)
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

                if (category.AllowsTerrain(map[x, y].terrainType))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private float GetPlacementJitter(Vector2Int candidate, int categoryIndex, int spawnIndex)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + seed;
            hash = hash * 31 + categoryIndex;
            hash = hash * 31 + spawnIndex;
            hash = hash * 31 + candidate.x;
            hash = hash * 31 + candidate.y;
            uint positiveHash = (uint)hash;
            return (positiveHash % 1000) / 1000f;
        }
    }

    private System.Random CreatePlaceableRandom()
    {
        unchecked
        {
            int placementSeed = seed * 397 ^ 0x51A7F00D;
            return new System.Random(placementSeed);
        }
    }

    private GameObject ResolveRandomPrefab(PlaceableCategoryConfig category, System.Random random)
    {
        int totalWeight = 0;

        for (int i = 0; i < category.prefabs.Count; i++)
        {
            PlaceablePrefabEntry entry = category.prefabs[i];
            if (entry != null && entry.IsUsable())
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = random.Next(totalWeight);
        for (int i = 0; i < category.prefabs.Count; i++)
        {
            PlaceablePrefabEntry entry = category.prefabs[i];
            if (entry == null || !entry.IsUsable())
            {
                continue;
            }

            if (roll < entry.weight)
            {
                return entry.prefab;
            }

            roll -= entry.weight;
        }

        return null;
    }

    private void ReservePlaceableArea(
        MapCellData[,] map,
        PlaceableOccupancyGrid occupancyGrid,
        Vector2Int center,
        int radius)
    {
        occupancyGrid.Reserve(center, radius);

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

                MapCellData cell = map[x, y];
                cell.isReservedForBuilding = true;
                map[x, y] = cell;
            }
        }
    }

    private void RenderPlaceables(List<ResolvedPlaceablePlacement> placements)
    {
        if (placements == null || placements.Count == 0)
        {
            return;
        }

        EnsurePlaceableHierarchy();

        for (int i = 0; i < placements.Count; i++)
        {
            ResolvedPlaceablePlacement placement = placements[i];
            Transform categoryParent = ResolveCategoryParent(placement.category);

            Vector3 worldPosition =
                terrainTilemap.GetCellCenterWorld(new Vector3Int(placement.cellPosition.x, placement.cellPosition.y, 0)) +
                placement.category.worldOffset;

            GameObject instance = runtimePrefabPool.Spawn(placement.prefab, worldPosition, Quaternion.identity, categoryParent);
            if (instance != null)
            {
                instance.name = $"{placement.category.GetDisplayName()}_{placement.prefab.name}";
            }
        }
    }

    private void ReleaseGeneratedPlaceables()
    {
        if (runtimePrefabPool == null)
        {
            return;
        }

        EnsurePlaceableHierarchy();
        runtimePrefabPool.ReleaseAll();
    }

    private void EnsurePlaceableHierarchy()
    {
        Transform parent = terrainTilemap.transform.parent != null
            ? terrainTilemap.transform.parent
            : terrainTilemap.transform;

        if (generatedPlaceablesParent == null)
        {
            Transform existing = parent.Find(DefaultPlaceableRootName);
            if (existing != null)
            {
                generatedPlaceablesParent = existing;
            }
            else
            {
                GameObject root = new GameObject(DefaultPlaceableRootName);
                root.transform.SetParent(parent, false);
                generatedPlaceablesParent = root.transform;
            }
        }

        if (runtimePoolRoot == null || runtimePoolRoot.parent != generatedPlaceablesParent)
        {
            Transform existingPool = generatedPlaceablesParent.Find(PoolRootName);
            if (existingPool != null)
            {
                runtimePoolRoot = existingPool;
            }
            else
            {
                GameObject pool = new GameObject(PoolRootName);
                pool.transform.SetParent(generatedPlaceablesParent, false);
                pool.SetActive(false);
                runtimePoolRoot = pool.transform;
            }
        }

        if (runtimePrefabPool == null)
        {
            runtimePrefabPool = new RuntimePrefabPool(generatedPlaceablesParent, runtimePoolRoot);
        }
        else
        {
            runtimePrefabPool.SetRoots(generatedPlaceablesParent, runtimePoolRoot);
        }
    }

    private Transform ResolveCategoryParent(PlaceableCategoryConfig category)
    {
        string categoryName = string.IsNullOrWhiteSpace(category.GetDisplayName())
            ? "Placeables"
            : category.GetDisplayName();

        Transform cachedParent;
        if (categoryParentCache.TryGetValue(categoryName, out cachedParent) &&
            cachedParent != null &&
            cachedParent.parent == generatedPlaceablesParent)
        {
            return cachedParent;
        }

        Transform existing = generatedPlaceablesParent.Find(categoryName);
        if (existing == null)
        {
            GameObject categoryRoot = new GameObject(categoryName);
            categoryRoot.transform.SetParent(generatedPlaceablesParent, false);
            existing = categoryRoot.transform;
        }

        categoryParentCache[categoryName] = existing;
        return existing;
    }

    private void LogGenerationSummary(MapCellData[,] map, List<ResolvedPlaceablePlacement> placements)
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

        Debug.Log(
            "Map generation summary:\n" +
            $"   seed: {seed}\n" +
            $"   heightOffset: {lastHeightOffset}\n" +
            $"   moistureOffset: {lastMoistureOffset}\n" +
            $"   water: {waterCount} ({waterCount * 100f / total:F1}%)\n" +
            $"   soil: {soilCount} ({soilCount * 100f / total:F1}%)\n" +
            $"   grass: {grassCount} ({grassCount * 100f / total:F1}%)\n" +
            $"   height range: {minHeight:F3} ~ {maxHeight:F3}\n" +
            $"   moisture range: {minMoisture:F3} ~ {maxMoisture:F3}\n" +
            $"   water clusters: {lastStats.waterClusterCount}, removed: {lastStats.removedWaterClusters}\n" +
            $"   soil clusters: {lastStats.soilClusterCount}, removed: {lastStats.removedSoilClusters}");

        if (placements == null || placements.Count == 0)
        {
            Debug.Log("   placeables: none");
            return;
        }

        Dictionary<string, int> countsByCategory = new Dictionary<string, int>();
        for (int i = 0; i < placements.Count; i++)
        {
            string categoryName = placements[i].category.GetDisplayName();
            if (!countsByCategory.ContainsKey(categoryName))
            {
                countsByCategory.Add(categoryName, 0);
            }

            countsByCategory[categoryName]++;
        }

        foreach (KeyValuePair<string, int> pair in countsByCategory)
        {
            Debug.Log($"   placeables: {pair.Key} x {pair.Value}");
        }
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

                    if (owner.applyEdgeFalloffToHeightNoise)
                    {
                        float edgeFactor = CalculateEdgeFactor(x, y);
                        heightNoise = Mathf.Lerp(0f, heightNoise, edgeFactor);
                    }

                    MapCellData cell = new MapCellData
                    {
                        heightNoise = heightNoise,
                        moistureNoise = moistureNoise,
                        terrainType = TerrainType.Grass,
                        isReservedForBuilding = false
                    };

                    map[x, y] = cell;
                }
            }

            owner.ResolveTerrainDistribution(map);
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
            CleanupSmallClusters(map, TerrainType.Water, owner.GetMinimumClusterSize(TerrainType.Water), ref stats);
            CleanupSmallClusters(map, TerrainType.Soil, owner.GetMinimumClusterSize(TerrainType.Soil), ref stats);
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

                            TerrainType terrain = owner.IsInside(neighborX, neighborY)
                                ? map[neighborX, neighborY].terrainType
                                : map[x, y].terrainType;

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
                    : map[x, y].terrainType;

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
                    : map[x, y].terrainType;

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

    private readonly struct PlaceableCandidate
    {
        public readonly Vector2Int position;
        public readonly float score;
        public readonly int clusterSize;

        public PlaceableCandidate(Vector2Int position, float score, int clusterSize)
        {
            this.position = position;
            this.score = score;
            this.clusterSize = clusterSize;
        }
    }

    private readonly struct ResolvedPlaceablePlacement
    {
        public readonly PlaceableCategoryConfig category;
        public readonly GameObject prefab;
        public readonly Vector2Int cellPosition;
        public readonly float score;
        public readonly int clusterSize;

        public ResolvedPlaceablePlacement(
            PlaceableCategoryConfig category,
            GameObject prefab,
            Vector2Int cellPosition,
            float score,
            int clusterSize)
        {
            this.category = category;
            this.prefab = prefab;
            this.cellPosition = cellPosition;
            this.score = score;
            this.clusterSize = clusterSize;
        }
    }

    private sealed class PlaceableOccupancyGrid
    {
        private readonly int width;
        private readonly int height;
        private readonly bool[,] occupied;

        public PlaceableOccupancyGrid(int width, int height)
        {
            this.width = width;
            this.height = height;
            occupied = new bool[width, height];
        }

        public bool IsOccupied(int x, int y)
        {
            return x < 0 || x >= width || y < 0 || y >= height || occupied[x, y];
        }

        public void Reserve(Vector2Int center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = center.x + dx;
                    int y = center.y + dy;
                    if (x < 0 || x >= width || y < 0 || y >= height)
                    {
                        continue;
                    }

                    occupied[x, y] = true;
                }
            }
        }
    }

    private readonly struct TerrainDistributionTargets
    {
        public readonly float grassRatio;
        public readonly float soilRatio;
        public readonly float waterRatio;

        public TerrainDistributionTargets(float grassRatio, float soilRatio, float waterRatio)
        {
            this.grassRatio = grassRatio;
            this.soilRatio = soilRatio;
            this.waterRatio = waterRatio;
        }
    }

    private readonly struct TerrainAssignmentCandidate
    {
        public readonly int x;
        public readonly int y;
        public readonly float score;
        public readonly int tieBreaker;

        public TerrainAssignmentCandidate(int x, int y, float score, int tieBreaker)
        {
            this.x = x;
            this.y = y;
            this.score = score;
            this.tieBreaker = tieBreaker;
        }
    }

    private sealed class TerrainAssignmentCandidateComparer : IComparer<TerrainAssignmentCandidate>
    {
        public static readonly TerrainAssignmentCandidateComparer Instance = new TerrainAssignmentCandidateComparer();

        public int Compare(TerrainAssignmentCandidate left, TerrainAssignmentCandidate right)
        {
            int scoreComparison = left.score.CompareTo(right.score);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            int tieComparison = left.tieBreaker.CompareTo(right.tieBreaker);
            if (tieComparison != 0)
            {
                return tieComparison;
            }

            int xComparison = left.x.CompareTo(right.x);
            if (xComparison != 0)
            {
                return xComparison;
            }

            return left.y.CompareTo(right.y);
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
