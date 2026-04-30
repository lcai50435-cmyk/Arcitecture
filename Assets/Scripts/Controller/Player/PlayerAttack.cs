using UnityEngine;
using System.Collections;

public class PlayerAttack : CharacterAttack
{
    private const float MultiShotLateralSpacing = 0.22f;
    private const float FallbackProjectileForwardOffset = 0.62f;
    private const float FallbackProjectileForwardPadding = 0.12f;

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
        EnsureRuntimeBindings();
        weaponTrans = GameplayStatusHudRuntime.EnsureWeaponGauge(weaponTrans);
        base.Awake();
        EnsureRuntimeBindings();

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
        EnsureRuntimeBindings();
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
        EnsureRuntimeBindings();

        if (inkPoint == null)
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
            Vector3 spawnPosition = ResolveProjectileSpawnPosition(shotDirection);
            if (projectileCount > 1)
            {
                float lateralOffset = (i - centerIndex) * MultiShotLateralSpacing;
                spawnPosition += (Vector3)(lateral * lateralOffset);
            }

            GameObject inkball = inkballPrefab != null
                ? Instantiate(inkballPrefab, spawnPosition, Quaternion.identity)
                : CreateRuntimeInkBall(spawnPosition);
            inkball.transform.right = shotDirection;

            InkBall inkBallComponent = inkball.GetComponent<InkBall>();
            if (inkBallComponent != null)
            {
                inkBallComponent.character = GetComponent<CharacterCore>();
                inkBallComponent.Init(inkConfig);
            }
        }
    }

    private Vector3 ResolveProjectileSpawnPosition(Vector2 shotDirection)
    {
        Vector2 normalizedDirection = shotDirection.sqrMagnitude > 0.0001f
            ? shotDirection.normalized
            : Vector2.right;
        Transform spawnTransform = inkPoint != null ? inkPoint : transform;
        Vector3 spawnPosition = spawnTransform.position;
        Collider2D ownerCollider = GetComponent<Collider2D>();

        if (spawnTransform == transform || IsInsideOwnerCollider(spawnPosition, ownerCollider))
        {
            spawnPosition += (Vector3)(normalizedDirection * ResolveFallbackProjectileForwardOffset(
                normalizedDirection,
                ownerCollider));
        }

        return spawnPosition;
    }

    private float ResolveFallbackProjectileForwardOffset(Vector2 normalizedDirection, Collider2D ownerCollider)
    {
        if (ownerCollider == null)
        {
            return FallbackProjectileForwardOffset;
        }

        Bounds ownerBounds = ownerCollider.bounds;
        Vector2 ownerCenterDelta = ownerBounds.center - transform.position;
        float centerForwardOffset = Vector2.Dot(ownerCenterDelta, normalizedDirection);
        float projectedExtent =
            Mathf.Abs(normalizedDirection.x) * ownerBounds.extents.x +
            Mathf.Abs(normalizedDirection.y) * ownerBounds.extents.y;

        return Mathf.Max(
            FallbackProjectileForwardOffset,
            centerForwardOffset + projectedExtent + FallbackProjectileForwardPadding);
    }

    private static bool IsInsideOwnerCollider(Vector3 spawnPosition, Collider2D ownerCollider)
    {
        return ownerCollider != null && ownerCollider.bounds.Contains(spawnPosition);
    }

    private GameObject CreateRuntimeInkBall(Vector3 spawnPosition)
    {
        GameObject inkball = new GameObject("RuntimeInkBall");
        inkball.transform.position = spawnPosition;

        SpriteRenderer renderer = inkball.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 8;

        Rigidbody2D body = inkball.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        CircleCollider2D collider = inkball.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.22f;

        inkball.AddComponent<InkBall>();
        return inkball;
    }

    private void EnsureRuntimeBindings()
    {
        RuntimeMiniMapHud.EnsureInstance();
        directionTracker ??= GetComponent<DirectionTracker>();
        animator ??= GetComponent<Animator>();

        if (anim == null)
        {
            anim = animator;
        }

        if (moveScript == null)
        {
            moveScript = GetComponent<PlayerMove>();
        }

        if (playerMove == null && moveScript != null)
        {
            playerMove = moveScript.GetComponent<PlayerMove>();
        }

        if (inkPoint == null)
        {
            inkPoint = transform;
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
        EnsureRuntimeBindings();
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
