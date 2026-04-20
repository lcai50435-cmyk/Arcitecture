using UnityEngine;

public class UnlockSlotDescriptionDataSmall : MonoBehaviour
{
    [Header("小图标名称")]
    public string slotName;

    [Header("介绍文本")]
    [TextArea(2, 6)]
    public string description;
}