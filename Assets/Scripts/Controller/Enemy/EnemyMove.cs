using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

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

    // 记录最后一次有效移动方向（用于动画过渡）
    private float lastInputX;
    private float lastInputY;

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
    /// <param name="moveDir">归一化的移动方向（仅上下左右，无斜向）</param>
    public void SetMoveDirection(Vector2 moveDir)
    {
        // 过滤斜向输入
        float inputX = moveDir.x;
        float inputY = moveDir.y;
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            inputX = 0; // 斜向时优先清空X轴
        }

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
        rb.velocity = new Vector2(inputX, inputY) * moveSpeed;
    }
}
