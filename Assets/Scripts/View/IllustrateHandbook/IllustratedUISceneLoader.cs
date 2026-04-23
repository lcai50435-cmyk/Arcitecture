using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class IllustratedUISceneLoader : MonoBehaviour
{
    private const string LoaderObjectName = "IllustratedUISceneLoader";
    private const string IllustratedUiSceneName = "IllustratedUIScene";
    private const string IllustratedUiScenePath = "Assets/Scenes/IllustratedUIScene.unity";

    private static readonly string[] HiddenCanvasNames =
    {
        "DialogCanvas",
        "PackBagCanvas",
        "InteractionCanvas",
        "DetailedInformationCanvas"
    };

    private static IllustratedUISceneLoader instance;

    private Coroutine loadRoutine;
    private OpenRequest pendingRequest;
    private string loadedForScenePath;

    private sealed class OpenRequest
    {
        public OpenRequest(
            RuntimeModalOpenSource source,
            IllustratedHandbookPage page,
            GameObject[] hideTargets,
            GameObject interactTip,
            GameObject playerObject)
        {
            Source = source;
            Page = page;
            HideTargets = hideTargets;
            InteractTip = interactTip;
            PlayerObject = playerObject;
        }

        public RuntimeModalOpenSource Source { get; }
        public IllustratedHandbookPage Page { get; }
        public GameObject[] HideTargets { get; }
        public GameObject InteractTip { get; }
        public GameObject PlayerObject { get; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static bool IsIllustratedUiScene(Scene scene)
    {
        return scene.IsValid() &&
               string.Equals(scene.name, IllustratedUiSceneName, StringComparison.Ordinal);
    }

    public static bool EnsureLoadedAsync()
    {
        if (!CanLoadScene())
        {
            return false;
        }

        EnsureInstance().QueueOpen(null);
        return true;
    }

    public static bool TryGetUIManager(out UIManager manager)
    {
        Scene scene = SceneManager.GetSceneByName(IllustratedUiSceneName);
        return TryResolveUiManager(scene, out manager);
    }

    public static bool Open(
        RuntimeModalOpenSource source,
        IllustratedHandbookPage page,
        GameObject[] hideTargets = null,
        GameObject interactTip = null,
        GameObject playerObject = null)
    {
        if (!CanLoadScene())
        {
            Debug.LogError($"无法加载 {IllustratedUiSceneName}，请确认 {IllustratedUiScenePath} 已加入 Build Settings。");
            return false;
        }

        if (TryGetUIManager(out UIManager manager))
        {
            BindRuntimeContext(manager, hideTargets, interactTip, playerObject);
            manager.OpenIllustratedHandbook(source, page);
            return true;
        }

        EnsureInstance().QueueOpen(new OpenRequest(source, page, hideTargets, interactTip, playerObject));
        return true;
    }

    public static void Close()
    {
        if (TryGetUIManager(out UIManager manager))
        {
            manager.CloseIllustratedHandbook();
        }
    }

    public static void ReleaseIfNeeded()
    {
        if (instance == null)
        {
            return;
        }

        instance.ReleaseLoadedSceneIfNeeded();
    }

    private static bool CanLoadScene()
    {
        return Application.CanStreamedLevelBeLoaded(IllustratedUiSceneName) ||
               Application.CanStreamedLevelBeLoaded(IllustratedUiScenePath);
    }

    private static bool TryResolveUiManager(Scene scene, out UIManager manager)
    {
        manager = null;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            UIManager candidate = roots[i].GetComponentInChildren<UIManager>(true);
            if (candidate == null)
            {
                continue;
            }

            manager = candidate;
            return true;
        }

        return false;
    }

    private static IllustratedUISceneLoader EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject loaderObject = new GameObject(LoaderObjectName);
        DontDestroyOnLoad(loaderObject);
        instance = loaderObject.AddComponent<IllustratedUISceneLoader>();
        return instance;
    }

    private static void BindRuntimeContext(
        UIManager manager,
        GameObject[] hideTargets,
        GameObject interactTip,
        GameObject playerObject)
    {
        if (manager == null)
        {
            return;
        }

        IllustratedHandbookTabsController.EnsureInstalled(manager);
        SetInitialCanvasStates(manager);

        GameObject resolvedInteractTip = interactTip != null
            ? interactTip
            : ResolveInteractTip();
        GameObject resolvedPlayerObject = playerObject != null
            ? playerObject
            : ResolvePlayerObject();
        GameObject[] resolvedHideTargets = hideTargets ?? Array.Empty<GameObject>();

        manager.ConfigureForRuntime(
            manager.illustratedHandbook,
            manager.detailedInformation,
            resolvedHideTargets,
            resolvedInteractTip,
            resolvedPlayerObject);

        UIRootManager.Instance?.RefreshRuntimeBindings();
    }

    private static void SetInitialCanvasStates(UIManager manager)
    {
        if (manager == null)
        {
            return;
        }

        Transform root = manager.transform.parent != null ? manager.transform.parent : manager.transform;
        for (int i = 0; i < HiddenCanvasNames.Length; i++)
        {
            Transform child = root.Find(HiddenCanvasNames[i]);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        if (manager.illustratedHandbook != null)
        {
            manager.illustratedHandbook.SetActive(false);
        }

        if (manager.detailedInformation != null)
        {
            manager.detailedInformation.SetActive(false);
        }
    }

    private static GameObject ResolveInteractTip()
    {
        UIRootManager rootManager = UIRootManager.Instance;
        if (rootManager != null && rootManager.interactTipUI != null)
        {
            return rootManager.interactTipUI.gameObject;
        }

        GameObject prompt = GameObject.Find("InteractPrompt");
        return prompt;
    }

    private static GameObject ResolvePlayerObject()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            return playerObject;
        }

        PlayerMove playerMove = FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
        {
            return playerMove.gameObject;
        }

        PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>(true);
        if (playerAttack != null)
        {
            return playerAttack.gameObject;
        }

        return null;
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
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    private void QueueOpen(OpenRequest request)
    {
        if (request != null)
        {
            pendingRequest = request;
            loadedForScenePath = SceneManager.GetActiveScene().path;
        }

        if (loadRoutine == null)
        {
            loadRoutine = StartCoroutine(EnsureLoadedRoutine());
        }
    }

    private IEnumerator EnsureLoadedRoutine()
    {
        while (true)
        {
            Scene scene = SceneManager.GetSceneByName(IllustratedUiSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(IllustratedUiSceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogError($"加载 {IllustratedUiSceneName} 失败。");
                    break;
                }

                while (!loadOperation.isDone)
                {
                    yield return null;
                }

                scene = SceneManager.GetSceneByName(IllustratedUiSceneName);
            }

            UIManager manager = null;
            while (!TryResolveUiManager(scene, out manager))
            {
                yield return null;
            }

            if (pendingRequest == null)
            {
                break;
            }

            OpenRequest request = pendingRequest;
            pendingRequest = null;
            BindRuntimeContext(manager, request.HideTargets, request.InteractTip, request.PlayerObject);
            manager.OpenIllustratedHandbook(request.Source, request.Page);

            if (pendingRequest == null)
            {
                break;
            }
        }

        loadRoutine = null;
    }

    private void HandleActiveSceneChanged(Scene previous, Scene next)
    {
        if (!string.IsNullOrEmpty(loadedForScenePath) &&
            !string.IsNullOrEmpty(next.path) &&
            !string.Equals(next.path, loadedForScenePath, StringComparison.Ordinal))
        {
            ReleaseLoadedSceneIfNeeded();
        }
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        if (IsIllustratedUiScene(scene))
        {
            pendingRequest = null;
            loadRoutine = null;
            loadedForScenePath = null;
        }
    }

    private void ReleaseLoadedSceneIfNeeded()
    {
        Scene scene = SceneManager.GetSceneByName(IllustratedUiSceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            loadedForScenePath = null;
            return;
        }

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        pendingRequest = null;
        loadedForScenePath = null;
        StartCoroutine(UnloadSceneRoutine(scene));
    }

    private static IEnumerator UnloadSceneRoutine(Scene scene)
    {
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
        if (unloadOperation == null)
        {
            yield break;
        }

        while (!unloadOperation.isDone)
        {
            yield return null;
        }

        UIRootManager.Instance?.RefreshRuntimeBindings();
    }
}
