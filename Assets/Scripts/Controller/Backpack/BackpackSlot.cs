using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackpackSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("格子编号 0~5")]
    public int slotIndex;

    [Header("长按几秒丢弃")]
    public float needHoldTime = 1f;

    private BackpackMananger backpack;
    private Image slotImage; // 改为Image类型
    private BackpackUI backpackUI;

    private bool isHolding;
    private float holdTimer;

    void Start()
    {
        slotImage = GetComponent<Image>(); // 获取Image组件
        backpack = BackpackMananger.Instance;
        backpackUI = FindObjectOfType<BackpackUI>();

        if (slotImage == null)
        {
            Debug.LogError($"BackpackSlot {slotIndex}: 缺少Image组件！");
        }

        // 如果没有Image组件，自动添加
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
            Debug.Log($"自动为格子{slotIndex}添加了Image组件");
        }

        backpack = BackpackMananger.Instance;
        backpackUI = FindObjectOfType<BackpackUI>();
    }

    
    void Update()
    {
        if (!isHolding) return;

        holdTimer += Time.deltaTime;

        // 长按进度反馈
        if (holdTimer >= needHoldTime)
        {
            DropSingleItem();
            StopHold();
        }
    }

    // UI事件：按下
    public void OnPointerDown(PointerEventData eventData)
    {
        // 只响应右键
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            StartSingleHold();
        }
    }

    // UI事件：抬起
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            StopHold();
        }
    }

    void StartSingleHold()
    {
        if (backpack == null)
        {
            Debug.LogError("BackpackManager未找到！");
            return;
        }

        ArchitecturalCrystal item = backpack.GetItem(slotIndex);
        if (item == null)
        {
            Debug.Log($"格子{slotIndex}没有物品，无法丢弃");
            return;
        }

        if (slotImage == null || slotImage.sprite == null || !slotImage.enabled)
        {
            Debug.Log("格子无显示图片");
            return;
        }

        Debug.Log($"开始长按格子{slotIndex}，物品：{item.type}");
        isHolding = true;
        holdTimer = 0;
    }

    void StopHold()
    {
        isHolding = false;
        holdTimer = 0;
    }

    void DropSingleItem()
    {
        if (backpack == null) return;

        ArchitecturalCrystal item = backpack.GetItem(slotIndex);
        if (item == null) return;

        // 获取玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("未找到Player对象！");
            return;
        }

        // 生成掉落物（场景中的2D物体，使用SpriteRenderer）
        GameObject dropObj = new GameObject($"Drop_{item.type}");

        // 物品生成位置：玩家位置 + 朝向偏移（避免卡在玩家身上）
        float faceDir = player.transform.localScale.x > 0 ? 1 : -1;
        dropObj.transform.position = new Vector2(
            player.transform.position.x + faceDir * 0.2f, // X轴偏移
            player.transform.position.y // Y轴与玩家齐平
        );

        // 添加SpriteRenderer（掉落物在场景中，不是UI）
        SpriteRenderer ren = dropObj.AddComponent<SpriteRenderer>();
        ren.sprite = item.icon; // 使用物品图标
        ren.sortingOrder = 0; // 设置图层

        // 添加碰撞器
        CircleCollider2D collider = dropObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        // collider.radius = 0.6f; // 适配2D物品大小

        // 添加交互组件
        CrystalInteractHandler script = dropObj.AddComponent<CrystalInteractHandler>();
        script.type = item.type;
        script.expValue = item.expValue;
        script.icon = item.icon;
        script.backIcon = item.backIcon;
        script.textDescription = item.textDescription;
        script.bonusType = item.bonusType;
        script.bonusValue = item.bonusValue;

        // 从背包移除物品
        backpack.RemoveItem(slotIndex);

        // 刷新UI
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
        else
        {
            Debug.LogError("BackpackUI未找到！");
        }

        Debug.Log($"格子{slotIndex}的物品{item.type}已丢弃");
    }
}