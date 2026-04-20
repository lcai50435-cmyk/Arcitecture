using UnityEngine;

public class BuildingDetailData : MonoBehaviour
{
    [Header("建筑名称")]
    public string buildingName;

    [Header("第一页图片")]
    public Sprite detailSprite1;

    [Header("第二页图片")]
    public Sprite detailSprite2;

    [Header("第一页介绍")]
    [TextArea(2, 6)]
    public string introduction1;

    [Header("第二页介绍")]
    [TextArea(2, 6)]
    public string introduction2;

    [Header("第二页最终介绍")]
    [TextArea(2, 8)]
    public string finalIntroduction;
}