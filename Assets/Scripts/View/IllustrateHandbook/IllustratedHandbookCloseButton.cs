using UnityEngine;

public class IllustratedHandbookCloseButton : MonoBehaviour
{
    [Header("图鉴主页")]
    public GameObject illustratedHandbookPanel;

    [Header("详细信息页")]
    public GameObject detailedInformationPanel;

    public void CloseHandbook()
    {
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
}
