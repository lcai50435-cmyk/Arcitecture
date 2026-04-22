using System.Collections.Generic;
using UnityEngine;

public class PlayerAttributeManager : MonoBehaviour
{
    public static PlayerAttributeManager Instance;

    public CharacterCore characterCore;
    public PlayerAttack playerAttack;
    public PlayerTakeDamage playerTakeDamage;
    public PlayerProfileData profileData;

    private readonly Dictionary<AttributeBonusType, float> temporaryBonuses =
        new Dictionary<AttributeBonusType, float>();

    private readonly Dictionary<AttributeBonusType, float> backpackBonuses =
        new Dictionary<AttributeBonusType, float>();

    private readonly Dictionary<AttributeBonusType, float> permanentBonuses =
        new Dictionary<AttributeBonusType, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolveReferences();
        EnsureDesignBaseline();
        RebuildPermanentBonuses();
        ApplyAllBonus();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RuntimeProgressState.EnsureInstance().OnStateChanged += HandleRuntimeStateChanged;
        RebuildPermanentBonuses();
        ApplyAllBonus();
    }

    private void OnDisable()
    {
        if (RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= HandleRuntimeStateChanged;
        }
    }

    public void AddBonus(AttributeBonusType type, float value)
    {
        AddToBonusMap(temporaryBonuses, type, value);
        ApplyAllBonus();
    }

    public void AddBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        AddToBonusMap(temporaryBonuses, type, value);
        AddToBonusMap(temporaryBonuses, subType, subValue);
        ApplyAllBonus();
    }

    public void RemoveBonus(AttributeBonusType type, float value)
    {
        AddToBonusMap(temporaryBonuses, type, -value);
        ApplyAllBonus();
    }

    public void RemoveBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        AddToBonusMap(temporaryBonuses, type, -value);
        AddToBonusMap(temporaryBonuses, subType, -subValue);
        ApplyAllBonus();
    }

    public void ApplyAllBonus()
    {
        ResolveReferences();
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        EnsureDesignBaseline();
        RebuildBackpackBonuses();
        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance);

        if (characterCore == null || characterCore.baseStats == null)
        {
            return;
        }

        float currentMaxHp = characterCore.stats != null ? Mathf.Max(1f, characterCore.stats.maxHp) : 1f;
        float healthRatio = currentMaxHp > 0f ? Mathf.Clamp01(characterCore.currentHp / currentMaxHp) : 1f;

        CharacterStats recalculated = characterCore.baseStats.Clone();
        recalculated.maxHp = Mathf.Max(1f, recalculated.maxHp + GetTotalBonus(AttributeBonusType.MaxHealth));
        recalculated.attackDamage = Mathf.Max(
            Mathf.Max(recalculated.attackDamage, InkTypeCatalog.Get(effectiveWeaponType).baseDamage) +
            GetTotalBonus(AttributeBonusType.AttackPower),
            0f);
        recalculated.moveSpeed = Mathf.Max(0f, recalculated.moveSpeed + GetTotalBonus(AttributeBonusType.MoveSpeed));
        recalculated.defense = Mathf.Max(0f, recalculated.defense + GetTotalBonus(AttributeBonusType.Defense));

        characterCore.stats = recalculated;

        float currentHpBonus = GetTotalBonus(AttributeBonusType.CurrentHealth);
        float expectedCurrentHp = recalculated.maxHp * healthRatio + currentHpBonus;
        characterCore.currentHp = Mathf.Clamp(expectedCurrentHp, 0f, recalculated.maxHp);

        RefreshAttackDurability();
        RefreshHealthUi();
        SyncProfileData(effectiveWeaponType);
    }

    public void ClearAllBonus()
    {
        temporaryBonuses.Clear();
        ApplyAllBonus();
    }

    private void HandleRuntimeStateChanged()
    {
        RebuildPermanentBonuses();
        ApplyAllBonus();
    }

    private void RebuildPermanentBonuses()
    {
        permanentBonuses.Clear();

        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        foreach (BuildingRewardDefinition reward in runtimeState.GetGrantedRewards())
        {
            if (reward == null)
            {
                continue;
            }

            AddToBonusMap(permanentBonuses, reward.bonusType, reward.bonusValue);
            AddToBonusMap(permanentBonuses, reward.subBonusType, reward.subBonusValue);
        }
    }

    private void RebuildBackpackBonuses()
    {
        backpackBonuses.Clear();

        BackpackMananger backpack = BackpackMananger.Instance;
        if (backpack == null || backpack.backpackItems == null)
        {
            return;
        }

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

            AddToBonusMap(backpackBonuses, item.bonusType, item.bonusValue);
            AddToBonusMap(backpackBonuses, item.subBonusType, item.subBonusValue);
        }
    }

    private void RefreshAttackDurability()
    {
        float durabilityBonus = GetTotalBonus(AttributeBonusType.Durability);
        float durabilityBase = 100f;

        if (playerAttack != null)
        {
            playerAttack.baseMaxInk = Mathf.Max(playerAttack.baseMaxInk, 100f);
            durabilityBase = playerAttack.baseMaxInk;
            playerAttack.maxInk = Mathf.Max(1f, playerAttack.baseMaxInk + durabilityBonus);
            playerAttack.ink = Mathf.Clamp(playerAttack.ink, 0f, playerAttack.maxInk);
            playerAttack.RefreshInkUI();
        }

        if (profileData != null)
        {
            profileData.maxDurability = Mathf.Max(1f, durabilityBase + durabilityBonus);
            profileData.currentDurability = Mathf.Clamp(profileData.currentDurability, 0f, profileData.maxDurability);
        }
    }

    private void RefreshHealthUi()
    {
        if (playerTakeDamage != null && playerTakeDamage.healthTrans != null && characterCore != null)
        {
            playerTakeDamage.healthTrans.SetMaxValue(characterCore.stats.maxHp);
            playerTakeDamage.healthTrans.SetValue(characterCore.currentHp);
            GameplayStatusHudRuntime.RefreshHealthText(characterCore.currentHp, characterCore.stats.maxHp);
        }
    }

    private void SyncProfileData(WeaponType effectiveWeaponType)
    {
        if (profileData == null)
        {
            profileData = FindObjectOfType<PlayerProfileData>();
        }

        if (profileData == null || characterCore == null)
        {
            return;
        }

        if (playerAttack != null)
        {
            profileData.maxDurability = playerAttack.maxInk;
            profileData.currentDurability = playerAttack.ink;
        }

        profileData.SyncSelectedLoadoutFromRuntime();
        profileData.SetEffectiveWeapon(effectiveWeaponType);
    }

    private void ResolveReferences()
    {
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }

        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        if (playerTakeDamage == null)
        {
            playerTakeDamage = GetComponent<PlayerTakeDamage>();
        }

        if (profileData == null)
        {
            profileData = GetComponent<PlayerProfileData>();
        }
    }

    private void EnsureDesignBaseline()
    {
        if (characterCore == null)
        {
            return;
        }

        if (characterCore.baseStats == null)
        {
            characterCore.baseStats = characterCore.stats != null ? characterCore.stats.Clone() : new CharacterStats();
        }

        InkTypeDefinition inkDefinition = InkTypeCatalog.Get(PlayerLoadoutRuntime.CurrentWeaponType);
        if (characterCore.baseStats.attackDamage < inkDefinition.baseDamage)
        {
            characterCore.baseStats.attackDamage = inkDefinition.baseDamage;
        }

        if (playerAttack != null)
        {
            playerAttack.baseMaxInk = Mathf.Max(playerAttack.baseMaxInk, 100f);
            if (playerAttack.maxInk <= 0f)
            {
                playerAttack.maxInk = playerAttack.baseMaxInk;
            }

            if (playerAttack.ink <= 0f)
            {
                playerAttack.ink = playerAttack.maxInk;
            }
        }
    }

    private float GetTotalBonus(AttributeBonusType type)
    {
        if (type == AttributeBonusType.None)
        {
            return 0f;
        }

        float total = 0f;
        if (temporaryBonuses.TryGetValue(type, out float temporaryValue))
        {
            total += temporaryValue;
        }

        if (backpackBonuses.TryGetValue(type, out float backpackValue))
        {
            total += backpackValue;
        }

        if (permanentBonuses.TryGetValue(type, out float permanentValue))
        {
            total += permanentValue;
        }

        return total;
    }

    private static void AddToBonusMap(Dictionary<AttributeBonusType, float> target, AttributeBonusType type, float value)
    {
        if (target == null || type == AttributeBonusType.None || Mathf.Approximately(value, 0f))
        {
            return;
        }

        if (target.TryGetValue(type, out float currentValue))
        {
            float nextValue = currentValue + value;
            if (Mathf.Approximately(nextValue, 0f))
            {
                target.Remove(type);
            }
            else
            {
                target[type] = nextValue;
            }
        }
        else
        {
            target[type] = value;
        }
    }
}
