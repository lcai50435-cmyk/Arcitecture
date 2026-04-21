using UnityEngine;

public class BaseHubInkAttack : MonoBehaviour
{
    private DirectionTracker directionTracker;
    private float cooldownTimer;

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

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Fire();
        }
    }

    private void Fire()
    {
        if (cooldownTimer > 0f) return;

        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(PlayerLoadoutRuntime.CurrentWeaponType);
        cooldownTimer = profile.usesMelee ? 0.28f : 0.36f;

        Vector2 direction = directionTracker != null ? directionTracker.LastDirection : Vector2.right;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        if (profile.usesMelee)
        {
            PerformMeleeAttack(direction.normalized, profile);
            return;
        }

        InkAttackRuntimeConfig config = profile.ApplyToInkConfig(InkAttackRuntimeConfig.Default);
        SpawnProjectiles(direction.normalized, config);
    }

    private void PerformMeleeAttack(Vector2 direction, WeaponAttackProfile profile)
    {
        Vector2 center = (Vector2)transform.position + direction * profile.meleeRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, profile.meleeRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            CharacterCore target = hits[i].GetComponent<CharacterCore>();
            if (target == null || target.gameObject == gameObject)
            {
                continue;
            }

            target.TakeDamage(22f * profile.meleeDamageMultiplier);

            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null && profile.meleeKnockbackForce > 0f)
            {
                targetBody.AddForce(direction * profile.meleeKnockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void SpawnProjectiles(Vector2 direction, InkAttackRuntimeConfig config)
    {
        int projectileCount = Mathf.Max(1, config.projectileCount);
        float centerIndex = (projectileCount - 1) * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (i - centerIndex) * config.fanAngleStep;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;

            GameObject projectile = new GameObject("BaseHubInkProjectile");
            projectile.transform.position = transform.position + (Vector3)(shotDirection.normalized * 0.62f);
            projectile.transform.right = shotDirection;
            projectile.transform.localScale = Vector3.one * config.projectileScale;

            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateProjectileSprite();
            renderer.color = GetProjectileColor();
            renderer.sortingOrder = 8;

            Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;

            BaseHubInkProjectile inkProjectile = projectile.AddComponent<BaseHubInkProjectile>();
            inkProjectile.Init(gameObject, shotDirection.normalized, 6f * config.speedMultiplier, 0.85f * config.lifetimeMultiplier, 18f, 0f);
        }
    }

    private Color GetProjectileColor()
    {
        switch (PlayerLoadoutRuntime.CurrentWeaponType)
        {
            case WeaponType.Melee:
                return new Color(0.88f, 0.32f, 0.18f, 1f);
            case WeaponType.Special:
                return new Color(0.94f, 0.76f, 0.22f, 1f);
            default:
                return new Color(0.26f, 0.72f, 0.90f, 1f);
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

public class BaseHubInkProjectile : MonoBehaviour
{
    private GameObject owner;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float knockbackForce;
    private Rigidbody2D body;

    public void Init(GameObject projectileOwner, Vector2 shotDirection, float projectileSpeed, float lifetime, float hitDamage, float hitKnockback)
    {
        owner = projectileOwner;
        direction = shotDirection;
        speed = projectileSpeed;
        damage = hitDamage;
        knockbackForce = hitKnockback;
        body = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (body != null)
        {
            body.velocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.root == owner.transform.root)
        {
            return;
        }

        CharacterCore target = other.GetComponent<CharacterCore>();
        if (target != null)
        {
            target.TakeDamage(damage);

            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null && knockbackForce > 0f)
            {
                targetBody.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }

            Destroy(gameObject);
        }
    }
}
