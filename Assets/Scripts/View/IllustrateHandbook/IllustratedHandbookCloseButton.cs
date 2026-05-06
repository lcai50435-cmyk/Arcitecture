using UnityEngine;

public class IllustratedHandbookCloseButton : MonoBehaviour
{
    [Header("图鉴主页")]
    public GameObject illustratedHandbookPanel;

    [Header("详细信息页")]
    public GameObject detailedInformationPanel;

    public void CloseHandbook()
    {
        DetailedInformationUI detailUi = ResolveVisibleDetailUi();
        if (detailUi != null)
        {
            detailUi.CloseDetailOnlyReturnHandbook();
            return;
        }

        if (illustratedHandbookPanel != null)
            illustratedHandbookPanel.SetActive(false);

        if (detailedInformationPanel != null)
            detailedInformationPanel.SetActive(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseIllustratedHandbook();
            return;
        }

        IllustratedUISceneLoader.Close();
    }

    private DetailedInformationUI ResolveVisibleDetailUi()
    {
        DetailedInformationUI detailUi = null;

        if (detailedInformationPanel != null)
        {
            detailUi = detailedInformationPanel.GetComponent<DetailedInformationUI>() ??
                       detailedInformationPanel.GetComponentInChildren<DetailedInformationUI>(true);
            if (detailUi != null &&
                transform.IsChildOf(detailedInformationPanel.transform))
            {
                return detailUi;
            }
        }

        detailUi = GetComponentInParent<DetailedInformationUI>(true);
        if (detailUi != null &&
            (detailUi.IsDetailVisible() ||
             transform.IsChildOf(GetDetailRootTransform(detailUi))))
        {
            return detailUi;
        }

        return null;
    }

    private static Transform GetDetailRootTransform(DetailedInformationUI detailUi)
    {
        return detailUi != null && detailUi.detailedInformationPanel != null
            ? detailUi.detailedInformationPanel.transform
            : detailUi != null
                ? detailUi.transform
                : null;
    }
}
