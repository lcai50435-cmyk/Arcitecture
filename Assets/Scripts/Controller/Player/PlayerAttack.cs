using UnityEngine;

public class PlayerAttack : CharacterAttack
{
    private readonly KeyCode attackKey = KeyCode.Mouse0;
    private DirectionTracker directionTracker;
    private Animator animator;
    private float nextAttackTime;

    [Header("远程攻击")]
    public GameObject inkballPrefab;
    public Transform inkPoint;

    [Header("墨水条")]
    public ValueTrans weaponTrans;

    [Header("墨水值")]
    public float ink = 100f;
    public float maxInk = 100f;
    public float baseMaxInk = 100f;

    protected override void Awake()
    {
        RuntimeMiniMapHud.EnsureInstance();
        directionTracker = GetComponent<DirectionTracker>();
        animator = GetComponent<Animator>();
        weaponTrans = GameplayStatusHudRuntime.EnsureWeaponGauge(weaponTrans);
        base.Awake();

        baseMaxInk = Mathf.Max(1f, baseMaxInk);
        maxInk = Mathf.Max(baseMaxInk, maxInk);
        if (ink <= 0f)
        {
            ink = maxInk;
        }

        RefreshInkUI();
    }

    private void Start()
    {
        weaponTrans = GameplayStatusHudRuntime.EnsureWeaponGauge(weaponTrans);
        RefreshInkUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
            {
                return;
            }

            if (!isAttacking && Time.time >= nextAttackTime)
            {
                TriggerAttack();
            }
        }
    }

    public override void TriggerAttack()
    {
        if (inkballPrefab == null || inkPoint == null)
        {
            return;
        }

        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(PlayerLoadoutRuntime.CurrentWeaponType);
        InkAttackRuntimeConfig inkConfig = profile.ApplyToInkConfig(
            InkModifierRuntimeConfig.BuildFromBackpack(BackpackMananger.Instance));

        if (ink < inkConfig.inkCost)
        {
            return;
        }

        ink = Mathf.Max(0f, ink - inkConfig.inkCost);
        nextAttackTime = Time.time + inkConfig.attackInterval;
        RefreshInkUI();

        base.TriggerAttack();

        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (lastDir == Vector2.zero)
        {
            lastDir = Vector2.right;
        }

        SpawnInkBalls(lastDir.normalized, inkConfig);
    }

    private void SpawnInkBalls(Vector2 direction, InkAttackRuntimeConfig inkConfig)
    {
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
        ink = Mathf.Clamp(ink + value, 0f, maxInk);
        RefreshInkUI();
    }

    public void RefreshInkUI()
    {
        if (weaponTrans != null)
        {
            weaponTrans.SetMaxValue(maxInk);
            weaponTrans.SetValue(ink);
        }

        GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk);

        PlayerProfileData profile = GetComponent<PlayerProfileData>();
        if (profile != null)
        {
            profile.currentDurability = ink;
            profile.maxDurability = maxInk;
            profile.currentInkType = PlayerLoadoutRuntime.CurrentInkType;
            profile.currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;
        }
    }
}
