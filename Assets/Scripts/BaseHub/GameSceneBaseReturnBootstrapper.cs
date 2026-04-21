using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameSceneBaseReturnBootstrapper : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string BaseSceneName = "BaseScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != GameSceneName) return;
        if (FindObjectOfType<GameSceneBaseReturnBootstrapper>() != null) return;

        GameObject bootstrapper = new GameObject("GameSceneBaseReturnUI");
        bootstrapper.AddComponent<GameSceneBaseReturnBootstrapper>().Build();
    }

    private void Build()
    {
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    public static bool IsGameSceneActive()
    {
        return SceneManager.GetActiveScene().name == GameSceneName;
    }

    public static void SubmitCatalogueAndReturnToBase()
    {
        SubmitBackpackToCatalogue();
        ReturnToBaseScene();
    }

    public static void ReturnToBaseScene()
    {
        Time.timeScale = 1f;

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(BaseSceneName);
            return;
        }

        SceneManager.LoadScene(BaseSceneName);
    }

    private static void SubmitBackpackToCatalogue()
    {
        BackpackMananger backpack = BackpackMananger.Instance;
        PlayerGetArchitectural player = FindObjectOfType<PlayerGetArchitectural>();

        if (backpack == null || player == null)
        {
            return;
        }

        int itemCount = backpack.GetOccupiedCount();
        if (itemCount <= 0)
        {
            return;
        }

        if (CatalogueUnlockSelectionManager.Instance != null)
        {
            CatalogueUnlockSelectionManager.Instance.AddUnlockCount(itemCount);
        }

        player.SubmitAllCachedExp();
    }
}
