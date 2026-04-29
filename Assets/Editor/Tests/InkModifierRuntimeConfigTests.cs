using NUnit.Framework;
using UnityEngine;

public sealed class InkModifierRuntimeConfigTests
{
    private GameObject rootObject;

    [SetUp]
    public void SetUp()
    {
        if (BackpackMananger.Instance != null)
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }
    }

    [Test]
    public void MortiseAndTenonJointFiresFanProjectilesCappedAtSix()
    {
        BackpackMananger backpack = CreateBackpackWith(
            ArchitecturalType.MortiseAndTenonJoint,
            6);

        InkAttackRuntimeConfig config = InkModifierRuntimeConfig.BuildFromBackpack(backpack);

        Assert.AreEqual(6, config.projectileCount);
        Assert.AreEqual(1, config.burstShotCount);
        Assert.AreEqual(1, config.maxHitCount);
    }

    [Test]
    public void BracketsFiresMultipleWavesCappedAtThree()
    {
        BackpackMananger backpack = CreateBackpackWith(ArchitecturalType.Brackets, 4);

        InkAttackRuntimeConfig config = InkModifierRuntimeConfig.BuildFromBackpack(backpack);

        Assert.AreEqual(1, config.projectileCount);
        Assert.AreEqual(3, config.burstShotCount);
    }

    [Test]
    public void BeamFrameReducesAttackIntervalButNotBelowPointFourSeconds()
    {
        BackpackMananger backpack = CreateBackpackWith(ArchitecturalType.BeamFrame, 6);

        InkAttackRuntimeConfig config = WeaponAttackProfile
            .FromWeaponType(WeaponType.DirectInk)
            .ApplyToInkConfig(InkModifierRuntimeConfig.BuildFromBackpack(backpack));

        Assert.AreEqual(0.4f, config.attackInterval, 0.001f);
        Assert.AreEqual(1f, config.speedMultiplier, 0.001f);
        Assert.AreEqual(1f, config.lifetimeMultiplier, 0.001f);
    }

    [Test]
    public void GroundMassIncreasesProjectileScaleAndDamage()
    {
        BackpackMananger backpack = CreateBackpackWith(ArchitecturalType.GroundMass, 2);

        InkAttackRuntimeConfig config = InkModifierRuntimeConfig.BuildFromBackpack(backpack);

        Assert.Greater(config.projectileScale, 1f);
        Assert.Greater(config.damageMultiplier, 1f);
        Assert.AreEqual(0f, config.debuff.knockbackForce);
    }

    [Test]
    public void TampedEarthIncreasesRangeAndSpeedWithoutSlowDebuff()
    {
        BackpackMananger backpack = CreateBackpackWith(ArchitecturalType.TampedEarth, 2);

        InkAttackRuntimeConfig config = InkModifierRuntimeConfig.BuildFromBackpack(backpack);

        Assert.Greater(config.speedMultiplier, 1f);
        Assert.Greater(config.lifetimeMultiplier, 1f);
        Assert.AreEqual(0f, config.debuff.slowRatio);
    }

    [Test]
    public void TileIncreasesProjectileScaleAndReducesInkCost()
    {
        BackpackMananger backpack = CreateBackpackWith(ArchitecturalType.Tile, 2);
        WeaponAttackProfile profile = new WeaponAttackProfile(new InkTypeDefinition
        {
            inkType = InkType.DirectInk,
            displayColor = Color.white,
            attackInterval = 1f,
            attackRange = 5f,
            projectileSpeed = 6f,
            projectileScale = 1f,
            inkCost = 5,
            baseProjectileCount = 1,
            baseHitCount = 1,
            fanAngleStep = 12f
        });

        InkAttackRuntimeConfig config = profile.ApplyToInkConfig(
            InkModifierRuntimeConfig.BuildFromBackpack(backpack));

        Assert.Greater(config.projectileScale, 1f);
        Assert.Less(config.inkCost, 5);
    }

    [Test]
    public void InkTypeProfilesMatchWeaponEffectSemantics()
    {
        InkAttackRuntimeConfig direct = BuildWeaponConfig(WeaponType.DirectInk);
        Assert.AreEqual(1, direct.maxHitCount);
        Assert.IsFalse(direct.explodeOnHit);
        Assert.IsFalse(direct.debuff.HasDamageOverTime);

        InkAttackRuntimeConfig burst = BuildWeaponConfig(WeaponType.BurstInk);
        Assert.AreEqual(1, burst.maxHitCount);
        Assert.IsTrue(burst.explodeOnHit);
        Assert.Greater(burst.explosionRadius, 0f);
        Assert.IsFalse(burst.debuff.HasDamageOverTime);

        InkAttackRuntimeConfig pierce = BuildWeaponConfig(WeaponType.PierceInk);
        Assert.Greater(pierce.maxHitCount, 1);
        Assert.IsFalse(pierce.explodeOnHit);
        Assert.IsFalse(pierce.debuff.HasDamageOverTime);

        InkAttackRuntimeConfig flow = BuildWeaponConfig(WeaponType.FlowInk);
        Assert.AreEqual(1, flow.maxHitCount);
        Assert.IsFalse(flow.explodeOnHit);
        Assert.IsTrue(flow.debuff.HasDamageOverTime);
    }

    [Test]
    public void InkTypeEffectDescriptionsUseMechanicCopy()
    {
        Assert.AreEqual("没有额外效果，单体单次攻击。", InkTypeCatalog.GetEffectDescription(WeaponType.DirectInk));
        Assert.AreEqual("命中目标后，在目标点爆炸，造成范围伤害。", InkTypeCatalog.GetEffectDescription(WeaponType.BurstInk));
        Assert.AreEqual("贯穿目标并造成伤害。", InkTypeCatalog.GetEffectDescription(WeaponType.PierceInk));
        Assert.AreEqual("命中目标后，让目标持续掉血。", InkTypeCatalog.GetEffectDescription(WeaponType.FlowInk));
    }

    [Test]
    public void CommonStructureDefinitionsDoNotCarryUnlistedAttributeBonuses()
    {
        ArchitecturalType[] commonTypes =
        {
            ArchitecturalType.MortiseAndTenonJoint,
            ArchitecturalType.Brackets,
            ArchitecturalType.BeamFrame,
            ArchitecturalType.GroundMass,
            ArchitecturalType.TampedEarth,
            ArchitecturalType.Tile
        };

        for (int i = 0; i < commonTypes.Length; i++)
        {
            CommonStructureCrystalDefinition definition =
                ArchitecturalCrystalFactory.GetCommonStructureDefinition(commonTypes[i]);

            Assert.AreEqual(AttributeBonusType.None, definition.bonusType, commonTypes[i].ToString());
            Assert.AreEqual(0f, definition.bonusValue, commonTypes[i].ToString());
            Assert.AreEqual(AttributeBonusType.None, definition.subBonusType, commonTypes[i].ToString());
            Assert.AreEqual(0f, definition.subBonusValue, commonTypes[i].ToString());
        }
    }

    private BackpackMananger CreateBackpackWith(ArchitecturalType type, int count)
    {
        rootObject = new GameObject("RuntimeBackpackManager");
        BackpackMananger backpack = rootObject.AddComponent<BackpackMananger>();
        for (int i = 0; i < count; i++)
        {
            Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateCommonStructure(type)));
        }

        return backpack;
    }

    private static InkAttackRuntimeConfig BuildWeaponConfig(WeaponType weaponType)
    {
        return WeaponAttackProfile
            .FromWeaponType(weaponType)
            .ApplyToInkConfig(InkAttackRuntimeConfig.Default);
    }
}
