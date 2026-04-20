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
        // 修复重复获取组件的问题
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
            Debug.Log($"自动为格子{slotIndex}添加了Image组件");
        }

        backpack = BackpackMananger.Instance;
        backpackUI = FindObjectOfType<BackpackUI>();

        if (backpack == null)
        {
            Debug.LogError($"BackpackSlot {slotIndex}: 未找到BackpackMananger实例！");
        }
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

        // 关键修改1：接收可空类型
        ArchitecturalCrystal? item = backpack.GetItem(slotIndex);
        // 关键修改2：判空改为 HasValue
        if (!item.HasValue)
        {
            Debug.Log($"格子{slotIndex}没有物品，无法丢弃");
            return;
        }

        if (slotImage == null || slotImage.sprite == null || !slotImage.enabled)
        {
            Debug.Log("格子无显示图片");
            return;
        }

        Debug.Log($"开始长按格子{slotIndex}，物品：{item.Value.type}");
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

        // 关键修改3：接收可空类型
        ArchitecturalCrystal? item = backpack.GetItem(slotIndex);
        // 关键修改4：判空改为 HasValue
        if (!item.HasValue) return;

        // 关键修改5：通过 Value 获取结构体实际值
        var crystal = item.Value;

        // 获取玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("未找到Player对象！");
            return;
        }

        // 生成掉落物
        GameObject dropObj = new GameObject($"Drop_{crystal.type}");

        // 物品生成位置：玩家位置 + 朝向偏移
        float faceDir = player.transform.localScale.x > 0 ? 1 : -1;
        dropObj.transform.position = new Vector2(
            player.transform.position.x + faceDir * 0.2f, // X轴偏移
            player.transform.position.y // Y轴与玩家齐平
        );

        // 添加SpriteRenderer
        SpriteRenderer ren = dropObj.AddComponent<SpriteRenderer>();
        ren.sprite = crystal.icon; // 使用物品图标
        ren.sortingOrder = 0; // 设置图层

        // 设置大小
        Transform tran = dropObj.transform;
        tran.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // 添加碰撞器
        CircleCollider2D collider = dropObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        // 添加交互组件
        CrystalInteractHandler script = dropObj.AddComponent<CrystalInteractHandler>();
        script.type = crystal.type;
        script.expValue = crystal.expValue;
        script.icon = crystal.icon;
        script.backIcon = crystal.backIcon;
        script.textDescription = crystal.textDescription;
        script.bonusType = crystal.bonusType;
        script.bonusValue = crystal.bonusValue;
        // 修复原代码笔误：subBonusType 被覆盖的问题
        script.subBonusType = crystal.subBonusType;
        script.subBonusValue = crystal.subBonusValue;

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

        Debug.Log($"格子{slotIndex}的物品{crystal.type}已丢弃");
    }
}