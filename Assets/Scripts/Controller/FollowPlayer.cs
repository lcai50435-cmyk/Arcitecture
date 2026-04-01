using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player; // 拖入你的 Player

    // Update is called once per frame
    void Update()
    {        
        // 在玩家头顶 1 米的位置跟随
        transform.position = player.position + new Vector3(4, -2, 0);
        transform.rotation = player.rotation;
    }
}
