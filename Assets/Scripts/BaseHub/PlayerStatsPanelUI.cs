using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsPanelUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI durabilityText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI defenseText;

    private CharacterCore characterCore;
    private PlayerProfileData profileData;

    public void Configure(
        Image avatar,
        TextMeshProUGUI health,
        TextMeshProUGUI maxHealth,
        TextMeshProUGUI durability,
        TextMeshProUGUI attack,
        TextMeshProUGUI moveSpeed,
        TextMeshProUGUI defense)
    {
        avatarImage = avatar;
        healthText = health;
        maxHealthText = maxHealth;
        durabilityText = durability;
        attackText = attack;
        moveSpeedText = moveSpeed;
        defenseText = defense;
    }

    public void Bind(CharacterCore core, PlayerProfileData profile)
    {
        characterCore = core;
        profileData = profile;
        Refresh();
    }

    public void Refresh()
    {
        if (characterCore == null || characterCore.stats == null || profileData == null) return;

        if (avatarImage != null)
        {
            avatarImage.sprite = profileData.avatar;
            avatarImage.enabled = profileData.avatar != null;
        }

        SetText(healthText, $"生命：{characterCore.currentHp:0}");
        SetText(maxHealthText, $"生命上限：{characterCore.stats.maxHp:0}");
        SetText(durabilityText, $"耐久：{profileData.currentDurability:0}/{profileData.maxDurability:0}");
        SetText(attackText, $"攻击力：{characterCore.stats.attackDamage:0}");
        SetText(moveSpeedText, $"移动速度：{characterCore.stats.moveSpeed:0.0}");
        SetText(defenseText, $"防御力：{characterCore.stats.defense:0}");
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }
}
