using UnityEngine;
public class ChaseSensor : MonoBehaviour
{
    EnemyStateManager state;
    EnemyChase enemyChase;

    void Awake()
    {
        state = GetComponentInParent<EnemyStateManager>();
        enemyChase = GetComponentInParent<EnemyChase>();
    }

    // 玩家进入追逐范围：设置状态为chase
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("ChaseSensor触发了！检测到对象：" + other.name, gameObject);
        if (other.CompareTag("Player"))
        {
            // Debug.Log("检测到玩家，设置isChase=true", gameObject);
            enemyChase.StartChase();
        }
    }

    // 玩家进入追逐范围：退出状态chase
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            enemyChase.StopChase();
    }
}