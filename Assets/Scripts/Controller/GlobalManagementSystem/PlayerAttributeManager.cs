using UnityEngine;

public class PlayerAttributeManager : MonoBehaviour
{
    public static PlayerAttributeManager Instance;

    public CharacterCore characterCore;
    public PlayerAttack playerAttack;
    public PlayerTakeDamage playerTakeDamage;

    private float bonusCurrentHp;
    private float bonusMaxHp;
    private float bonusMoveSpeed;
    private float bonusAttackDamage;
    private float bonusDefense;
    private float bonusDurability;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddBonus(AttributeBonusType type, float value)
    {
        Accumulate(type, value);
        ApplyAllBonus();
    }

    public void AddBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        Accumulate(type, value);
        Accumulate(subType, subValue);
        ApplyAllBonus();
    }

    public void RemoveBonus(AttributeBonusType type, float value)
    {
        Accumulate(type, -value);
        ApplyAllBonus();
    }

    public void RemoveBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        Accumulate(type, -value);
        Accumulate(subType, -subValue);
        ApplyAllBonus();
    }

    private void Accumulate(AttributeBonusType type, float value)
    {
        switch (type)
        {
            case AttributeBonusType.CurrentHealth:
                bonusCurrentHp += value;
                break;
            case AttributeBonusType.MaxHealth:
                bonusMaxHp += value;
                break;
            case AttributeBonusType.MoveSpeed:
                bonusMoveSpeed += value;
                break;
            case AttributeBonusType.AttackPower:
                bonusAttackDamage += value;
                break;
            case AttributeBonusType.Defense:
                bonusDefense += value;
                break;
            case AttributeBonusType.Durability:
                bonusDurability += value;
                break;
        }
    }

    public void ApplyAllBonus()
    {
        if (characterCore != null && characterCore.stats != null)
        {
            characterCore.stats.maxHp = Mathf.Max(1f, characterCore.stats.maxHp + bonusMaxHp);
            characterCore.currentHp = Mathf.Clamp(characterCore.currentHp + bonusCurrentHp, 0f, characterCore.stats.maxHp);
            characterCore.stats.moveSpeed = Mathf.Max(0f, characterCore.stats.moveSpeed + bonusMoveSpeed);
            characterCore.stats.attackDamage = Mathf.Max(0f, characterCore.stats.attackDamage + bonusAttackDamage);
            characterCore.stats.defense = Mathf.Max(0f, characterCore.stats.defense + bonusDefense);
        }

        if (playerAttack != null)
        {
            playerAttack.maxInk = Mathf.Max(0f, playerAttack.maxInk + bonusDurability);
            playerAttack.ink = Mathf.Clamp(playerAttack.ink, 0f, playerAttack.maxInk);

            if (playerAttack.weaponTrans != null)
            {
                playerAttack.weaponTrans.SetMaxValue(playerAttack.maxInk);
                playerAttack.weaponTrans.SetValue(playerAttack.ink);
                GameplayStatusHudRuntime.RefreshWeaponText(playerAttack.ink, playerAttack.maxInk);
            }
        }

        if (playerTakeDamage != null && playerTakeDamage.healthTrans != null && characterCore != null)
        {
            playerTakeDamage.healthTrans.SetMaxValue(characterCore.stats.maxHp);
            playerTakeDamage.healthTrans.SetValue(characterCore.currentHp);
            GameplayStatusHudRuntime.RefreshHealthText(characterCore.currentHp, characterCore.stats.maxHp);
        }

        ClearAllBonus();
    }

    public void ClearAllBonus()
    {
        bonusCurrentHp = 0f;
        bonusMaxHp = 0f;
        bonusMoveSpeed = 0f;
        bonusAttackDamage = 0f;
        bonusDefense = 0f;
        bonusDurability = 0f;
    }
}
