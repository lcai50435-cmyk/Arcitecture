using UnityEngine;
using UnityEngine.UI;

public class StartGameBtn : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("开始游戏按钮点击成功！");
        SceneLoader.Instance.ToBase();
    }
}