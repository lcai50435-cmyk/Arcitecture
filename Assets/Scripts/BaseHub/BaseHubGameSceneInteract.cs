using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseHubGameSceneInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private string gameSceneName = "GameScene";

    public string InteractionTip => "进入关卡";

    public void OnInteract()
    {
        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(gameSceneName);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
