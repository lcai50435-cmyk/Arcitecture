using UnityEngine;

public readonly struct WeaponAttackProfile
{
    public readonly InkTypeDefinition inkDefinition;

    public float InkCost => inkDefinition != null ? inkDefinition.inkCost : 1f;
    public float AttackInterval => inkDefinition != null ? inkDefinition.attackInterval : 1f;

    public WeaponAttackProfile(InkTypeDefinition inkDefinition)
    {
        this.inkDefinition = inkDefinition;
    }

    public static WeaponAttackProfile FromWeaponType(WeaponType weaponType)
    {
        return new WeaponAttackProfile(InkTypeCatalog.Get(weaponType));
    }

    public InkAttackRuntimeConfig ApplyToInkConfig(InkAttackRuntimeConfig baseConfig)
    {
        InkAttackRuntimeConfig config = baseConfig;
        InkTypeDefinition definition = inkDefinition ?? InkTypeCatalog.Get(InkType.DirectInk);

        config.inkType = definition.inkType;
        config.displayColor = definition.displayColor;
        config.projectileCount = Mathf.Max(config.projectileCount, definition.baseProjectileCount);
        config.maxHitCount = Mathf.Max(config.maxHitCount, definition.baseHitCount);
        config.projectileScale *= Mathf.Max(0.01f, definition.projectileScale);
        config.projectileStretch = Vector2.Scale(config.projectileStretch, definition.projectileStretch);
        config.fanAngleStep = definition.fanAngleStep;
        config.baseProjectileSpeed = definition.projectileSpeed;
        config.baseProjectileLifetime = definition.projectileSpeed > 0.01f
            ? definition.attackRange / definition.projectileSpeed
            : definition.attackRange;
        config.attackInterval = definition.attackInterval;
        config.inkCost = definition.inkCost;
        config.explodeOnHit = definition.explodeOnHit;
        config.explosionRadius = definition.explosionRadius;
        config.explosionDamageMultiplier = definition.explosionDamageMultiplier;
        config.impactPulseScale = definition.impactPulseScale;
        config.impactPulseDuration = definition.impactPulseDuration;

        if (definition.hasDamageOverTime)
        {
            config.debuff.dotDuration = definition.dotDuration;
            config.debuff.dotTickInterval = definition.dotTickInterval;
            config.debuff.dotDamageMultiplier = definition.dotDamageMultiplier;
        }

        return config;
    }
}
