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
    
    private void Awake()
    {
        core = GetComponent<CharacterCore>();

        moveSpeed = core.stats.moveSpeed;
    }

    void Update()
    {
        // 若脚本禁用则直接不处理
        if (!enabled) return;

        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputY = Input.GetAxisRaw("Vertical");

        // 人物强制四方向行走
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            // 同时按了两个键 则 清空一个轴，强制四方向
            inputX = 0;
        }

        // 判断是否移动
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        if (isMoving)
        {
            lastInputX = inputX;
            lastInputY = inputY;
        }


        // 每一帧都更新动画
        animator.SetFloat("InputX", lastInputX);
        animator.SetFloat("InputY", lastInputY);
        animator.SetBool("IsMoving", isMoving);

        // 移动
        rb.velocity = new Vector2(inputX, inputY) * moveSpeed;
    }
}