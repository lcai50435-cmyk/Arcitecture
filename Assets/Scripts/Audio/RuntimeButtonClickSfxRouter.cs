using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimeButtonClickSfxRouter : MonoBehaviour
{
    private const float RescanInterval = 0.45f;

    private static RuntimeButtonClickSfxRouter instance;

    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static RuntimeButtonClickSfxRouter EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        RuntimeButtonClickSfxRouter existing = FindObjectOfType<RuntimeButtonClickSfxRouter>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject routerObject = new GameObject(nameof(RuntimeButtonClickSfxRouter));
        instance = routerObject.AddComponent<RuntimeButtonClickSfxRouter>();
        return instance;
    }

    public static void Register(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (button.GetComponent<RuntimeButtonClickSfxEmitter>() == null)
        {
            button.gameObject.AddComponent<RuntimeButtonClickSfxEmitter>();
        }
    }

    public static void PlayClick()
    {
        MusicManager.PlayButtonClickSfx();
    }

    public static bool ShouldPlayClickForButton(Button button)
    {
        return button != null &&
               button.IsInteractable() &&
               button.gameObject.activeInHierarchy;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ScanButtons();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + RescanInterval;
        ScanButtons();
    }

    private void OnDisable()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextScanTime = 0f;
        ScanButtons();
    }

    private static void ScanButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Register(buttons[i]);
        }
    }
}
