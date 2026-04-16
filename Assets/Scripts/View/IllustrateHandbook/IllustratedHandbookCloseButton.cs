using UnityEngine;

public class IllustratedHandbookCloseButton : MonoBehaviour
{
    [Header("Í¼¼øÖ÷Ò³")]
    public GameObject illustratedHandbookPanel;

    [Header("ÏêÏ¸ÐÅÏ¢Ò³")]
    public GameObject detailedInformationPanel;

    public void CloseHandbook()
    {
        if (illustratedHandbookPanel != null)
            illustratedHandbookPanel.SetActive(false);

        if (detailedInformationPanel != null)
            detailedInformationPanel.SetActive(false);

        UIManager.Instance?.RestoreUI();
    }
}