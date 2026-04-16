using UnityEngine;

public class StoneMonsterDeath : EnemyDeathBase
{
    private Rigidbody2D rb;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
    }

    protected override void OnEnemyDie()
    {
        // 清空刚体速度 + 锁物理
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // 冻结所有移动 关键！
            rb.bodyType = RigidbodyType2D.Static;
        }

        // 关闭碰撞体 不再触发伤害/碰撞
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
      
    }

    public void OnDestory()
    {
        // 触发你动画机的死亡Trigger
        anim.SetTrigger("IsDeath");
    }
}