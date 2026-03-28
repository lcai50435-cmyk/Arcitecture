using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交互接口
/// </summary>
public interface IInteractable 
{
    string InteractionTip { get; }  // 物品交互信息提示

    void OnInteract(); // 物品被交互时执行
}
