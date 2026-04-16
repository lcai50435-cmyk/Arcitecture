using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    private KeyCode attackKey = KeyCode.Mouse0;
    private DirectionTracker directionTracker;
    private Animator animator;

    [Header("远程设置")]
    public GameObject inkballPrefab;
    public Transform inkPoint;
    [Header("血条脚本")]
    public ValueTrans weaponTrans;
    [Header("墨水数量")]
    public float ink;

    protected override void Awake()
    {
        directionTracker = GetComponent<DirectionTracker>();
        animator = GetComponent<Animator>();

        // 执行父类初始化
        base.Awake();

        // 初始化墨水数量
        weaponTrans.SetMaxValue(ink);
    }

    private void Update()
    {
        if (Input.GetKeyDown(attackKey) && !isAttacking)
        {
            TriggerAttack();
        }
    }

    public override void TriggerAttack()
    {
        if (ink <= 0) return;

        //墨水数量减少
        ink -= 5;
        weaponTrans.SetValue(ink);

        // 动画、朝向、禁止移动
        base.TriggerAttack(); 

        // 获取玩家最后面朝方向
        Vector2 lastDir = directionTracker.LastDirection;

        // 生成火球，并让攻击朝向最后方向
        if (inkballPrefab != null && inkPoint != null)
        {
            GameObject inkball = Instantiate(inkballPrefab, inkPoint.position, Quaternion.identity);

            // 让攻击朝向最后方向
            inkball.transform.right = lastDir;
        }
    }
}