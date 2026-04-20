using UnityEngine;

public class PlayerAttributeManager : MonoBehaviour
{
    public static PlayerAttributeManager Instance;

    // 角色属性
    public CharacterCore characterCore;

    private float bonusMaxHp = 0f;
    private float bonusMoveSpeed = 0f;
    private float bonusAttackDamage = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 捡起道具则加总加成
    public void AddBonus(AttributeBonusType type, float value)
    {
        switch (type)
        {
            case AttributeBonusType.MaxHealth: bonusMaxHp += value; break;
            case AttributeBonusType.MoveSpeed: bonusMoveSpeed += value; break;
            case AttributeBonusType.AttackPower: bonusAttackDamage += value; break;
        }
        ApplyAllBonus(); // 把总加成应用到角色
    }

    // 丢弃或者上交则减总加成
    public void RemoveBonus(AttributeBonusType type, float value)
    {
        switch (type)
        {
            case AttributeBonusType.MaxHealth: bonusMaxHp -= value; break;
            case AttributeBonusType.MoveSpeed: bonusMoveSpeed -= value; break;
            case AttributeBonusType.AttackPower: bonusAttackDamage -= value; break;
        }
        ApplyAllBonus(); // 把总加成应用到角色
    }

    // 一次性应用总加成到角色
    public void ApplyAllBonus()
    {
        characterCore.stats.maxHp = characterCore.stats.maxHp + bonusMaxHp;
        characterCore.stats.moveSpeed = characterCore.stats.moveSpeed + bonusMoveSpeed;
        characterCore.stats.attackDamage = characterCore.stats.attackDamage + bonusAttackDamage;

        ClearAllBonus();
    }

    // 清空总加成 // 下一次从零开始
    public void ClearAllBonus()
    {
        bonusMaxHp = 0;
        bonusMoveSpeed = 0;
        bonusAttackDamage = 0;
        // ApplyAllBonus();
    }
}