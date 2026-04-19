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
    public float maxInk = 100f;

    protected override void Awake()
    {
        directionTracker = GetComponent<DirectionTracker>();
        animator = GetComponent<Animator>();

        base.Awake();

        if (weaponTrans != null)
        {
            weaponTrans.SetMaxValue(maxInk);
            weaponTrans.SetValue(ink);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            // 只要有操作类UI开着，就禁止攻击
            if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
            {
                return;
            }

            if (!isAttacking)
            {
                TriggerAttack();
            }
        }
    }

    public override void TriggerAttack()
    {
        if (ink < 5f) return;

        ink = Mathf.Max(0, ink - 5f);

        if (weaponTrans != null)
        {
            weaponTrans.SetValue(ink);
        }

        base.TriggerAttack();

        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (lastDir == Vector2.zero)
        {
            lastDir = Vector2.right;
        }

        if (inkballPrefab != null && inkPoint != null)
        {
            GameObject inkball = Instantiate(inkballPrefab, inkPoint.position, Quaternion.identity);
            inkball.transform.right = lastDir;
        }
    }

    public void AddInk(float value)
    {
        ink += value;
        ink = Mathf.Min(ink, maxInk);

        if (weaponTrans != null)
        {
            weaponTrans.SetValue(ink);
        }
    }
}