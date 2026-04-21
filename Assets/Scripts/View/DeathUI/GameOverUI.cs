using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game Over 界面按钮控制
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("按钮")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("场景名")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainScene";

    private void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }
    }

    public void RestartGame()
    {
        ResetRuntimeState();
        GameplayStageRuntime.EnsureSelectedStageUnlocked();
        string targetSceneName = GameplayStageRuntime.GetSelectedSceneName();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(string.IsNullOrWhiteSpace(targetSceneName) ? gameSceneName : targetSceneName);
            return;
        }

        SceneManager.LoadScene(string.IsNullOrWhiteSpace(targetSceneName) ? gameSceneName : targetSceneName);
    }

    public void GoToMainMenu()
    {
        ResetRuntimeState();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(mainMenuSceneName);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void ResetRuntimeState()
    {
        Time.timeScale = 1f;
        RuntimeCollectedCrystalRegistry.EnsureInstance().Clear();
        BackpackMananger.Instance?.ClearAllItems();
    }
}
