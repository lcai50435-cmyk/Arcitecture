using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    private readonly KeyCode attackKey = KeyCode.Mouse0;
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
        weaponTrans = GameplayStatusHudRuntime.EnsureWeaponGauge(weaponTrans);
        base.Awake();

        if (weaponTrans != null)
        {
            weaponTrans.SetMaxValue(maxInk);
            weaponTrans.SetValue(ink);
            GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk);
        }
    }

    private void Start()
    {
        weaponTrans = GameplayStatusHudRuntime.EnsureWeaponGauge(weaponTrans);
        if (weaponTrans != null)
        {
            weaponTrans.SetMaxValue(maxInk);
            weaponTrans.SetValue(ink);
            GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk);
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
        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(PlayerLoadoutRuntime.CurrentWeaponType);
        if (ink < profile.inkCost) return;

        ink = Mathf.Max(0f, ink - profile.inkCost);
        if (weaponTrans != null)
        {
            weaponTrans.SetValue(ink);
            GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk);
        }

        base.TriggerAttack();

        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (lastDir == Vector2.zero)
        {
            lastDir = Vector2.right;
        }

        if (profile.usesMelee)
        {
            PerformMeleeAttack(lastDir.normalized, profile);
            return;
        }

        InkAttackRuntimeConfig inkConfig = InkModifierRuntimeConfig.BuildFromBackpack(BackpackMananger.Instance);
        SpawnInkBalls(lastDir.normalized, profile.ApplyToInkConfig(inkConfig));
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

    private void PerformMeleeAttack(Vector2 direction, WeaponAttackProfile profile)
    {
        Vector2 center = (Vector2)transform.position + direction * profile.meleeRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, profile.meleeRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            CharacterCore target = hits[i].GetComponent<CharacterCore>();
            if (target == null || target == core)
            {
                continue;
            }

            target.TakeDamage(core.stats.attackDamage * profile.meleeDamageMultiplier);

            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null && profile.meleeKnockbackForce > 0f)
            {
                targetBody.AddForce(direction * profile.meleeKnockbackForce, ForceMode2D.Impulse);
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
            GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk);
        }
    }
}
