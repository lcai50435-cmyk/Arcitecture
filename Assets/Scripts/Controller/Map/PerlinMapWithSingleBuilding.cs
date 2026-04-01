using UnityEngine;
using UnityEngine.Tilemaps;

public class PerlinMapWithSingleBuilding : MonoBehaviour
{
    [Header("Tilemap 引用")]
    public Tilemap terrainTilemap;
    public Tilemap buildingTilemap;

    [Header("瓦片资源")]
    public TileBase grassTile;
    public TileBase dirtTile;
    public TileBase buildingTile;

    [Header("地图尺寸")]
    public int width = 80;
    public int height = 80;

    [Header("柏林噪声参数")]
    public float scale = 15f;           // 改小让地形变化更明显
    public int octaves = 3;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;

    [Header("地形阈值")]
    [Range(0, 1)]
    public float dirtThreshold = 0.4f;   // 降低阈值，让泥土更容易出现

    [Header("建筑生成位置")]
    public bool buildingOnDirt = true;
    public Vector2Int fixedPosition;
    public bool randomPosition = true;

    [Header("调试")]
    public bool showDebugInfo = true;     // 显示调试信息

    void Start()
    {
        if (terrainTilemap == null)
        {
            Debug.LogError("❌ 请将 Terrain Tilemap 拖拽到脚本的 terrainTilemap 字段！");
            return;
        }

        if (grassTile == null || dirtTile == null)
        {
            Debug.LogError("❌ 请拖入草地和泥土瓦片！");
            return;
        }

        GenerateMap();
    }

    void GenerateMap()
    {
        if (clearBeforeGenerate)
        {
            terrainTilemap.ClearAllTiles();
            if (buildingTilemap != null)
                buildingTilemap.ClearAllTiles();
        }

        if (offset == Vector2.zero)
            offset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));

        Debug.Log("开始生成地形...");

        // 统计各种地形的数量
        int grassCount = 0;
        int dirtCount = 0;
        float minNoise = 1f;
        float maxNoise = 0f;

        // 生成所有地形
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = GetPerlinNoiseValue(x, y);

                // 记录最大最小值
                if (noiseValue < minNoise) minNoise = noiseValue;
                if (noiseValue > maxNoise) maxNoise = noiseValue;

                // 根据噪声值放置地形
                TileBase terrainTile;
                if (noiseValue > dirtThreshold)
                {
                    terrainTile = dirtTile;
                    dirtCount++;
                }
                else
                {
                    terrainTile = grassTile;
                    grassCount++;
                }

                terrainTilemap.SetTile(new Vector3Int(x, y, 0), terrainTile);
            }
        }

        // 输出调试信息
        if (showDebugInfo)
        {
            Debug.Log($"📊 地形统计:");
            Debug.Log($"   草地: {grassCount} 个 ({grassCount * 100f / (width * height):F1}%)");
            Debug.Log($"   泥土: {dirtCount} 个 ({dirtCount * 100f / (width * height):F1}%)");
            Debug.Log($"   噪声范围: {minNoise:F3} ~ {maxNoise:F3}");
            Debug.Log($"   阈值: {dirtThreshold}");

            if (dirtCount == 0)
            {
                Debug.LogWarning("⚠️ 没有生成任何泥土！请尝试:");
                Debug.LogWarning("   1. 降低 dirtThreshold (比如 0.3)");
                Debug.LogWarning("   2. 降低 scale (比如 10)");
                Debug.LogWarning("   3. 增加 octaves");
            }
        }

        // 放置建筑
        PlaceBuilding();

        Debug.Log("✅ 地图生成完成！");
    }

    void PlaceBuilding()
    {
        if (buildingTilemap == null || buildingTile == null)
        {
            Debug.LogWarning("没有建筑层或建筑瓦片，跳过建筑生成");
            return;
        }

        Vector3Int buildingPosition = Vector3Int.zero;
        bool buildingPlaced = false;

        // 尝试固定位置
        if (fixedPosition != Vector2Int.zero)
        {
            buildingPosition = new Vector3Int(fixedPosition.x, fixedPosition.y, 0);
            if (IsValidBuildingPosition(buildingPosition))
            {
                buildingPlaced = true;
                Debug.Log($"使用固定位置: ({buildingPosition.x}, {buildingPosition.y})");
            }
            else
            {
                Debug.LogWarning($"固定位置 ({buildingPosition.x}, {buildingPosition.y}) 无效（该位置不是泥土）");
            }
        }

        // 随机寻找位置
        if (!buildingPlaced && randomPosition)
        {
            buildingPosition = FindRandomBuildingPosition();
            if (buildingPosition != Vector3Int.zero)
            {
                buildingPlaced = true;
                Debug.Log($"随机找到位置: ({buildingPosition.x}, {buildingPosition.y})");
            }
        }

        // 如果还是没找到，放在地图中心（并强制铺上泥土）
        if (!buildingPlaced)
        {
            buildingPosition = new Vector3Int(width / 2, height / 2, 0);
            Debug.LogWarning($"未找到合适的泥土位置，将建筑放在地图中心 ({buildingPosition.x}, {buildingPosition.y})，并强制铺上泥土");

            // 强制把中心变成泥土
            terrainTilemap.SetTile(buildingPosition, dirtTile);
        }

        // 放置建筑
        buildingTilemap.SetTile(buildingPosition, buildingTile);

        // 可选：在建筑周围铺一圈泥土
        SurroundWithDirt(buildingPosition);
    }

    bool IsValidBuildingPosition(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return false;

        if (buildingOnDirt)
        {
            TileBase tile = terrainTilemap.GetTile(pos);
            return tile == dirtTile;
        }

        return true;
    }

    Vector3Int FindRandomBuildingPosition()
    {
        int maxAttempts = 2000;
        int attempts = 0;

        // 先收集所有泥土位置
        System.Collections.Generic.List<Vector3Int> dirtPositions = new System.Collections.Generic.List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileBase tile = terrainTilemap.GetTile(new Vector3Int(x, y, 0));
                if (tile == dirtTile)
                {
                    dirtPositions.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"找到 {dirtPositions.Count} 个泥土位置");
        }

        if (dirtPositions.Count > 0)
        {
            // 随机选择一个泥土位置
            int randomIndex = Random.Range(0, dirtPositions.Count);
            return dirtPositions[randomIndex];
        }

        return Vector3Int.zero;
    }

    void SurroundWithDirt(Vector3Int center)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);
                if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
                {
                    // 建筑周围铺上泥土，让建筑更显眼
                    terrainTilemap.SetTile(pos, dirtTile);
                }
            }
        }
    }

    float GetPerlinNoiseValue(int x, int y)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseValue = 0f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + offset.x) / scale * frequency;
            float sampleY = (y + offset.y) / scale * frequency;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            noiseValue += perlinValue * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseValue / maxAmplitude;
    }

    public bool clearBeforeGenerate = true;

    public void RegenerateMap()
    {
        offset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RegenerateMap();
        }
    }
}               