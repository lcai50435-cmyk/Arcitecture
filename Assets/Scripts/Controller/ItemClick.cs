using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鼠标点击触发
/// </summary>
public class ItemClick : MonoBehaviour
{
    // 物品被点击时，这里会自动执行
    private void OnMouseDown()
    {
        Debug.Log("你点击了物品：" + gameObject.name);

        // 我想做的事    
    }
}
