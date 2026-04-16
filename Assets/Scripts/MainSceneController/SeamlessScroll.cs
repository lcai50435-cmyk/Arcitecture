using UnityEngine;

/// <summary>
/// 云朵移动逻辑
/// </summary>
public class SeamlessScroll : MonoBehaviour
{
    public float speed = 2f;           // 滚动速度（负数向左，正数向右）
    public bool horizontal = true;     // true=水平滚动，false=垂直

    private Transform bg1, bg2;
    private float size;
    private Vector3 startPos;

    void Start()
    {
        bg1 = transform.GetChild(0);
        bg2 = transform.GetChild(1);

        // 获取尺寸
        var sr = bg1.GetComponent<SpriteRenderer>();
        size = horizontal ? sr.bounds.size.x : sr.bounds.size.y;
        startPos = bg1.position;

        // 设置bg2位置
        bg2.position = startPos + (horizontal ? Vector3.right : Vector3.up) * size;
    }

    void Update()
    {
        Vector3 move = (horizontal ? Vector3.right : Vector3.up) * speed * Time.deltaTime;
        bg1.position += move;
        bg2.position += move;

        // 循环检测
        float dist = horizontal ? bg1.position.x - startPos.x : bg1.position.y - startPos.y;
        if (Mathf.Abs(dist) >= size)
            bg1.position = bg2.position - move.normalized * size;

        dist = horizontal ? bg2.position.x - startPos.x : bg2.position.y - startPos.y;
        if (Mathf.Abs(dist) >= size)
            bg2.position = bg1.position - move.normalized * size;
    }
}