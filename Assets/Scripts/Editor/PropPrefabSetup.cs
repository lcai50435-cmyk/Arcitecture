using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PropPrefabSetup : MonoBehaviour
{
    private static readonly Dictionary<string, ArchitecturalType> nameToTypeMap = new Dictionary<string, ArchitecturalType>
    {
        { "MortiseAndTenonJoint", ArchitecturalType.MortiseAndTenonJoint },
        { "Brackets", ArchitecturalType.Brackets },
        { "BeamFrame", ArchitecturalType.BeamFrame },
        { "GroundMass", ArchitecturalType.GroundMass },
        { "RammedEarth", ArchitecturalType.TampedEarth },
        { "Tile", ArchitecturalType.Tile }
    };

    private static readonly Dictionary<string, string> typeDescriptions = new Dictionary<string, string>
    {
        { "MortiseAndTenonJoint", "这是榫卯结构，通过凹凸咬合连接木材，无需钉子。它让建筑既稳固又灵活，是中国古建筑最重要的技艺之一。" },
        { "Brackets", "斗拱位于柱与屋顶之间，用来承托重量并分散压力。这种结构还能缓冲震动，使建筑更加稳固。" },
        { "BeamFrame", "梁架是建筑的骨架，由梁与柱共同构成，支撑起整个屋顶结构。" },
        { "GroundMass", "石基位于建筑底部，可以防潮、防腐，让建筑更加耐久。" },
        { "RammedEarth", "夯土是将土层反复压实形成地基的方法，简单却非常坚固，广泛用于古代建筑。" },
        { "Tile", "瓦片覆盖在屋顶上，用来防水和保护内部结构，是最常见的屋面材料。" }
    };

#if UNITY_EDITOR
    [MenuItem("Tools/Setup Prop Prefabs for Pickup")]
    public static void SetupAllPropPrefabs()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/File/Prefab/PropPrefab" });
        
        foreach (string guid in prefabPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                SetupPrefab(prefab);
                EditorUtility.SetDirty(prefab);
                Debug.Log($"已配置预制体: {path}");
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("所有建筑物品预制体配置完成！");
    }

    private static void SetupPrefab(GameObject prefab)
    {
        string prefabName = prefab.name;
        
        // 移除旧的交互组件
        CrystalInteractHandler existingHandler = prefab.GetComponent<CrystalInteractHandler>();
        if (existingHandler != null)
        {
            Component.DestroyImmediate(existingHandler, true);
        }

        // 添加CrystalInteractHandler组件
        CrystalInteractHandler handler = prefab.AddComponent<CrystalInteractHandler>();

        // 设置类型
        if (nameToTypeMap.TryGetValue(prefabName, out ArchitecturalType type))
        {
            handler.type = type;
        }
        else
        {
            Debug.LogWarning($"未找到 {prefabName} 的类型映射");
            handler.type = ArchitecturalType.MortiseAndTenonJoint;
        }

        // 设置文本描述
        if (typeDescriptions.TryGetValue(prefabName, out string description))
        {
            handler.textDescription = description;
        }

        // 设置默认属性
        handler.resourceCategory = ArchitecturalResourceCategory.CommonStructure;
        handler.isUnlockMaterial = false;
        handler.buildProgressPercent = 0;
        handler.expValue = 0;
        handler.persistCollectedAcrossSceneLoads = false;
        handler.startClosedAsLootBag = false;

        // 移除旧的碰撞器
        CircleCollider2D existingCollider = prefab.GetComponent<CircleCollider2D>();
        if (existingCollider != null)
        {
            Component.DestroyImmediate(existingCollider, true);
        }

        // 添加圆形碰撞器
        CircleCollider2D collider = prefab.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.2f;

        // 设置SpriteRenderer的sortingOrder
        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 4;
        }
    }
#endif
}
