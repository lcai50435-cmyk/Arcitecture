using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    private readonly KeyCode attackKey = KeyCode.Mouse0;
    private const float InkCostPerAttack = 5f;
    private DirectionTracker directionTracker;
    private Animator animator;

    [Header("远程攻击")]
    public GameObject inkballPrefab;
    public Transform inkPoint;

    [Header("墨水条")]
    public ValueTrans weaponTrans;

    [Header("墨水值")]
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
        if (ink < InkCostPerAttack) return;

        ink = Mathf.Max(0f, ink - InkCostPerAttack);
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

        InkAttackRuntimeConfig inkConfig = InkModifierRuntimeConfig.BuildFromBackpack(BackpackMananger.Instance);
        SpawnInkBalls(lastDir.normalized, inkConfig);
    }

    private void SpawnInkBalls(Vector2 direction, InkAttackRuntimeConfig inkConfig)
    {
        if (inkballPrefab == null || inkPoint == null)
        {
            return;
        }

        int projectileCount = Mathf.Max(1, inkConfig.projectileCount);
        float centerIndex = (projectileCount - 1) * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - centerIndex) * inkConfig.fanAngleStep;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;

            GameObject inkball = Instantiate(inkballPrefab, inkPoint.position, Quaternion.identity);
            inkball.transform.right = shotDirection;

            InkBall inkBallComponent = inkball.GetComponent<InkBall>();
            if (inkBallComponent != null)
            {
                inkBallComponent.Init(inkConfig);
            }
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
