using TMPro;
using UnityEngine;

/// <summary>
/// 交互判断
/// 对可交互物体进行判断是否进入范围
/// 获取交互对象
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("交互提示UI")]
    public GameObject interactTipUI; // "按F交互"提示面板/文本
    [Header("UI 文本")]
    public TextMeshProUGUI interactTipText;  // 交互信息提示

    private IInteractable currentInteractable; // 获取可交互物体

    void Awake()
    {
        // 初始隐藏提示
        if (interactTipUI != null)
            interactTipUI.SetActive(false);
    }


    void Update()
    {
        // 检测交互按键
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    // 进入交互范围就显示提示
    private void OnTriggerEnter2D(Collider2D col) 
    {
        if (col.TryGetComponent(out IInteractable interactable)) // 获取碰撞对象中的接口组件 
        {
            currentInteractable = interactable;

            // 获得物品信息
            string tip = interactable.InteractionTip;
            interactTipText.text = tip;

            // 显示"按F交互"提示
            if (interactTipUI != null)
                interactTipUI.SetActive(true);
        }
    }

    // 离开交互范围就隐藏提示
    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.TryGetComponent(out IInteractable interactable))
        {          
            if (interactable == currentInteractable) // 若离开当前目标
            {                              
                currentInteractable = null;   // 清空+隐藏
                
                interactTipText.text = null;  // 清空文本
                if (interactTipUI != null)
                    interactTipUI.SetActive(false);
            }
        }
    }

    // 触发交互
    private void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInteract();

            // 交互后自动隐藏提示
            if (interactTipUI != null)
                interactTipUI.SetActive(false);
        }
    }
}