using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalInteractHandler1 : MonoBehaviour, IInteractable
{
    public void OnInteract()
    {
        // 提交经验
        var player = FindObjectOfType<PlayerGetArchitectural>();
        player.SubmitAllCachedExp();
    }

    public string InteractionTip => "打开图鉴";
}
