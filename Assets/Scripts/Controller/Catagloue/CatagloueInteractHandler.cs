using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatagloueInteractHandler : MonoBehaviour, IInteractable
{
    public void OnInteract()
    {
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();
        if (player == null)
        {
            Debug.LogWarning("未找到 PlayerGetArchitectural，无法提交图鉴");
            return;
        }

        player.SubmitAllCachedExp();
    }

    public string InteractionTip => "提交图鉴";
}
