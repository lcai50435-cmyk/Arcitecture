using TMPro;
using UnityEngine;

/// <summary>
/// 拾取普通结构后的轻量提示。
/// </summary>
public class CrystalDescriptionUI : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;

    private BackpackMananger backpack;
    private bool subscribed;

    private void Start()
    {
        gameObject.SetActive(false);
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    /// <summary>
    /// 展示拾取反馈
    /// </summary>
    private void ShowDescription(ArchitecturalCrystal crystal)
    {
        if (!crystal.IsCommonStructure)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        string desc = InkModifierRuntimeConfig.BuildCrystalActivationText(
            crystal,
            BackpackMananger.Instance,
            RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance));

        if (string.IsNullOrEmpty(desc))
        {
            desc = string.IsNullOrEmpty(crystal.textDescription)
                ? $"{crystal.DisplayName} 已生效"
                : crystal.textDescription;
        }

        descriptionText.text = desc;
        gameObject.SetActive(true);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), 4f);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backpack != null)
        {
            backpack.OnItemPicked -= ShowDescription;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed)
        {
            return;
        }

        backpack = BackpackMananger.Instance;
        if (backpack == null)
        {
            return;
        }

        backpack.OnItemPicked += ShowDescription;
        subscribed = true;
    }
}
