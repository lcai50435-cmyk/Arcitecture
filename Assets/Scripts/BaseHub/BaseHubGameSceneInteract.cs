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
        if (TryOpenLevelSelectionScene())
        {
            return;
        }

        if (uiController == null)
        {
            uiController = FindObjectOfType<BaseHubUIController>();
        }

        if (uiController != null)
        {
            uiController.OpenStageSelectionPanel(RuntimeModalOpenSource.Interact);
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

    private static bool TryOpenLevelSelectionScene()
    {
        if (!LevelSelectionSceneController.CanLoadScene())
        {
            return false;
        }

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            LevelSelectionSceneController.CaptureBaseReturnPositionFromCurrentPlayer();
            LevelSelectionSceneController.CaptureBackdropBeforeSceneLoad();
            loader.ToScene(LevelSelectionSceneController.SceneName);
            return true;
        }

        LevelSelectionSceneController.CaptureBaseReturnPositionFromCurrentPlayer();
        LevelSelectionSceneController.CaptureBackdropBeforeSceneLoad();
        SceneManager.LoadScene(LevelSelectionSceneController.SceneName);
        return true;
    }
}
