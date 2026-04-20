using UnityEngine;

public class CrackDamage : MonoBehaviour
{
    private float damage = 20; // 攻击伤害


    // 因为碰撞矩阵已经过滤，这里进来的一定是玩家
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只认玩家标签！敌人永远不会触发！
        if (!other.CompareTag("Player")) return;

        // 直接拿玩家脚本造成伤害
        CharacterCore player = other.GetComponent<CharacterCore>();

        if (other.CompareTag("Player")) 
        {
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }
       

    }
}