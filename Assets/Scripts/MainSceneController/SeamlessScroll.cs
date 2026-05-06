using UnityEngine;

/// <summary>
/// Cloud movement logic
/// </summary>
public class SeamlessScroll : MonoBehaviour
{
    public float speed = 2f;           // Scroll speed (negative moves left, positive moves right)
    public bool horizontal = true;     // true = horizontal scrolling, false = vertical

    private Transform bg1, bg2;
    private float size;
    private Vector3 startPos;

    void Start()
    {
        bg1 = transform.GetChild(0);
        bg2 = transform.GetChild(1);

        // Get size
        var sr = bg1.GetComponent<SpriteRenderer>();
        size = horizontal ? sr.bounds.size.x : sr.bounds.size.y;
        startPos = bg1.position;

        // Set bg2 position
        bg2.position = startPos + (horizontal ? Vector3.right : Vector3.up) * size;
    }

    void Update()
    {
        Vector3 move = (horizontal ? Vector3.right : Vector3.up) * speed * Time.deltaTime;
        bg1.position += move;
        bg2.position += move;

        // Loop check
        float dist = horizontal ? bg1.position.x - startPos.x : bg1.position.y - startPos.y;
        if (Mathf.Abs(dist) >= size)
            bg1.position = bg2.position - move.normalized * size;

        dist = horizontal ? bg2.position.x - startPos.x : bg2.position.y - startPos.y;
        if (Mathf.Abs(dist) >= size)
            bg2.position = bg1.position - move.normalized * size;
    }
}
