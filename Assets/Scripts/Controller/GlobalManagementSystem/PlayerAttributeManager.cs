using UnityEngine;

public class PlayerAttributeManager : MonoBehaviour
{
    public static PlayerAttributeManager Instance;

    // 角色属性
    public CharacterCore characterCore;
    public PlayerAttack playerAttack;
    public PlayerTakeDamage playerTakeDamage;

    // 主属性
    private float bonusCurrentHp = 0f;
    private float bonusMoveSpeed = 0f;
    private float bonusAttackDamage = 0f;
    private float bonusDefense = 0f;
    private float bonusDurability = 0f;

    // 副属性
    private float subBonusCurrentHp = 0f;
    private float subBonusMoveSpeed = 0f;
    private float subBonusAttackDamage = 0f;
    private float subBonusDefense = 0f;
    private float subBonusDurability = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 捡起道具则加总加成
    public void AddBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        // 主属性加成
        switch (type)
        {
            case AttributeBonusType.CurrentHealth: bonusCurrentHp += value; break;
            case AttributeBonusType.MoveSpeed: bonusMoveSpeed += value; break;
            case AttributeBonusType.AttackPower: bonusAttackDamage += value; break;
            case AttributeBonusType.Defense: bonusDefense += value; break;
            case AttributeBonusType.Durability: bonusDurability += value; break;
        }
        // 副属性加成
        switch (subType)
        {
            case AttributeBonusType.CurrentHealth: subBonusCurrentHp += subValue; break;
            case AttributeBonusType.MoveSpeed: subBonusMoveSpeed += subValue; break;
            case AttributeBonusType.AttackPower: subBonusAttackDamage += subValue; break;
            case AttributeBonusType.Defense: subBonusDefense += subValue; break;
            case AttributeBonusType.Durability: subBonusDurability += subValue; break;
        }

        ApplyAllBonus(); // 把总加成应用到角色
    }

    // 丢弃或者上交则减总加成
    public void RemoveBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        // 主属性扣除
        switch (type)
        {
            case AttributeBonusType.CurrentHealth: bonusCurrentHp -= value; break;
            case AttributeBonusType.MoveSpeed: bonusMoveSpeed -= value; break;
            case AttributeBonusType.AttackPower: bonusAttackDamage -= value; break;
            case AttributeBonusType.Defense: bonusDefense -= value; break;
            case AttributeBonusType.Durability: bonusDurability -= value; break;
        }
        // 副属性扣除
        switch (subType)
        {
            case AttributeBonusType.CurrentHealth: subBonusCurrentHp -= subValue; break;
            case AttributeBonusType.MoveSpeed: subBonusMoveSpeed -= subValue; break;
            case AttributeBonusType.AttackPower: subBonusAttackDamage -= subValue; break;
            case AttributeBonusType.Defense: subBonusDefense -= subValue; break;
            case AttributeBonusType.Durability: subBonusDurability -= subValue; break;
        }

        ApplyAllBonus(); // 把总加成应用到角色
    }

    // 一次性应用总加成到角色
    public void ApplyAllBonus()
    {
        // 血量：0 ≤ currentHp ≤ 100
        float newHp = characterCore.currentHp + bonusCurrentHp + subBonusCurrentHp;
        characterCore.currentHp = Mathf.Clamp(newHp, 0f, 100f);
        // 刷新UI
        playerTakeDamage.healthTrans.SetValue(characterCore.currentHp);

        // 移动速度：≥1
        float newMoveSpeed = characterCore.stats.moveSpeed + bonusMoveSpeed + subBonusMoveSpeed;
        characterCore.stats.moveSpeed = Mathf.Max(newMoveSpeed, 1f);

        // 攻击力：≥1
        float newAttack = characterCore.stats.attackDamage + bonusAttackDamage + subBonusAttackDamage;
        characterCore.stats.attackDamage = Mathf.Max(newAttack, 1f);

        // 防御力：≥0
        float newDefense = characterCore.stats.defense + bonusDefense + subBonusDefense;
        characterCore.stats.defense = Mathf.Max(newDefense, 0f);

        // 耐久度（ink）：≥0
        float newDurability = playerAttack.ink + bonusDurability + subBonusDurability;
        playerAttack.ink = Mathf.Max(newDurability, 0f);
        // 刷新UI
        playerAttack.weaponTrans.SetValue(playerAttack.ink);

        ClearAllBonus();
    }

    // 清空总加成 // 下一次从零开始
    public void ClearAllBonus()
    {
        // 主属性清0
        bonusCurrentHp = 0;
        bonusMoveSpeed = 0;
        bonusAttackDamage = 0;
        bonusDefense = 0;
        bonusDurability = 0;

        // 副属性清0
        subBonusCurrentHp = 0;
        subBonusMoveSpeed = 0;
        subBonusAttackDamage = 0;
        subBonusDefense = 0;
        subBonusDurability = 0;
    }
}