using UnityEngine;

public class BookInteract : MonoBehaviour, IInteractable
{
    [Header("图鉴界面")]
    public GameObject illustratedHandbook;

    public string InteractionTip => "打开图鉴";

    public void OnInteract()
    {
        Debug.Log("书本被交互！");  // 调试日志

        if (illustratedHandbook != null)
        {
            UIManager.Instance?.OpenIllustratedHandbook();
        }
        else
        {
            Debug.LogError("图鉴界面未赋值！");
        }
    }
}