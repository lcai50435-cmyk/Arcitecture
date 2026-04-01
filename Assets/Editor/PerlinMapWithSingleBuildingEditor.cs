using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(PerlinMapWithSingleBuilding))]
public class PerlinMapWithSingleBuildingEditor : Editor
{
    private const string GrassPath = "Assets/File/MapResources/Block/GrassBlock.png";
    private const string SoilPath = "Assets/File/MapResources/Block/SoilBlock.png";
    private const string WaterPath = "Assets/File/MapResources/Block/WaterBlock.png";

    private const string Trilateral1Path = "Assets/File/MapResources/TrilateralGrassBlock/TrilateralGrassBlock_1.png";
    private const string Trilateral2Path = "Assets/File/MapResources/TrilateralGrassBlock/TrilateralGrassBlock_2.png";
    private const string Trilateral3Path = "Assets/File/MapResources/TrilateralGrassBlock/TrilateralGrassBlock_3.png";
    private const string Trilateral4Path = "Assets/File/MapResources/TrilateralGrassBlock/TrilateralGrassBlock_4.png";
    private const string Quadrilateral1Path = "Assets/File/MapResources/QuadrilateralGrassBlock/QuadrilateralGrassBlock_1.png";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        PerlinMapWithSingleBuilding map = (PerlinMapWithSingleBuilding)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PerlinMap Editor Helpers", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "四向 mask 约定: N/E/S/W = 1/2/4/8，对角 diagonalMask 约定: NE/SE/SW/NW = 1/2/4/8。建议先用稳定资源跑通，再补单边和双边细节。",
            MessageType.Info);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (GUILayout.Button("填充基础地形示例资源"))
            {
                ApplyBaseTerrainExample(map);
            }

            if (GUILayout.Button("初始化推荐过渡骨架"))
            {
                InitializeRecommendedSkeleton(map);
            }

            if (GUILayout.Button("初始化完整过渡骨架"))
            {
                InitializeFullSkeleton(map);
            }

            if (GUILayout.Button("填充 Phase 1 稳定草土示例"))
            {
                ApplyPhaseOneSoilGrassExample(map);
            }

            if (GUILayout.Button("清空全部过渡表"))
            {
                ClearTransitionLookups(map);
            }
        }

        EditorGUILayout.HelpBox(
            "参考文档: Docs/PerlinMapTransitionAssetNotes.md, Docs/PerlinMapInspectorExample.md, Docs/PerlinMapSetupTODO.md",
            MessageType.None);
    }

    private static void ApplyBaseTerrainExample(PerlinMapWithSingleBuilding map)
    {
        Undo.RecordObject(map, "Apply Base Terrain Example");
        EnsureCatalog(map);

        AssignSpriteIfEmpty(map.terrainTiles.grass, GrassPath);
        AssignSpriteIfEmpty(map.terrainTiles.soil, SoilPath);
        AssignSpriteIfEmpty(map.terrainTiles.water, WaterPath);

        MarkDirty(map);
        Debug.Log("✅ 已填充基础地形示例资源。");
    }

    private static void InitializeRecommendedSkeleton(PerlinMapWithSingleBuilding map)
    {
        Undo.RecordObject(map, "Initialize Recommended Transition Skeleton");
        EnsureCatalog(map);

        map.terrainTiles.soilGrassTransitions.entries = new List<MaskedTileVariants>
        {
            CreateEntry("Phase 1 - 左侧缺口", 7),
            CreateEntry("Phase 1 - 底部缺口", 11),
            CreateEntry("Phase 1 - 右侧缺口", 13),
            CreateEntry("Phase 1 - 顶部缺口", 14),
            CreateEntry("Phase 1 - 四边包围", 15)
        };

        map.terrainTiles.waterEdgeTransitions.entries = new List<MaskedTileVariants>
        {
            CreateEntry("手工确认 - 顶部临水直岸", 1),
            CreateEntry("手工确认 - 右侧临水直岸", 2),
            CreateEntry("手工确认 - 底部临水直岸", 4),
            CreateEntry("手工确认 - 左侧临水直岸", 8)
        };

        map.terrainTiles.mixedTransitions.entries = new List<MaskedTileVariants>
        {
            CreateEntry("手工确认 - 顶右转角", 3),
            CreateEntry("手工确认 - 右下转角", 6),
            CreateEntry("手工确认 - 左下转角", 12),
            CreateEntry("手工确认 - 左上转角", 9)
        };

        MarkDirty(map);
        Debug.Log("✅ 已初始化推荐过渡骨架。");
    }

    private static void InitializeFullSkeleton(PerlinMapWithSingleBuilding map)
    {
        Undo.RecordObject(map, "Initialize Full Transition Skeleton");
        EnsureCatalog(map);

        map.terrainTiles.soilGrassTransitions.entries = CreateFullEntryList("草土");
        map.terrainTiles.waterEdgeTransitions.entries = CreateFullEntryList("水边");
        map.terrainTiles.mixedTransitions.entries = CreateFullEntryList("混合");

        MarkDirty(map);
        Debug.Log("✅ 已初始化完整过渡骨架。");
    }

    private static void ApplyPhaseOneSoilGrassExample(PerlinMapWithSingleBuilding map)
    {
        Undo.RecordObject(map, "Apply Phase One Soil Grass Example");
        EnsureCatalog(map);

        AssignSpriteIfEmpty(map.terrainTiles.grass, GrassPath);
        AssignSpriteIfEmpty(map.terrainTiles.soil, SoilPath);
        AssignSpriteIfEmpty(map.terrainTiles.water, WaterPath);

        if (map.terrainTiles.soilGrassTransitions.entries == null || map.terrainTiles.soilGrassTransitions.entries.Count == 0)
        {
            InitializeRecommendedSkeleton(map);
        }

        SetSingleVariant(map.terrainTiles.soilGrassTransitions, 7, "Phase 1 - 左侧缺口", Trilateral4Path);
        SetSingleVariant(map.terrainTiles.soilGrassTransitions, 11, "Phase 1 - 底部缺口", Trilateral2Path);
        SetSingleVariant(map.terrainTiles.soilGrassTransitions, 13, "Phase 1 - 右侧缺口", Trilateral1Path);
        SetSingleVariant(map.terrainTiles.soilGrassTransitions, 14, "Phase 1 - 顶部缺口", Trilateral3Path);
        SetSingleVariant(map.terrainTiles.soilGrassTransitions, 15, "Phase 1 - 四边包围", Quadrilateral1Path);

        MarkDirty(map);
        Debug.Log("✅ 已填充 Phase 1 稳定草土示例。");
    }

    private static void ClearTransitionLookups(PerlinMapWithSingleBuilding map)
    {
        Undo.RecordObject(map, "Clear Transition Lookups");
        EnsureCatalog(map);

        map.terrainTiles.soilGrassTransitions.entries = new List<MaskedTileVariants>();
        map.terrainTiles.waterEdgeTransitions.entries = new List<MaskedTileVariants>();
        map.terrainTiles.mixedTransitions.entries = new List<MaskedTileVariants>();

        MarkDirty(map);
        Debug.Log("🧹 已清空全部过渡表。");
    }

    private static List<MaskedTileVariants> CreateFullEntryList(string prefix)
    {
        List<MaskedTileVariants> entries = new List<MaskedTileVariants>();
        for (int mask = 1; mask <= 15; mask++)
        {
            entries.Add(CreateEntry($"{prefix} - {DescribeMask(mask)}", mask));
        }

        return entries;
    }

    private static MaskedTileVariants CreateEntry(string description, int mask, int diagonalMask = -1)
    {
        return new MaskedTileVariants
        {
            description = description,
            mask = mask,
            diagonalMask = diagonalMask,
            variants = new List<TileAssetReference>()
        };
    }

    private static void SetSingleVariant(TransitionTileLookup lookup, int mask, string description, string spritePath)
    {
        MaskedTileVariants entry = FindOrCreateEntry(lookup, mask, description);
        entry.description = description;
        entry.diagonalMask = -1;
        entry.variants = new List<TileAssetReference>();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"⚠️ 未找到示例资源: {spritePath}");
            return;
        }

        entry.variants.Add(new TileAssetReference { sprite = sprite });
    }

    private static MaskedTileVariants FindOrCreateEntry(TransitionTileLookup lookup, int mask, string description)
    {
        if (lookup.entries == null)
        {
            lookup.entries = new List<MaskedTileVariants>();
        }

        for (int i = 0; i < lookup.entries.Count; i++)
        {
            if (lookup.entries[i] != null && lookup.entries[i].mask == mask)
            {
                return lookup.entries[i];
            }
        }

        MaskedTileVariants created = CreateEntry(description, mask);
        lookup.entries.Add(created);
        return created;
    }

    private static void AssignSpriteIfEmpty(TileAssetReference reference, string path)
    {
        if (reference == null || !reference.IsEmpty)
        {
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            reference.sprite = sprite;
        }
    }

    private static void EnsureCatalog(PerlinMapWithSingleBuilding map)
    {
        if (map.terrainTiles == null)
        {
            map.terrainTiles = new TerrainTileCatalog();
        }

        if (map.terrainTiles.grass == null)
        {
            map.terrainTiles.grass = new TileAssetReference();
        }

        if (map.terrainTiles.soil == null)
        {
            map.terrainTiles.soil = new TileAssetReference();
        }

        if (map.terrainTiles.water == null)
        {
            map.terrainTiles.water = new TileAssetReference();
        }

        if (map.terrainTiles.soilGrassTransitions == null)
        {
            map.terrainTiles.soilGrassTransitions = new TransitionTileLookup();
        }

        if (map.terrainTiles.waterEdgeTransitions == null)
        {
            map.terrainTiles.waterEdgeTransitions = new TransitionTileLookup();
        }

        if (map.terrainTiles.mixedTransitions == null)
        {
            map.terrainTiles.mixedTransitions = new TransitionTileLookup();
        }
    }

    private static string DescribeMask(int mask)
    {
        List<string> parts = new List<string>();
        if ((mask & 1) != 0)
        {
            parts.Add("N");
        }

        if ((mask & 2) != 0)
        {
            parts.Add("E");
        }

        if ((mask & 4) != 0)
        {
            parts.Add("S");
        }

        if ((mask & 8) != 0)
        {
            parts.Add("W");
        }

        return string.Join("+", parts);
    }

    private static void MarkDirty(PerlinMapWithSingleBuilding map)
    {
        EditorUtility.SetDirty(map);
        if (map != null && map.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        }
    }
}
