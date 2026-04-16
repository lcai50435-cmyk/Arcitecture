using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 人物移动
/// </summary>
public class PlayerMove : MonoBehaviour
{
    
    public Rigidbody2D rb;
    public Animator animator;
    
    protected CharacterCore core;

    // 记住最后一次的方向
    private float lastInputX;
    private float lastInputY;

    private float moveSpeed; // 速度

    [HideInInspector] public bool canMove = true;

    // 挂载朝向跟踪组件
    private DirectionTracker directionTracker;

    private void Awake()
    {
        core = GetComponent<CharacterCore>();

        // 获取朝向跟踪组件
        directionTracker = GetComponent<DirectionTracker>();
    }

    void Update()
    {
        // 若脚本禁用则直接不处理
        // if (!enabled) return;

        moveSpeed = core.stats.moveSpeed;

        if (!canMove) return;

        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputY = Input.GetAxisRaw("Vertical");

        // 人物强制四方向行走
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            // 同时按了两个键 则 清空一个轴，强制四方向
            inputX = 0;
        }

        Vector2 currentMoveDir = new Vector2(inputX, inputY);
        // 判断是否移动
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // 关键：更新朝向到工具类
        if (isMoving)
        {
            directionTracker.UpdateMoveDirection(currentMoveDir);
        }

        // 从工具类获取最后朝向，更新动画
        Vector2 lastDir = directionTracker.LastDirection;
        animator.SetFloat("InputX", lastDir.x);
        animator.SetFloat("InputY", lastDir.y);
        animator.SetBool("IsMoving", isMoving);

        // 移动
        rb.velocity = new Vector2(inputX, inputY) * moveSpeed;
    }
}