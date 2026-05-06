using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatagloueAreaTrigger2D : MonoBehaviour
{
    // Only detect the player
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameCountDownManager.Instance != null)
            {
                GameCountDownManager.Instance.SetInBaseState(true);
            }
            Debug.Log("进入基地 → 倒计时暂停");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameCountDownManager.Instance != null)
            {
                GameCountDownManager.Instance.SetInBaseState(false);
            }
            Debug.Log("离开基地 → 倒计时开始");
        }
    }
}
