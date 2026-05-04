using UnityEngine;
using System.Collections.Generic;

public class PropInteractiveSetup : MonoBehaviour
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

    private void Awake()
    {
        SetupAllPropsInScene();
    }

    [ContextMenu("Setup All Props in Scene")]
    public void SetupAllPropsInScene()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name;
            
            // 移除后缀 (Clone)
            if (objName.EndsWith("(Clone)"))
            {
                objName = objName.Substring(0, objName.Length - 7);
            }
            
            if (nameToTypeMap.ContainsKey(objName))
            {
                SetupPropObject(obj, objName);
            }
        }
        
        Debug.Log("场景中所有建筑物品已配置完成！");
    }

    private void SetupPropObject(GameObject obj, string propName)
    {
        // 检查是否已经有CrystalInteractHandler
        CrystalInteractHandler existingHandler = obj.GetComponent<CrystalInteractHandler>();
        if (existingHandler != null)
        {
            // 如果已经有组件，更新配置
            UpdateHandlerConfig(existingHandler, propName);
            return;
        }

        // 添加CrystalInteractHandler组件
        CrystalInteractHandler handler = obj.AddComponent<CrystalInteractHandler>();
        
        // 设置配置
        UpdateHandlerConfig(handler, propName);

        // 添加圆形碰撞器
        CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = 0.2f;

        // 设置SpriteRenderer的sortingOrder
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 4;
        }

        Debug.Log($"已配置物品: {propName}");
    }

    private void UpdateHandlerConfig(CrystalInteractHandler handler, string propName)
    {
        // 设置类型
        if (nameToTypeMap.TryGetValue(propName, out ArchitecturalType type))
        {
            handler.type = type;
        }

        // 设置文本描述
        if (typeDescriptions.TryGetValue(propName, out string description))
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
    }

    public static void SetupSingleProp(GameObject obj)
    {
        string objName = obj.name;
        if (objName.EndsWith("(Clone)"))
        {
            objName = objName.Substring(0, objName.Length - 7);
        }
        
        if (nameToTypeMap.ContainsKey(objName))
        {
            PropInteractiveSetup setup = new PropInteractiveSetup();
            setup.SetupPropObject(obj, objName);
        }
    }
}
