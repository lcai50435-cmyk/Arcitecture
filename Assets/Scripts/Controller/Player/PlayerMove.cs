using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 人物移动
/// </summary>
public class PlayerMove : MonoBehaviour
{
    public float speed = 5;
    public Rigidbody2D rb;
    public Animator animator;

    // 记住最后一次的方向
    private float lastInputX;
    private float lastInputY;
 
    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputY = Input.GetAxisRaw("Vertical");

        // 人物强制四方向行走
        if (Mathf.Abs(inputX) > 0.1f && Mathf.Abs(inputY) > 0.1f)
        {
            // 同时按了两个键 → 清空一个轴，强制四方向
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
        rb.velocity = new Vector2(inputX, inputY) * speed;

       
    }
   
}