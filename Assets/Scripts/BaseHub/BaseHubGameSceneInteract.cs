using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseHubGameSceneInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private string gameSceneName = "GameScene";

    public string InteractionTip => "进入关卡";

    public void OnInteract()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ToGame();
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
