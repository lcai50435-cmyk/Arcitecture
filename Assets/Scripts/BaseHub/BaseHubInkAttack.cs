using UnityEngine;
using System.Collections;

public class BaseHubInkAttack : MonoBehaviour
{
    private const float MultiShotLateralSpacing = 0.22f;

    private DirectionTracker directionTracker;
    private float cooldownTimer;
    private Coroutine fireSequenceCoroutine;

    private void Awake()
    {
        directionTracker = GetComponent<DirectionTracker>();
    }

    private void Update()
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (!PlayerLoadoutRuntime.AllowBaseAttack)
        {
            return;
        }

        KeyCode attackKey = GameSettingsStore.GetKeyBinding(GameInputAction.Attack);
        if (attackKey == KeyCode.None || !Input.GetKeyDown(attackKey))
        {
            return;
        }

        if (RuntimeUiInputGuard.ShouldBlockGameplayAttack(attackKey))
        {
            return;
        }

        Fire();
    }

    private void Fire()
    {
        if (cooldownTimer > 0f)
        {
            return;
        }

        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance);
        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(effectiveWeaponType);
        InkAttackRuntimeConfig config = profile.ApplyToInkConfig(InkModifierRuntimeConfig.BuildFromBackpack(BackpackMananger.Instance));
        cooldownTimer = config.attackInterval;
        MusicManager.PlaySfx(SfxCueId.PlayerAttack);

        Vector2 direction = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        if (fireSequenceCoroutine != null)
        {
            StopCoroutine(fireSequenceCoroutine);
        }

        fireSequenceCoroutine = StartCoroutine(FireSequence(direction.normalized, config));
    }

    private IEnumerator FireSequence(Vector2 direction, InkAttackRuntimeConfig config)
    {
        int burstShotCount = Mathf.Max(1, config.burstShotCount);
        float burstInterval = Mathf.Max(0.01f, config.burstInterval);

        for (int shotIndex = 0; shotIndex < burstShotCount; shotIndex++)
        {
            SpawnProjectiles(direction, config);

            if (shotIndex < burstShotCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        fireSequenceCoroutine = null;
    }

    private void SpawnProjectiles(Vector2 direction, InkAttackRuntimeConfig config)
    {
        int projectileCount = Mathf.Max(1, config.projectileCount);
        float centerIndex = (projectileCount - 1) * 0.5f;
        Vector2 lateral = new Vector2(-direction.y, direction.x);

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - centerIndex) * config.fanAngleStep;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;

            GameObject projectile = new GameObject("BaseHubInkProjectile");
            Vector3 spawnPosition = transform.position + (Vector3)(shotDirection.normalized * 0.62f);
            if (projectileCount > 1)
            {
                float lateralOffset = (i - centerIndex) * MultiShotLateralSpacing;
                spawnPosition += (Vector3)(lateral * lateralOffset);
            }

            projectile.transform.position = spawnPosition;
            projectile.transform.right = shotDirection;

            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateProjectileSprite();
            renderer.color = config.displayColor;
            renderer.sortingOrder = 8;

            Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;

            InkBall inkBall = projectile.AddComponent<InkBall>();
            inkBall.character = GetComponent<CharacterCore>();
            inkBall.Init(config);
        }
    }

    private void OnDisable()
    {
        if (fireSequenceCoroutine != null)
        {
            StopCoroutine(fireSequenceCoroutine);
            fireSequenceCoroutine = null;
        }
    }

    private static Sprite CreateProjectileSprite()
    {
        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= 6.8f ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
    }
}
