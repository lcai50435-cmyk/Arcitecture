using UnityEngine;

public enum InkModifierType
{
    ProjectileCount,
    HitCount,
    ProjectileScale,
    SlowDebuff,
    KnockbackDebuff,
    SpeedAndRange
}

public struct InkDebuffRuntimeConfig
{
    public float slowRatio;
    public float slowDuration;
    public float knockbackForce;

    public bool HasSlow => slowRatio > 0f && slowDuration > 0f;
    public bool HasKnockback => knockbackForce > 0f;
}

public struct InkAttackRuntimeConfig
{
    public int projectileCount;
    public int maxHitCount;
    public float projectileScale;
    public float speedMultiplier;
    public float lifetimeMultiplier;
    public float fanAngleStep;
    public InkDebuffRuntimeConfig debuff;

    public static InkAttackRuntimeConfig Default => new InkAttackRuntimeConfig
    {
        projectileCount = 1,
        maxHitCount = 1,
        projectileScale = 1f,
        speedMultiplier = 1f,
        lifetimeMultiplier = 1f,
        fanAngleStep = 12f,
        debuff = new InkDebuffRuntimeConfig()
    };
}

public static class InkModifierRuntimeConfig
{
    private const float TileScaleBonus = 0.15f;
    private const float BeamSpeedAndRangeBonus = 0.1f;
    private const float SlowPerModifier = 0.2f;
    private const float MaxSlowRatio = 0.5f;
    private const float BaseSlowDuration = 2f;
    private const float ExtraSlowDuration = 0.5f;
    private const float BaseKnockbackForce = 1.5f;
    private const float ExtraKnockbackForce = 0.5f;

    public static InkAttackRuntimeConfig BuildFromBackpack(BackpackMananger backpack)
    {
        InkAttackRuntimeConfig config = InkAttackRuntimeConfig.Default;
        if (backpack == null || backpack.backpackItems == null)
        {
            return config;
        }

        int bracketCount = 0;
        int mortiseCount = 0;
        int tileCount = 0;
        int tampedEarthCount = 0;
        int groundMassCount = 0;
        int beamFrameCount = 0;

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            ArchitecturalCrystal? nullableItem = backpack.backpackItems[i];
            if (!nullableItem.HasValue)
            {
                continue;
            }

            ArchitecturalCrystal item = nullableItem.Value;
            if (item.isUnlockMaterial)
            {
                continue;
            }

            if (!TryGetModifierType(item.type, out InkModifierType modifierType))
            {
                continue;
            }

            switch (modifierType)
            {
                case InkModifierType.ProjectileCount:
                    bracketCount++;
                    break;
                case InkModifierType.HitCount:
                    mortiseCount++;
                    break;
                case InkModifierType.ProjectileScale:
                    tileCount++;
                    break;
                case InkModifierType.SlowDebuff:
                    tampedEarthCount++;
                    break;
                case InkModifierType.KnockbackDebuff:
                    groundMassCount++;
                    break;
                case InkModifierType.SpeedAndRange:
                    beamFrameCount++;
                    break;
            }
        }

        config.projectileCount += bracketCount;
        config.maxHitCount += mortiseCount;
        config.projectileScale += tileCount * TileScaleBonus;
        config.speedMultiplier += beamFrameCount * BeamSpeedAndRangeBonus;
        config.lifetimeMultiplier += beamFrameCount * BeamSpeedAndRangeBonus;

        if (tampedEarthCount > 0)
        {
            config.debuff.slowRatio = Mathf.Min(tampedEarthCount * SlowPerModifier, MaxSlowRatio);
            config.debuff.slowDuration = BaseSlowDuration + (tampedEarthCount - 1) * ExtraSlowDuration;
        }

        if (groundMassCount > 0)
        {
            config.debuff.knockbackForce = BaseKnockbackForce + (groundMassCount - 1) * ExtraKnockbackForce;
        }

        return config;
    }

    public static bool TryGetModifierType(ArchitecturalType type, out InkModifierType modifierType)
    {
        switch (type)
        {
            case ArchitecturalType.Brackets:
                modifierType = InkModifierType.ProjectileCount;
                return true;
            case ArchitecturalType.MortiseAndTenonJoint:
                modifierType = InkModifierType.HitCount;
                return true;
            case ArchitecturalType.Tile:
                modifierType = InkModifierType.ProjectileScale;
                return true;
            case ArchitecturalType.TampedEarth:
                modifierType = InkModifierType.SlowDebuff;
                return true;
            case ArchitecturalType.GroundMass:
                modifierType = InkModifierType.KnockbackDebuff;
                return true;
            case ArchitecturalType.BeamFrame:
                modifierType = InkModifierType.SpeedAndRange;
                return true;
            default:
                modifierType = InkModifierType.ProjectileCount;
                return false;
        }
    }
}
