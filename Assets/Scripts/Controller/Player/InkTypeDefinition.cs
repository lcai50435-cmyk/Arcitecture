using System;
using System.Collections.Generic;
using UnityEngine;

public enum InkType
{
    DirectInk,
    BurstInk,
    PierceInk,
    FlowInk
}

[Serializable]
public class InkTypeDefinition
{
    public InkType inkType;
    public string displayName;
    public Color displayColor = Color.white;
    public float baseDamage = 20f;
    public float attackInterval = 1f;
    public float attackRange = 5f;
    public float projectileSpeed = 6f;
    public float projectileScale = 1f;
    public int inkCost = 1;
    public int baseProjectileCount = 1;
    public int baseHitCount = 1;
    public float fanAngleStep = 12f;
    public bool explodeOnHit;
    public float explosionRadius = 1.35f;
    public float explosionDamageMultiplier = 1f;
    public bool hasDamageOverTime;
    public float dotDuration = 3f;
    public float dotTickInterval = 1f;
    public float dotDamageMultiplier = 0.35f;
    public Vector2 projectileStretch = Vector2.one;
    public float impactPulseScale = 0.9f;
    public float impactPulseDuration = 0.16f;
}

public static class InkTypeCatalog
{
    private static readonly Dictionary<InkType, InkTypeDefinition> definitions =
        new Dictionary<InkType, InkTypeDefinition>
        {
            {
                InkType.DirectInk,
                new InkTypeDefinition
                {
                    inkType = InkType.DirectInk,
                    displayName = "直墨",
                    displayColor = new Color(0.26f, 0.72f, 0.90f, 1f),
                    projectileStretch = new Vector2(1f, 1f),
                    impactPulseScale = 0.85f,
                    impactPulseDuration = 0.14f
                }
            },
            {
                InkType.BurstInk,
                new InkTypeDefinition
                {
                    inkType = InkType.BurstInk,
                    displayName = "爆墨",
                    displayColor = new Color(0.90f, 0.38f, 0.24f, 1f),
                    projectileStretch = new Vector2(1.15f, 1.15f),
                    explodeOnHit = true,
                    explosionRadius = 1.85f,
                    explosionDamageMultiplier = 1f,
                    impactPulseScale = 2.35f,
                    impactPulseDuration = 0.24f
                }
            },
            {
                InkType.PierceInk,
                new InkTypeDefinition
                {
                    inkType = InkType.PierceInk,
                    displayName = "贯墨",
                    displayColor = new Color(0.94f, 0.78f, 0.28f, 1f),
                    baseHitCount = 3,
                    projectileStretch = new Vector2(1.8f, 0.55f),
                    impactPulseScale = 0.7f,
                    impactPulseDuration = 0.1f
                }
            },
            {
                InkType.FlowInk,
                new InkTypeDefinition
                {
                    inkType = InkType.FlowInk,
                    displayName = "流墨",
                    displayColor = new Color(0.24f, 0.78f, 0.56f, 1f),
                    hasDamageOverTime = true,
                    dotDuration = 3f,
                    dotTickInterval = 0.5f,
                    dotDamageMultiplier = 0.25f,
                    projectileStretch = new Vector2(1.25f, 0.82f),
                    impactPulseScale = 1.05f,
                    impactPulseDuration = 0.2f
                }
            }
        };

    public static InkTypeDefinition Get(InkType inkType)
    {
        return definitions[inkType];
    }

    public static InkTypeDefinition Get(WeaponType weaponType)
    {
        return Get(weaponType.ToInkType());
    }

    public static string GetDisplayName(InkType inkType)
    {
        return Get(inkType).displayName;
    }

    public static string GetDisplayName(WeaponType weaponType)
    {
        return GetDisplayName(weaponType.ToInkType());
    }

    public static Color GetDisplayColor(InkType inkType)
    {
        return Get(inkType).displayColor;
    }

    public static Color GetDisplayColor(WeaponType weaponType)
    {
        return GetDisplayColor(weaponType.ToInkType());
    }
}

public static class InkTypeCompatExtensions
{
    public static InkType ToInkType(this WeaponType weaponType)
    {
        return (InkType)weaponType;
    }

    public static WeaponType ToWeaponType(this InkType inkType)
    {
        return (WeaponType)inkType;
    }
}
