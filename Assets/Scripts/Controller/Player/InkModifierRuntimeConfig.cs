using System.Collections.Generic;
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
    public float dotDuration;
    public float dotTickInterval;
    public float dotDamageMultiplier;

    public bool HasSlow => slowRatio > 0f && slowDuration > 0f;
    public bool HasKnockback => knockbackForce > 0f;
    public bool HasDamageOverTime => dotDuration > 0f && dotTickInterval > 0f && dotDamageMultiplier > 0f;
}

public struct InkAttackRuntimeConfig
{
    public InkType inkType;
    public Color displayColor;
    public int projectileCount;
    public int burstShotCount;
    public int maxHitCount;
    public float projectileScale;
    public Vector2 projectileStretch;
    public float speedMultiplier;
    public float lifetimeMultiplier;
    public float fanAngleStep;
    public float fanAngleBonus;
    public float burstInterval;
    public float baseProjectileSpeed;
    public float baseProjectileLifetime;
    public float attackInterval;
    public int inkCost;
    public bool explodeOnHit;
    public float explosionRadius;
    public float explosionDamageMultiplier;
    public float impactPulseScale;
    public float impactPulseDuration;
    public InkDebuffRuntimeConfig debuff;
    public bool enableTrailAfterImage;
    public float trailSpawnInterval;
    public float trailLifetime;
    public float trailScaleMultiplier;
    public float trailAlpha;
    public bool enableHeavyShockwave;
    public float heavyShockwaveScale;
    public float heavyShockwaveDurationMultiplier;
    public bool enableSlowResidue;
    public float slowResidueScale;
    public float slowResidueDuration;

    public static InkAttackRuntimeConfig Default => new InkAttackRuntimeConfig
    {
        inkType = InkType.DirectInk,
        displayColor = Color.white,
        projectileCount = 1,
        burstShotCount = 1,
        maxHitCount = 1,
        projectileScale = 1f,
        projectileStretch = Vector2.one,
        speedMultiplier = 1f,
        lifetimeMultiplier = 1f,
        fanAngleStep = 12f,
        fanAngleBonus = 0f,
        burstInterval = 0.09f,
        baseProjectileSpeed = 6f,
        baseProjectileLifetime = 5f / 6f,
        attackInterval = 1f,
        inkCost = 1,
        explodeOnHit = false,
        explosionRadius = 1.35f,
        explosionDamageMultiplier = 1f,
        impactPulseScale = 0.9f,
        impactPulseDuration = 0.16f,
        debuff = new InkDebuffRuntimeConfig(),
        enableTrailAfterImage = false,
        trailSpawnInterval = 0.05f,
        trailLifetime = 0.18f,
        trailScaleMultiplier = 0.82f,
        trailAlpha = 0.28f,
        enableHeavyShockwave = false,
        heavyShockwaveScale = 1.45f,
        heavyShockwaveDurationMultiplier = 1.25f,
        enableSlowResidue = false,
        slowResidueScale = 1.1f,
        slowResidueDuration = 0.55f
    };
}

public static class InkModifierRuntimeConfig
{
    private const float TileScaleBonus = 0.28f;
    private const float BeamSpeedAndRangeBonus = 0.14f;
    private const float MortiseStretchBonus = 0.55f;
    private const float MortiseHeightCompression = 0.12f;
    private const float BracketDualShotAngleBonus = 14f;
    private const float BracketTripleFanAngleBonus = 22f;
    private const float BracketQuintFanAngleBonus = 30f;
    private const float SlowPerModifier = 0.2f;
    private const float MaxSlowRatio = 0.5f;
    private const float BaseSlowDuration = 2f;
    private const float ExtraSlowDuration = 0.5f;
    private const float BaseKnockbackForce = 1.5f;
    private const float ExtraKnockbackForce = 0.5f;
    private const float GroundMassProjectileScaleBonus = 0.14f;
    private const float GroundMassImpactPulseBonus = 0.28f;
    private const float GroundMassHeavyShockwaveBonus = 0.22f;
    private const float SlowResidueScaleBonus = 0.2f;
    private const float SlowResidueDurationBonus = 0.18f;

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
            if (!item.IsCommonStructure)
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

        config.maxHitCount += mortiseCount;
        config.projectileStretch.x += mortiseCount * MortiseStretchBonus;
        config.projectileStretch.y *= Mathf.Max(0.62f, 1f - mortiseCount * MortiseHeightCompression);
        config.projectileScale += tileCount * TileScaleBonus;
        config.speedMultiplier += beamFrameCount * BeamSpeedAndRangeBonus;
        config.lifetimeMultiplier += beamFrameCount * BeamSpeedAndRangeBonus;

        if (mortiseCount > 0)
        {
            config.enableTrailAfterImage = true;
            config.trailSpawnInterval = Mathf.Min(config.trailSpawnInterval, 0.045f);
            config.trailLifetime = Mathf.Max(config.trailLifetime, 0.22f + (mortiseCount - 1) * 0.04f);
            config.trailScaleMultiplier = Mathf.Max(config.trailScaleMultiplier, 0.92f);
            config.trailAlpha = Mathf.Clamp01(config.trailAlpha + mortiseCount * 0.08f);
        }

        if (bracketCount == 1)
        {
            config.projectileCount = Mathf.Max(config.projectileCount, 2);
            config.fanAngleBonus += BracketDualShotAngleBonus;
        }
        else if (bracketCount == 2)
        {
            config.projectileCount = Mathf.Max(config.projectileCount, 3);
            config.fanAngleBonus += BracketTripleFanAngleBonus;
        }
        else if (bracketCount >= 3)
        {
            config.projectileCount = Mathf.Max(config.projectileCount, 5);
            config.fanAngleBonus += BracketQuintFanAngleBonus;
            config.burstShotCount = 2;
            config.burstInterval = 0.07f;
        }

        if (tampedEarthCount > 0)
        {
            config.debuff.slowRatio = Mathf.Min(tampedEarthCount * SlowPerModifier, MaxSlowRatio);
            config.debuff.slowDuration = BaseSlowDuration + (tampedEarthCount - 1) * ExtraSlowDuration;
            config.enableSlowResidue = true;
            config.slowResidueScale += tampedEarthCount * SlowResidueScaleBonus;
            config.slowResidueDuration += (tampedEarthCount - 1) * SlowResidueDurationBonus;
            config.impactPulseScale += tampedEarthCount * 0.08f;
        }

        if (groundMassCount > 0)
        {
            config.debuff.knockbackForce = BaseKnockbackForce + (groundMassCount - 1) * ExtraKnockbackForce;
            config.projectileScale += groundMassCount * GroundMassProjectileScaleBonus;
            config.impactPulseScale += groundMassCount * GroundMassImpactPulseBonus;
            config.impactPulseDuration += groundMassCount * 0.03f;
            config.enableHeavyShockwave = true;
            config.heavyShockwaveScale += groundMassCount * GroundMassHeavyShockwaveBonus;
            config.heavyShockwaveDurationMultiplier += groundMassCount * 0.08f;
        }

        if (beamFrameCount > 0)
        {
            config.enableTrailAfterImage = true;
            config.trailSpawnInterval = Mathf.Min(config.trailSpawnInterval, 0.034f);
            config.trailLifetime = Mathf.Max(config.trailLifetime, 0.18f + (beamFrameCount - 1) * 0.03f);
            config.trailAlpha = Mathf.Clamp01(config.trailAlpha + beamFrameCount * 0.05f);
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

    public static string GetVolleyLabel(int projectileCount)
    {
        int safeCount = Mathf.Max(1, projectileCount);
        switch (safeCount)
        {
            case 1:
                return "单发";
            case 2:
                return "二连发";
            case 3:
                return "三连发";
            case 4:
                return "四连发";
            default:
                return $"{safeCount}连发";
        }
    }

    public static string GetAttackPatternLabel(InkAttackRuntimeConfig config)
    {
        if (config.projectileCount >= 5 && config.burstShotCount > 1)
        {
            return $"{config.projectileCount}发扇形连击";
        }

        if (config.projectileCount >= 3)
        {
            return $"{config.projectileCount}发扇形";
        }

        if (config.projectileCount == 2)
        {
            return "双弹齐射";
        }

        if (config.burstShotCount > 1)
        {
            return $"{config.burstShotCount}连击";
        }

        return "单发";
    }

    public static string BuildActiveModifierSummary(BackpackMananger backpack, WeaponType weaponType)
    {
        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(weaponType);
        InkAttackRuntimeConfig config = profile.ApplyToInkConfig(BuildFromBackpack(backpack));
        List<string> parts = new List<string>();

        if (config.burstShotCount > 1 || config.projectileCount > 1)
        {
            parts.Add(GetAttackPatternLabel(config));
        }

        if (config.maxHitCount > 1)
        {
            parts.Add($"贯穿 x{config.maxHitCount}");
        }

        if (config.projectileScale > 1.01f)
        {
            parts.Add($"大墨团 x{config.projectileScale:0.00}");
        }

        if (config.debuff.slowRatio > 0f)
        {
            parts.Add($"滞留减速 {config.debuff.slowRatio:P0}");
        }

        if (config.debuff.knockbackForce > 0f)
        {
            parts.Add($"重击震退 {config.debuff.knockbackForce:0.0}");
        }

        if (config.speedMultiplier > 1.01f)
        {
            parts.Add($"疾射 x{config.speedMultiplier:0.00}");
        }

        if (config.lifetimeMultiplier > 1.01f)
        {
            parts.Add($"长程 x{config.lifetimeMultiplier:0.00}");
        }

        return parts.Count > 0 ? string.Join(" / ", parts) : "当前无临时构筑效果";
    }

    public static string BuildCrystalActivationText(ArchitecturalCrystal crystal, BackpackMananger backpack, WeaponType weaponType)
    {
        if (!crystal.IsCommonStructure)
        {
            return string.Empty;
        }

        WeaponAttackProfile profile = WeaponAttackProfile.FromWeaponType(weaponType);
        InkAttackRuntimeConfig config = profile.ApplyToInkConfig(BuildFromBackpack(backpack));

        string effectLine;
        switch (crystal.type)
        {
            case ArchitecturalType.Brackets:
                effectLine = $"已生效：{GetAttackPatternLabel(config)}";
                break;
            case ArchitecturalType.MortiseAndTenonJoint:
                effectLine = $"已生效：贯穿墨矛 x{config.maxHitCount}";
                break;
            case ArchitecturalType.Tile:
                effectLine = $"已生效：大墨团 x{config.projectileScale:0.00}";
                break;
            case ArchitecturalType.TampedEarth:
                effectLine = $"已生效：滞留减速 {config.debuff.slowRatio:P0}";
                break;
            case ArchitecturalType.GroundMass:
                effectLine = $"已生效：重击冲波 / 击退 {config.debuff.knockbackForce:0.0}";
                break;
            case ArchitecturalType.BeamFrame:
                effectLine = $"已生效：疾射 x{config.speedMultiplier:0.00} / 长程 x{config.lifetimeMultiplier:0.00}";
                break;
            default:
                effectLine = "已生效";
                break;
        }

        return $"{crystal.DisplayName} {effectLine}\n当前构筑：{BuildActiveModifierSummary(backpack, weaponType)}";
    }
}
