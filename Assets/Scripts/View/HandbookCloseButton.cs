using UnityEngine;

public class HandbookCloseButton : MonoBehaviour
{
    public void CloseHandbook()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseIllustratedHandbook();
            return;
        }

        IllustratedUISceneLoader.Close();
    }
}
