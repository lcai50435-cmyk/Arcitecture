using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家捡建筑结构物品
/// </summary>
public class PlayerGetArchitectural : MonoBehaviour
{
    private BackpackMananger _backpack;
    private BackpackUI _backpackUI;

    private void Start()
    {
        if (BackpackMananger.Instance == null)
        {
            Debug.LogError("背包管理器不存在！请放到场景里");
            return;
        }
        _backpack = BackpackMananger.Instance;
        _backpackUI = FindObjectOfType<BackpackUI>();
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="crystal"></param>
    public void PickCrystal(ArchitecturalCrystal crystal)
    {
        // 调用背包的拾取方法，成功则刷新UI
        if (_backpack.PickItem(crystal))
        {
            _backpackUI.RefreshUI();
        }
    }

    /// <summary>
    /// 提交所有构建度给基地
    /// </summary>
    public void SubmitAllCachedExp()
    {
        if (_backpack.backpackItems.Count == 0)
        {
            Debug.Log("背包为空，无需上交！");
            return;
        }
        // 遍历背包物品，添加经验
        foreach (var item in _backpack.backpackItems)
        {
            ExperienceManager.Instance.AddExperience(item.type, item.expValue);
        }
        // 清空背包并刷新UI
        _backpack.ClearAllItems();
        _backpackUI.RefreshUI();
    }
}
   
