using UnityEngine;

public class BackpackSlot : MonoBehaviour
{
    [Header("格子编号 0~5")]
    public int slotIndex;

    [Header("长按几秒丢弃")]
    public float needHoldTime = 3f;

    private BackpackMananger _backpack;
    private SpriteRenderer _sr;

    private bool _isHolding;
    private float _holdTimer;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _backpack = BackpackMananger.Instance;
    }

    void Update()
    {
        // === 右键按下：只对当前格子生效，开始单格长按 ===
        if (Input.GetMouseButtonDown(1) && IsMouseOverCollider())
        {
            StartSingleHold();
        }

        // 右键抬起取消
        if (Input.GetMouseButtonUp(1))
        {
            StopHold();
        }

        if (!_isHolding) return;

        _holdTimer += Time.deltaTime;
        if (_holdTimer >= needHoldTime)
        {
            // 只丢当前这个格子物品
            DropSingleItem();
            StopHold();
        }
    }

    // 开始长按当前格子
    void StartSingleHold()
    {
        if (_backpack == null || _backpack.GetItem(slotIndex) == null)
        {
            Debug.Log("当前格子没有物品");
            return;
        }
        if (_sr == null || _sr.sprite == null || !_sr.enabled)
        {
            Debug.Log("格子无显示图片");
            return;
        }

        Debug.Log("右键长按当前单个格子");
        _isHolding = true;
        _holdTimer = 0;
    }

    void StopHold()
    {
        _isHolding = false;
        _holdTimer = 0;
    }

    // 只丢弃【当前slotIndex】这一个物品
    void DropSingleItem()
    {
        if (_backpack == null) return;

        ArchitecturalCrystal item = _backpack.GetItem(slotIndex);
        if (item == null) return;

        // 生成掉落物
        GameObject dropObj = new GameObject(item.type.ToString());
        dropObj.transform.position = GameObject.FindWithTag("Player").transform.position;

        // 对物品添加图画组件
        SpriteRenderer ren = dropObj.AddComponent<SpriteRenderer>();
        ren.sprite = item.icon;

        // 对物品添加碰撞器组件
        CircleCollider2D c = dropObj.AddComponent<CircleCollider2D>();
        c.isTrigger = true;

        // 对物品添加可交互组件
        CrystalInteractHandler script = dropObj.AddComponent<CrystalInteractHandler>();

        // 对物品赋值
        script.type = item.type;
        script.expValue = item.expValue;
        script.icon = item.icon;
        script.backIcon = item.backIcon;
        script.textDescription = item.textDescription;

        // 只移除当前这一格
        _backpack.RemoveItem(slotIndex);
        FindObjectOfType<BackpackUI>().RefreshUI();

        Debug.Log("当前单个物品丢弃成功");
    }

    // 判断鼠标是否悬停在当前格子碰撞体上
    private bool IsMouseOverCollider()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity))
        {
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            return hit.collider != null && hit.collider.gameObject == gameObject;
        }
        return false;
    }
}