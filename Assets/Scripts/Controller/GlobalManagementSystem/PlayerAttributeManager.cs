using UnityEngine;

public class PlayerAttributeManager : MonoBehaviour
{
    public static PlayerAttributeManager Instance;

    public CharacterCore characterCore;
    public PlayerAttack playerAttack;
    public PlayerTakeDamage playerTakeDamage;

    private float baseMoveSpeed;
    private float baseAttackDamage;
    private float baseDefense;
    private float baseInk;

    private float bonusHp;
    private float bonusMoveSpeed;
    private float bonusAttackDamage;
    private float bonusDefense;
    private float bonusInk;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        baseMoveSpeed = characterCore.stats.moveSpeed;
        baseAttackDamage = characterCore.stats.attackDamage;
        baseDefense = characterCore.stats.defense;
        baseInk = playerAttack.ink;
        Refresh();
    }

    public void AddBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        Add(type, value);
        Add(subType, subValue);
        Refresh();
    }

    public void RemoveBonus(AttributeBonusType type, float value, AttributeBonusType subType, float subValue)
    {
        Add(type, -value);
        Add(subType, -subValue);
        Refresh();
    }

    private void Add(AttributeBonusType t, float v)
    {
        switch (t)
        {
            case AttributeBonusType.CurrentHealth: bonusHp += v; break;
            case AttributeBonusType.MoveSpeed: bonusMoveSpeed += v; break;
            case AttributeBonusType.AttackPower: bonusAttackDamage += v; break;
            case AttributeBonusType.Defense: bonusDefense += v; break;
            case AttributeBonusType.Durability: bonusInk += v; break;
        }
    }

    private void Refresh()
    {
        // HP
        float hp = characterCore.currentHp + bonusHp;
        characterCore.currentHp = Mathf.Clamp(hp, 0f, 100f);
        playerTakeDamage.healthTrans.SetValue(characterCore.currentHp);

        // 移动速度
        characterCore.stats.moveSpeed = Mathf.Max(baseMoveSpeed + bonusMoveSpeed, 1f);

        // 攻击
        characterCore.stats.attackDamage = Mathf.Max(baseAttackDamage + bonusAttackDamage, 1f);

        // 防御
        characterCore.stats.defense = Mathf.Max(baseDefense + bonusDefense, 0f);

        // 墨水
        playerAttack.ink = Mathf.Max(baseInk + bonusInk, 0f);
        playerAttack.weaponTrans.SetValue(playerAttack.ink);
    }

    public void ClearAllBonus()
    {
        bonusHp = 0;
        bonusMoveSpeed = 0;
        bonusAttackDamage = 0;
        bonusDefense = 0;
        bonusInk = 0;
        Refresh();
    }
}