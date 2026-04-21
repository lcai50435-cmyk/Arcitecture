using UnityEngine;

public readonly struct WeaponAttackProfile
{
    public readonly bool usesMelee;
    public readonly int projectileCountBonus;
    public readonly float projectileScaleMultiplier;
    public readonly float speedMultiplier;
    public readonly float lifetimeMultiplier;
    public readonly float fanAngleStep;
    public readonly float inkCost;
    public readonly float meleeDamageMultiplier;
    public readonly float meleeRange;
    public readonly float meleeRadius;
    public readonly float meleeKnockbackForce;

    public WeaponAttackProfile(
        bool usesMelee,
        int projectileCountBonus,
        float projectileScaleMultiplier,
        float speedMultiplier,
        float lifetimeMultiplier,
        float fanAngleStep,
        float inkCost,
        float meleeDamageMultiplier,
        float meleeRange,
        float meleeRadius,
        float meleeKnockbackForce)
    {
        this.usesMelee = usesMelee;
        this.projectileCountBonus = projectileCountBonus;
        this.projectileScaleMultiplier = projectileScaleMultiplier;
        this.speedMultiplier = speedMultiplier;
        this.lifetimeMultiplier = lifetimeMultiplier;
        this.fanAngleStep = fanAngleStep;
        this.inkCost = inkCost;
        this.meleeDamageMultiplier = meleeDamageMultiplier;
        this.meleeRange = meleeRange;
        this.meleeRadius = meleeRadius;
        this.meleeKnockbackForce = meleeKnockbackForce;
    }

    public static WeaponAttackProfile FromWeaponType(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Melee:
                return new WeaponAttackProfile(
                    true,
                    0,
                    1f,
                    1f,
                    1f,
                    0f,
                    0f,
                    1.45f,
                    0.95f,
                    0.78f,
                    1.1f);
            case WeaponType.Special:
                return new WeaponAttackProfile(
                    false,
                    2,
                    1.18f,
                    0.92f,
                    1.15f,
                    20f,
                    10f,
                    1f,
                    0f,
                    0f,
                    0f);
            default:
                return new WeaponAttackProfile(
                    false,
                    0,
                    1f,
                    1f,
                    1f,
                    12f,
                    5f,
                    1f,
                    0f,
                    0f,
                    0f);
        }
    }

    public InkAttackRuntimeConfig ApplyToInkConfig(InkAttackRuntimeConfig baseConfig)
    {
        InkAttackRuntimeConfig config = baseConfig;
        config.projectileCount = Mathf.Max(1, config.projectileCount + projectileCountBonus);
        config.projectileScale *= Mathf.Max(0.01f, projectileScaleMultiplier);
        config.speedMultiplier *= Mathf.Max(0.01f, speedMultiplier);
        config.lifetimeMultiplier *= Mathf.Max(0.01f, lifetimeMultiplier);
        config.fanAngleStep = fanAngleStep > 0f ? fanAngleStep : config.fanAngleStep;
        return config;
    }
}
