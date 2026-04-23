using UnityEngine;
using System.Collections;

public class PlayerAttack : CharacterAttack
{
    private const float MultiShotLateralSpacing = 0.22f;

    private DirectionTracker directionTracker;
    private Animator animator;
    private float nextAttackTime;
    private Coroutine attackSequenceCoroutine;

    [Header("远程攻击")]
    public GameObject inkballPrefab;
    public Transform inkPoint;

    [Header("墨水条")]
    public ValueTrans weaponTrans;

    [Header("墨水值")]
    public float ink = 100f;
    public float maxInk = 100f;
    public float baseMaxInk = 100f;

    protected override bool ShouldMirrorRootForAttack => false;

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
        KeyCode attackKey = GameSettingsStore.GetKeyBinding(GameInputAction.Attack);
        if (attackKey == KeyCode.None || !Input.GetKeyDown(attackKey))
        {
            return;
        }

        if (RuntimeUiInputGuard.ShouldBlockGameplayAttack(attackKey))
        {
            return;
        }

        if (!isAttacking && Time.time >= nextAttackTime)
        {
            TriggerAttack();
        }
    }

    public override void TriggerAttack()
    {
        if (inkballPrefab == null || inkPoint == null)
        {
            return;
        }

        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance);
        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(effectiveWeaponType);
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
        MusicManager.PlaySfx(SfxCueId.PlayerAttack);

        Vector2 lastDir = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (lastDir == Vector2.zero)
        {
            lastDir = Vector2.right;
        }

        if (attackSequenceCoroutine != null)
        {
            StopCoroutine(attackSequenceCoroutine);
        }

        attackSequenceCoroutine = StartCoroutine(FireAttackSequence(lastDir.normalized, inkConfig));
    }

    private IEnumerator FireAttackSequence(Vector2 direction, InkAttackRuntimeConfig inkConfig)
    {
        int burstShotCount = Mathf.Max(1, inkConfig.burstShotCount);
        float burstInterval = Mathf.Max(0.01f, inkConfig.burstInterval);

        for (int shotIndex = 0; shotIndex < burstShotCount; shotIndex++)
        {
            SpawnInkBalls(direction, inkConfig);

            if (shotIndex < burstShotCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        attackSequenceCoroutine = null;
    }

    private void SpawnInkBalls(Vector2 direction, InkAttackRuntimeConfig inkConfig)
    {
        int projectileCount = Mathf.Max(1, inkConfig.projectileCount);
        float centerIndex = (projectileCount - 1) * 0.5f;
        Vector2 lateral = new Vector2(-direction.y, direction.x);

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - centerIndex) * inkConfig.fanAngleStep;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            Vector3 spawnPosition = inkPoint.position;
            if (projectileCount > 1)
            {
                float lateralOffset = (i - centerIndex) * MultiShotLateralSpacing;
                spawnPosition += (Vector3)(lateral * lateralOffset);
            }

            GameObject inkball = Instantiate(inkballPrefab, spawnPosition, Quaternion.identity);
            inkball.transform.right = shotDirection;

            InkBall inkBallComponent = inkball.GetComponent<InkBall>();
            if (inkBallComponent != null)
            {
                inkBallComponent.Init(inkConfig);
            }
        }
    }

    protected override void OnDisable()
    {
        if (attackSequenceCoroutine != null)
        {
            StopCoroutine(attackSequenceCoroutine);
            attackSequenceCoroutine = null;
        }

        base.OnDisable();
    }

    public void AddInk(float value)
    {
        ink = Mathf.Clamp(ink + value, 0f, maxInk);
        RefreshInkUI();
    }

    public void RefreshInkUI()
    {
        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance);

        if (weaponTrans != null)
        {
            weaponTrans.SetMaxValue(maxInk);
            weaponTrans.SetValue(ink);
        }

        GameplayStatusHudRuntime.RefreshWeaponText(ink, maxInk, effectiveWeaponType);

        PlayerProfileData profile = GetComponent<PlayerProfileData>();
        if (profile != null)
        {
            profile.currentDurability = ink;
            profile.maxDurability = maxInk;
            profile.SetEffectiveWeapon(effectiveWeaponType);
        }
    }
}
