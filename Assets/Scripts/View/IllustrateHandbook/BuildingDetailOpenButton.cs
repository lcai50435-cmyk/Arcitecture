using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingDetailOpenButton : MonoBehaviour
{
    [Header("当前建筑是否解锁")]
    public CatalogueBuildingUnlockState buildingUnlockState;

    [Header("当前建筑的数据")]
    public BuildingDetailData buildingDetailData;

    [Header("详细信息界面控制器")]
    public DetailedInformationUI detailedInformationUI;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OpenDetail);
        }

        RefreshClickable();
    }

    private void Update()
    {
        RefreshClickable();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OpenDetail);
        }
    }

    private void RefreshClickable()
    {
        if (button == null) return;

        button.interactable = buildingUnlockState != null && buildingUnlockState.isBuildingUnlocked;
    }

    private void OpenDetail()
    {
        if (buildingUnlockState == null || !buildingUnlockState.isBuildingUnlocked)
        {
            return;
        }

        if (detailedInformationUI == null)
        {
            Debug.LogError("DetailedInformationUI 未绑定");
            return;
        }

        detailedInformationUI.ShowDetail(buildingDetailData);
    }
}