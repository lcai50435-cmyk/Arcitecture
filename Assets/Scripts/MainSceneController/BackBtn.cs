using UnityEngine;
using UnityEngine.UI;

public class BackBtn : MonoBehaviour
{
    public Button yourButton;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("返回按钮点击成功！");

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("SceneLoader 单例不存在！");
            return;
        }

        // 调用返回菜单
        SceneLoader.Instance.ToMenu();
    }
}