using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// 敌人四方向移动核心脚本
/// 负责：四方向限制、面朝移动方向、基础移动
/// </summary>
public class EnemyMove : MonoBehaviour
{
    [Header("移动配置")]
    public float moveSpeed = 2f;          // 敌人移动速度（可与玩家不同）
    public Rigidbody2D rb;                // 敌人刚体组件
    public Animator animator;             // 敌人动画控制器

    // 记录最后一次有效移动方向
    private float lastInputX;
    private float lastInputY;

    [HideInInspector] public Vector2 moveDirection; // 当前移动方向
    [HideInInspector] public Vector2 targetPos;     // 目标位置（Patrol的currentTarget）

    private void Awake()
    {
        // 自动获取组件
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Debug.Log(this.transform.position);
    }

    /// <summary>
    /// 由巡逻逻辑调用 - 设置敌人移动方向
    /// </summary>
    /// <param name="moveDir">敌人与玩家的距离向量</param>
    public void SetMoveDirection(Vector2 moveDir)
    {

        Debug.Log($"[EnemyMove] 设置移动方向: {moveDir}");

        // 过滤斜向输入
        float inputX = moveDir.x;
        float inputY = moveDir.y;

        if (Mathf.Abs(inputX) > Mathf.Abs(inputY))
            inputY = 0;
        else
            inputX = 0;

        // 判断是否处于移动状态
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // 更新最后一次有效方向（用于停止时保持朝向动画）
        if (isMoving)
        {
            lastInputX = inputX;
            lastInputY = inputY;
        }

        // 同步动画参数
        animator.SetFloat("InputX", lastInputX);
        animator.SetFloat("InputY", lastInputY);
        animator.SetBool("IsMoving", isMoving);

        // 执行移动
        // 移动（匀速，不减速、不滑步）
        if (isMoving)
        {
            Vector2 dir = new Vector2(inputX, inputY).normalized;
            rb.velocity = dir * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
}
