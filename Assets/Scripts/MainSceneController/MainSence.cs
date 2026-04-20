using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏主页面场景效果
/// </summary>
public class MainSence : MonoBehaviour
{
    public float offsetMultipliter = 1f; // 偏移乘数
    public float smoothTime = 0.3f; // 平滑时间

    private Vector3 startPosition; // 开始位置
    private Vector3 velocity; // 速度

    void Start()
    {
        startPosition = transform.position; //获取当前脚本挂载对象的变换的组件位置
    }

    void Update()
    {
        //Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);// 从屏幕空间转化成视口空间
        ////
        //transform.position = Vector3.SmoothDamp(transform.position, startPosition + (offset * offsetMultipliter) ,ref velocity , smoothTime);
       
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        Vector3 targetPosition = startPosition + (Vector3)(offset * offsetMultipliter);
        targetPosition.z = transform.position.z;   // 保持原来的 Z 值

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

    }
}
