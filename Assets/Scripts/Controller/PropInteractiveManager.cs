using UnityEngine;
using System.Collections.Generic;

public class PropInteractiveManager : MonoBehaviour
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
        { "MortiseAndTenonJoint", "这是榫卯结构，通过凹凸咬合连接木材，无需钉子。它让建筑既稳固又灵活。你握住它时，能感觉到力量的传递变得更加顺畅，出手也更灵活了一些。" },
        { "Brackets", "斗拱位于柱与屋顶之间，用来承托重量并分散压力。这种结构还能缓冲震动，使建筑更加稳固。这种稳固感，似乎也在保护你，让你更能承受冲击。" },
        { "BeamFrame", "梁架是建筑的骨架，由梁与柱共同构成，支撑起整个屋顶结构。它让整体运转更加高效，就像你的动作与攻击，也变得更加流畅有力。" },
        { "GroundMass", "石基位于建筑底部，可以防潮、防腐，让建筑更加耐久。但也正因为它厚重稳固，你会感觉行动略微沉了一些，不过更不容易被动摇。" },
        { "RammedEarth", "夯土是将土层反复压实形成地基的方法，简单却非常坚固。这种稳定与延展，让你的攻击能够传得更远，也更加迅速。" },
        { "Tile", "瓦片覆盖在屋顶上，用来防水和保护内部结构，是最常见的屋面材料。它轻巧而实用，让你的消耗变得更少，只是力量也显得更加分散。" }
    };

    public static PropInteractiveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        SetupAllPropsInScene();
    }

    [ContextMenu("Setup All Props in Scene")]
    public void SetupAllPropsInScene()
    {
        SpriteRenderer[] allRenderers = FindObjectsOfType<SpriteRenderer>();
        
        foreach (SpriteRenderer renderer in allRenderers)
        {
            GameObject obj = renderer.gameObject;
            string objName = obj.name;
            
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

    public static void SetupProp(GameObject obj)
    {
        string objName = obj.name;
        if (objName.EndsWith("(Clone)"))
        {
            objName = objName.Substring(0, objName.Length - 7);
        }
        
        if (nameToTypeMap.ContainsKey(objName))
        {
            if (Instance != null)
            {
                Instance.SetupPropObject(obj, objName);
            }
            else
            {
                // 如果Instance不存在，直接设置
                SetupPropObjectDirect(obj, objName);
            }
        }
    }

    private void SetupPropObject(GameObject obj, string propName)
    {
        SetupPropObjectDirect(obj, propName);
    }

    private static void SetupPropObjectDirect(GameObject obj, string propName)
    {
        CrystalInteractHandler handler = obj.GetComponent<CrystalInteractHandler>();
        if (handler == null)
        {
            handler = obj.AddComponent<CrystalInteractHandler>();
            Debug.Log($"[PropSetup] 为 {propName} 新增 CrystalInteractHandler 组件");
        }

        if (nameToTypeMap.TryGetValue(propName, out ArchitecturalType type))
        {
            handler.type = type;
            Debug.Log($"[PropSetup] 设置 {propName} 的 type = {type}");
        }
        else
        {
            Debug.LogWarning($"[PropSetup] 未找到 {propName} 的类型映射");
        }

        if (typeDescriptions.TryGetValue(propName, out string description))
        {
            handler.textDescription = description;
        }

        handler.resourceCategory = ArchitecturalResourceCategory.CommonStructure;
        handler.isUnlockMaterial = false;
        handler.buildProgressPercent = 0;
        handler.expValue = 0;
        handler.persistCollectedAcrossSceneLoads = false;
        handler.startClosedAsLootBag = false;

        CircleCollider2D collider = obj.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = obj.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = 0.2f;

        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 4;
        }

        Debug.Log($"[PropSetup] 已配置物品: {propName}, type={handler.type}, desc={handler.textDescription}");
    }
}
