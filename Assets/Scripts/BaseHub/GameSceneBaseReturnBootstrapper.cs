using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        GameObject canvasObject = new GameObject("GameSceneBaseReturnCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        Button returnButton = CreateReturnButton(canvasObject.transform);
        returnButton.onClick.AddListener(ReturnToBaseScene);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Button CreateReturnButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ReturnBaseButton", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-36f, -36f);
        rect.sizeDelta = new Vector2(160f, 54f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.08f, 0.05f, 0.88f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "返回基地";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.86f, 0.62f, 1f);

        return button;
    }

    private static void ReturnToBaseScene()
    {
        Time.timeScale = 1f;

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ToBase();
            return;
        }

        SceneManager.LoadScene(BaseSceneName);
    }
}
