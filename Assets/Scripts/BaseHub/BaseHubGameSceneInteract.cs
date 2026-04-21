using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseHubGameSceneInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private BaseHubUIController uiController;

    public string InteractionTip => "选择关卡";

    public void Configure(BaseHubUIController controller)
    {
        uiController = controller;
    }

    public void OnInteract()
    {
        if (uiController == null)
        {
            uiController = FindObjectOfType<BaseHubUIController>();
        }

        if (uiController != null)
        {
            uiController.OpenStageSelectionPanel();
            return;
        }

        GameplayStageRuntime.EnsureSelectedStageUnlocked();
        string sceneName = GameplayStageRuntime.GetSelectedSceneName();
        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
