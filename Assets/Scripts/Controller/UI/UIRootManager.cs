using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RuntimeModalType
{
    None = 0,
    Handbook = 1,
    DetailPage1 = 2,
    DetailPage2 = 3,
    SubmitSelection1 = 4,
    SubmitSelection2 = 5,
    SubmitSelection3 = 6,
    Dialog = 7,
    Spirit = 8,
    Stage = 9,
    Album = 10
}

public enum RuntimeModalOpenSource
{
    None = 0,
    Interact = 1
}

public static class RuntimeGameplayPauseController
{
    private static readonly HashSet<string> PauseReasons = new HashSet<string>();
    private static float resumeTimeScale = 1f;
    private static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (sceneHookRegistered)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneHookRegistered = false;
        }

        PauseReasons.Clear();
        resumeTimeScale = 1f;
    }

    public static void RequestPause(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || !GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        EnsureSceneHook();
        if (PauseReasons.Count == 0 && Time.timeScale > 0.0001f)
        {
            resumeTimeScale = Time.timeScale;
        }

        PauseReasons.Add(reason);
        Time.timeScale = 0f;
    }

    public static void ReleasePause(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        PauseReasons.Remove(reason);
        if (PauseReasons.Count > 0)
        {
            Time.timeScale = 0f;
            return;
        }

        RestoreTimeScaleIfNeeded();
    }

    private static void EnsureSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PauseReasons.Clear();
        resumeTimeScale = 1f;

        if (!GameplayStageCatalog.IsGameplayScene(scene.name))
        {
            Time.timeScale = 1f;
        }
    }

    private static void RestoreTimeScaleIfNeeded()
    {
        if (GameplayFailureController.IsFailureActive)
        {
            return;
        }

        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            Time.timeScale = 1f;
            resumeTimeScale = 1f;
            return;
        }

        Time.timeScale = resumeTimeScale > 0.0001f ? resumeTimeScale : 1f;
        resumeTimeScale = 1f;
    }
}

public class UIRootManager : MonoBehaviour
{
    private const string ModalPauseReason = "RuntimeModalFlow";

    private enum RuntimeModalFlowGroup
    {
        None = 0,
        Handbook = 1,
        Dialog = 2,
        Spirit = 3,
        Stage = 4,
        Album = 5
    }

    private sealed class RuntimeModalBinding
    {
        public RuntimeModalBinding(RuntimeModalType type, CanvasGroup canvasGroup, Canvas canvas)
        {
            Type = type;
            CanvasGroup = canvasGroup;
            Canvas = canvas;
        }

        public RuntimeModalType Type { get; }
        public CanvasGroup CanvasGroup { get; }
        public Canvas Canvas { get; }
    }

    public static UIRootManager Instance;

    [Header("图鉴主页")]
    public CanvasGroup handbookUI;

    [Header("详细信息页")]
    public CanvasGroup detailUIPage1;
    public CanvasGroup detailUIPage2;

    [Header("提交窗口 - 三个建筑分别一个")]
    public CanvasGroup submitSelectionUI1;
    public CanvasGroup submitSelectionUI2;
    public CanvasGroup submitSelectionUI3;

    [Header("Dialog弹窗")]
    public CanvasGroup dialogUI;

    [Header("基地弹窗（可选）")]
    public CanvasGroup spiritPanelUI;
    public CanvasGroup stageSelectionPanelUI;
    public CanvasGroup albumPanelUI;

    [Header("场景交互提示UI")]
    public CanvasGroup interactTipUI;

    [Header("背包UI（可选）")]
    public CanvasGroup backpackUI;

    private readonly Dictionary<RuntimeModalType, RuntimeModalBinding> modalBindings = new Dictionary<RuntimeModalType, RuntimeModalBinding>();

    private RuntimeModalShell modalShell;
    private RuntimeModalType activeModalType = RuntimeModalType.None;
    private RuntimeModalFlowGroup activeFlowGroup = RuntimeModalFlowGroup.None;
    private RuntimeModalOpenSource activeFlowSource = RuntimeModalOpenSource.None;
    private bool isFlowClosing;
    private int suppressInteractUntilFrame = -1;
    private int suppressCloseUntilFrame = -1;

    private UIManager handbookManager;
    private BaseHubUIController baseHubUiController;
    private Dialog dialogController;
    private DetailedInformationUI detailedInformationController;
    private SubmitSelectionPanelUI[] submitPanelControllers = Array.Empty<SubmitSelectionPanelUI>();

    public bool IsModalFlowOpen => activeFlowGroup != RuntimeModalFlowGroup.None || (modalShell != null && modalShell.IsVisible);
    public RuntimeModalType ActiveModalType => activeModalType;
    public RuntimeModalOpenSource ActiveFlowSource => activeFlowSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        modalShell = GetComponent<RuntimeModalShell>();
        if (modalShell == null)
        {
            modalShell = gameObject.AddComponent<RuntimeModalShell>();
        }
    }

    private void Start()
    {
        RefreshRuntimeBindings();
        HideAllRuntimeUiImmediate();
        ShowBackpack(true);
    }

    private void Update()
    {
        RefreshRuntimeBindingsIfNeeded();
        TryHandleModalCloseHotkeys();
    }

    private void OnDestroy()
    {
        RuntimeGameplayPauseController.ReleasePause(ModalPauseReason);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RefreshRuntimeBindings()
    {
        handbookManager = FindObjectOfType<UIManager>(true);
        baseHubUiController = FindObjectOfType<BaseHubUIController>(true);
        dialogController = FindObjectOfType<Dialog>(true);
        detailedInformationController = FindObjectOfType<DetailedInformationUI>(true);
        submitPanelControllers = FindObjectsOfType<SubmitSelectionPanelUI>(true);

        if (handbookManager != null && handbookManager.illustratedHandbook != null)
        {
            handbookUI = EnsureCanvasGroup(handbookManager.illustratedHandbook);
        }

        if (dialogController != null && dialogController.dialogPanel != null)
        {
            dialogUI = EnsureCanvasGroup(dialogController.dialogPanel);
        }

        if (detailedInformationController != null)
        {
            GameObject detailRootObject = detailedInformationController.detailedInformationPanel != null
                ? detailedInformationController.detailedInformationPanel
                : detailedInformationController.gameObject;
            CanvasGroup detailRoot = EnsureCanvasGroup(detailRootObject);
            detailUIPage1 = detailRoot;
            detailUIPage2 = detailRoot;
        }

        if (submitPanelControllers != null && submitPanelControllers.Length > 0)
        {
            submitSelectionUI1 = ResolveSubmitCanvasGroup(0);
            submitSelectionUI2 = ResolveSubmitCanvasGroup(1);
            submitSelectionUI3 = ResolveSubmitCanvasGroup(2);
        }

        if (spiritPanelUI == null)
        {
            SpiritPanelUI spiritPanel = FindObjectOfType<SpiritPanelUI>(true);
            if (spiritPanel != null)
            {
                spiritPanelUI = EnsureCanvasGroup(spiritPanel.gameObject);
            }
        }

        if (stageSelectionPanelUI == null)
        {
            StageSelectionPanelUI stageSelectionPanel = FindObjectOfType<StageSelectionPanelUI>(true);
            if (stageSelectionPanel != null)
            {
                stageSelectionPanelUI = EnsureCanvasGroup(stageSelectionPanel.gameObject);
            }
        }

        if (albumPanelUI == null)
        {
            BaseHubAlbumPanel albumPanel = FindObjectOfType<BaseHubAlbumPanel>(true);
            if (albumPanel != null)
            {
                albumPanelUI = EnsureCanvasGroup(albumPanel.gameObject);
            }
        }

        if (backpackUI == null)
        {
            BackpackUI backpackView = FindObjectOfType<BackpackUI>(true);
            if (backpackView != null)
            {
                backpackUI = EnsureCanvasGroup(backpackView.gameObject);
            }
        }

        RefreshModalRegistry();
    }

    public void OpenModal(RuntimeModalType type, RuntimeModalOpenSource source, bool inheritFlowSource = false)
    {
        if (type == RuntimeModalType.None)
        {
            return;
        }

        RefreshRuntimeBindings();
        RuntimeModalBinding binding = GetBinding(type);
        if (binding == null)
        {
            Debug.LogWarning($"未找到运行时弹窗绑定：{type}");
            return;
        }

        RuntimeModalFlowGroup targetFlowGroup = GetFlowGroup(type);
        RuntimeModalOpenSource effectiveSource = inheritFlowSource && activeFlowGroup != RuntimeModalFlowGroup.None
            ? activeFlowSource
            : source;
        bool sameFlow = !isFlowClosing &&
                        activeFlowGroup != RuntimeModalFlowGroup.None &&
                        activeFlowGroup == targetFlowGroup;

        PrepareModalForDisplay(binding);

        if (!sameFlow)
        {
            if (modalShell != null && modalShell.IsVisible)
            {
                modalShell.Hide(true);
            }

            if (activeModalType != RuntimeModalType.None)
            {
                RuntimeModalBinding previousBinding = GetBinding(activeModalType);
                if (previousBinding != null && previousBinding != binding)
                {
                    HideModalImmediate(previousBinding);
                }
            }

            activeModalType = type;
            activeFlowGroup = targetFlowGroup;
            activeFlowSource = effectiveSource;
            isFlowClosing = false;
            suppressCloseUntilFrame = Time.frameCount;
            HideBackpack();
            ApplyGameplayPauseForModalFlow();

            if (modalShell != null)
            {
                modalShell.Show(binding.CanvasGroup);
            }

            return;
        }

        RuntimeModalBinding activeBinding = GetBinding(activeModalType);
        if (activeBinding != null && activeBinding != binding)
        {
            HideModalImmediate(activeBinding);
        }

        activeModalType = type;
        activeFlowSource = effectiveSource;
        suppressCloseUntilFrame = Time.frameCount;

        if (modalShell != null)
        {
            modalShell.Retarget(binding.CanvasGroup);
        }
    }

    public bool CloseActiveModal()
    {
        if (isFlowClosing || activeModalType == RuntimeModalType.None)
        {
            return false;
        }

        switch (activeModalType)
        {
            case RuntimeModalType.DetailPage1:
            case RuntimeModalType.DetailPage2:
                if (detailedInformationController != null)
                {
                    detailedInformationController.CloseDetailOnlyReturnHandbook();
                }
                else
                {
                    OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
                }

                return true;
            case RuntimeModalType.SubmitSelection1:
            case RuntimeModalType.SubmitSelection2:
            case RuntimeModalType.SubmitSelection3:
                SubmitSelectionPanelUI submitPanel = FindSubmitPanel(activeModalType);
                if (submitPanel != null)
                {
                    submitPanel.ClosePanel();
                }
                else
                {
                    OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
                }

                return true;
            case RuntimeModalType.Handbook:
                if (handbookManager != null && handbookManager.IsHandbookOpen)
                {
                    handbookManager.CloseIllustratedHandbook();
                }
                else if (baseHubUiController != null)
                {
                    baseHubUiController.CloseAll();
                }
                else
                {
                    CloseModalFlow();
                }

                return true;
            case RuntimeModalType.Dialog:
                if (dialogController != null)
                {
                    dialogController.CloseDialog();
                }
                else
                {
                    CloseModalFlow();
                }

                return true;
            case RuntimeModalType.Spirit:
            case RuntimeModalType.Stage:
            case RuntimeModalType.Album:
                if (baseHubUiController != null)
                {
                    baseHubUiController.CloseAll();
                }
                else
                {
                    CloseModalFlow();
                }

                return true;
            default:
                return false;
        }
    }

    public void CloseModalFlow()
    {
        CloseModalFlowInternal(null, false);
    }

    public void CloseModalFlow(Action afterClosed)
    {
        CloseModalFlowInternal(afterClosed, false);
    }

    public void CloseModalFlowImmediate()
    {
        CloseModalFlowInternal(null, true);
    }

    public bool ShouldSuppressInteractionInput()
    {
        return Time.frameCount <= suppressInteractUntilFrame;
    }

    public void ShowHandbook()
    {
        OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
    }

    public void HideHandbook()
    {
        HideModalImmediate(GetBinding(RuntimeModalType.Handbook));
    }

    public void ShowDetailPage1()
    {
        OpenModal(RuntimeModalType.DetailPage1, RuntimeModalOpenSource.None, true);
    }

    public void ShowDetailPage2()
    {
        OpenModal(RuntimeModalType.DetailPage2, RuntimeModalOpenSource.None, true);
    }

    public void HideAllDetail()
    {
        HideModalImmediate(GetBinding(RuntimeModalType.DetailPage1));
        HideModalImmediate(GetBinding(RuntimeModalType.DetailPage2));
    }

    public void ShowSubmitSelection(int buildingIndex)
    {
        OpenModal(GetSubmitModalType(buildingIndex), RuntimeModalOpenSource.None, true);
    }

    public void HideSubmitSelection(int buildingIndex)
    {
        HideModalImmediate(GetBinding(GetSubmitModalType(buildingIndex)));
    }

    public void HideAllSubmitSelection()
    {
        HideModalImmediate(GetBinding(RuntimeModalType.SubmitSelection1));
        HideModalImmediate(GetBinding(RuntimeModalType.SubmitSelection2));
        HideModalImmediate(GetBinding(RuntimeModalType.SubmitSelection3));
    }

    public void ShowDialog()
    {
        OpenModal(RuntimeModalType.Dialog, RuntimeModalOpenSource.None);
    }

    public void HideDialog()
    {
        RuntimeModalBinding binding = GetBinding(RuntimeModalType.Dialog);
        if (activeModalType == RuntimeModalType.Dialog)
        {
            CloseModalFlowImmediate();
            return;
        }

        HideModalImmediate(binding);
    }

    public void ShowInteractTip() => SetCanvasGroupVisible(interactTipUI, true);
    public void HideInteractTip() => SetCanvasGroupVisible(interactTipUI, false);
    public void ShowBackpack(bool immediate = false) => SetBackpackVisible(true, immediate);
    public void HideBackpack(bool immediate = false) => SetBackpackVisible(false, immediate);

    public void OpenHandbookView()
    {
        OpenModal(RuntimeModalType.Handbook, RuntimeModalOpenSource.None, true);
    }

    public void OpenDetailViewPage1()
    {
        OpenModal(RuntimeModalType.DetailPage1, RuntimeModalOpenSource.None, true);
    }

    public void OpenDetailViewPage2()
    {
        OpenModal(RuntimeModalType.DetailPage2, RuntimeModalOpenSource.None, true);
    }

    public void CloseAllBookUI()
    {
        if (activeFlowGroup == RuntimeModalFlowGroup.Handbook)
        {
            CloseModalFlowImmediate();
        }

        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        ShowBackpack();
        ShowInteractTip();
    }

    public bool IsAnyGameplayBlockingUIOpen()
    {
        return IsModalFlowOpen ||
               IsCanvasGroupOpen(handbookUI) ||
               IsCanvasGroupOpen(detailUIPage1) ||
               IsCanvasGroupOpen(detailUIPage2) ||
               IsCanvasGroupOpen(submitSelectionUI1) ||
               IsCanvasGroupOpen(submitSelectionUI2) ||
               IsCanvasGroupOpen(submitSelectionUI3) ||
               IsCanvasGroupOpen(dialogUI) ||
               IsCanvasGroupOpen(spiritPanelUI) ||
               IsCanvasGroupOpen(albumPanelUI) ||
               IsCanvasGroupOpen(stageSelectionPanelUI) ||
               RuntimePauseMenu.IsPauseOpen;
    }

    private void CloseModalFlowInternal(Action afterClosed, bool immediate)
    {
        if (isFlowClosing)
        {
            return;
        }

        RuntimeModalBinding binding = GetBinding(activeModalType);
        if (binding == null)
        {
            modalShell?.Hide(immediate);
            ShowBackpack(immediate);
            RuntimeGameplayPauseController.ReleasePause(ModalPauseReason);
            activeModalType = RuntimeModalType.None;
            activeFlowGroup = RuntimeModalFlowGroup.None;
            activeFlowSource = RuntimeModalOpenSource.None;
            isFlowClosing = false;
            afterClosed?.Invoke();
            return;
        }

        isFlowClosing = true;
        activeModalType = RuntimeModalType.None;
        activeFlowGroup = RuntimeModalFlowGroup.None;
        activeFlowSource = RuntimeModalOpenSource.None;

        Action finishClose = () =>
        {
            HideModalImmediate(binding);
            ShowBackpack(immediate);
            RuntimeGameplayPauseController.ReleasePause(ModalPauseReason);
            isFlowClosing = false;
            afterClosed?.Invoke();
        };

        if (modalShell != null)
        {
            modalShell.Hide(immediate, finishClose);
            return;
        }

        finishClose();
    }

    private void HideAllRuntimeUiImmediate()
    {
        RefreshModalRegistry();
        activeModalType = RuntimeModalType.None;
        activeFlowGroup = RuntimeModalFlowGroup.None;
        activeFlowSource = RuntimeModalOpenSource.None;
        isFlowClosing = false;

        foreach (KeyValuePair<RuntimeModalType, RuntimeModalBinding> entry in modalBindings)
        {
            HideModalImmediate(entry.Value);
        }

        RuntimeGameplayPauseController.ReleasePause(ModalPauseReason);

        if (modalShell != null)
        {
            modalShell.Hide(true);
        }
    }

    private void RefreshRuntimeBindingsIfNeeded()
    {
        if (handbookManager == null ||
            dialogController == null ||
            detailedInformationController == null ||
            submitPanelControllers == null ||
            submitPanelControllers.Length == 0 ||
            (spiritPanelUI == null && SceneManager.GetActiveScene().name == "BaseScene") ||
            (stageSelectionPanelUI == null && SceneManager.GetActiveScene().name == "BaseScene") ||
            (albumPanelUI == null && SceneManager.GetActiveScene().name == "BaseScene"))
        {
            RefreshRuntimeBindings();
        }
    }

    private void RefreshModalRegistry()
    {
        modalBindings.Clear();
        RegisterModal(RuntimeModalType.Handbook, handbookUI);
        RegisterModal(RuntimeModalType.DetailPage1, detailUIPage1);
        RegisterModal(RuntimeModalType.DetailPage2, detailUIPage2);
        RegisterModal(RuntimeModalType.SubmitSelection1, submitSelectionUI1);
        RegisterModal(RuntimeModalType.SubmitSelection2, submitSelectionUI2);
        RegisterModal(RuntimeModalType.SubmitSelection3, submitSelectionUI3);
        RegisterModal(RuntimeModalType.Dialog, dialogUI);
        RegisterModal(RuntimeModalType.Spirit, spiritPanelUI);
        RegisterModal(RuntimeModalType.Stage, stageSelectionPanelUI);
        RegisterModal(RuntimeModalType.Album, albumPanelUI);
    }

    private void RegisterModal(RuntimeModalType type, CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
        {
            return;
        }

        Canvas canvas = EnsureModalCanvas(canvasGroup.gameObject);
        RuntimeModalBinding binding = new RuntimeModalBinding(type, canvasGroup, canvas);
        modalBindings[type] = binding;
    }

    private RuntimeModalBinding GetBinding(RuntimeModalType type)
    {
        if (type == RuntimeModalType.None)
        {
            return null;
        }

        modalBindings.TryGetValue(type, out RuntimeModalBinding binding);
        return binding;
    }

    private void PrepareModalForDisplay(RuntimeModalBinding binding)
    {
        if (binding == null || binding.CanvasGroup == null)
        {
            return;
        }

        if (!binding.CanvasGroup.gameObject.activeSelf)
        {
            binding.CanvasGroup.gameObject.SetActive(true);
        }

        if (binding.Canvas != null)
        {
            binding.Canvas.overrideSorting = true;
            binding.Canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder;
        }

        binding.CanvasGroup.alpha = 1f;
        binding.CanvasGroup.interactable = true;
        binding.CanvasGroup.blocksRaycasts = true;
    }

    private void HideModalImmediate(RuntimeModalBinding binding)
    {
        if (binding == null || binding.CanvasGroup == null)
        {
            return;
        }

        binding.CanvasGroup.alpha = 0f;
        binding.CanvasGroup.interactable = false;
        binding.CanvasGroup.blocksRaycasts = false;

        if (binding.CanvasGroup.gameObject.activeSelf)
        {
            binding.CanvasGroup.gameObject.SetActive(false);
        }
    }

    private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool active, bool deactivateWhenHidden = true)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (active && !canvasGroup.gameObject.activeSelf)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        canvasGroup.alpha = active ? 1f : 0f;
        canvasGroup.interactable = active;
        canvasGroup.blocksRaycasts = active;

        if (!active && deactivateWhenHidden && canvasGroup.gameObject.activeSelf)
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void SetBackpackVisible(bool visible, bool immediate)
    {
        if (backpackUI == null)
        {
            return;
        }

        if (!backpackUI.gameObject.activeSelf)
        {
            backpackUI.gameObject.SetActive(true);
        }

        BackpackUI backpackView = backpackUI.GetComponent<BackpackUI>();
        if (backpackView != null)
        {
            backpackView.SetRuntimeVisible(visible, immediate);
            return;
        }

        SetCanvasGroupVisible(backpackUI, visible, false);
    }

    private void TryHandleModalCloseHotkeys()
    {
        if (isFlowClosing || activeModalType == RuntimeModalType.None || Time.frameCount <= suppressCloseUntilFrame)
        {
            return;
        }

        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        KeyCode interactKey = GameSettingsStore.GetKeyBinding(GameInputAction.Interact);
        bool shouldSuppressInteract = activeFlowSource == RuntimeModalOpenSource.Interact &&
                                      interactKey != KeyCode.None &&
                                      interactKey == pauseKey;

        if (pauseKey != KeyCode.None && Input.GetKeyDown(pauseKey))
        {
            if (shouldSuppressInteract)
            {
                suppressInteractUntilFrame = Time.frameCount + 1;
            }

            RuntimePauseMenu.ConsumeOpenHotkey();
            CloseActiveModal();
            return;
        }

        if (activeFlowSource != RuntimeModalOpenSource.Interact || interactKey == KeyCode.None)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            suppressInteractUntilFrame = Time.frameCount + 1;
            CloseActiveModal();
        }
    }

    private SubmitSelectionPanelUI FindSubmitPanel(RuntimeModalType type)
    {
        if (submitPanelControllers == null || submitPanelControllers.Length == 0)
        {
            return null;
        }

        RuntimeModalBinding binding = GetBinding(type);
        if (binding == null || binding.CanvasGroup == null)
        {
            return null;
        }

        for (int i = 0; i < submitPanelControllers.Length; i++)
        {
            SubmitSelectionPanelUI panel = submitPanelControllers[i];
            if (panel == null)
            {
                continue;
            }

            GameObject panelRoot = panel.panelRoot != null ? panel.panelRoot : panel.gameObject;
            if (panelRoot == binding.CanvasGroup.gameObject)
            {
                return panel;
            }
        }

        return null;
    }

    private void ApplyGameplayPauseForModalFlow()
    {
        if (activeFlowGroup == RuntimeModalFlowGroup.None)
        {
            return;
        }

        RuntimeGameplayPauseController.RequestPause(ModalPauseReason);
    }

    private CanvasGroup ResolveSubmitCanvasGroup(int index)
    {
        if (submitPanelControllers == null || index < 0 || index >= submitPanelControllers.Length)
        {
            return null;
        }

        SubmitSelectionPanelUI panel = submitPanelControllers[index];
        if (panel == null)
        {
            return null;
        }

        GameObject panelRoot = panel.panelRoot != null ? panel.panelRoot : panel.gameObject;
        return EnsureCanvasGroup(panelRoot);
    }

    private static RuntimeModalFlowGroup GetFlowGroup(RuntimeModalType type)
    {
        switch (type)
        {
            case RuntimeModalType.Handbook:
            case RuntimeModalType.DetailPage1:
            case RuntimeModalType.DetailPage2:
            case RuntimeModalType.SubmitSelection1:
            case RuntimeModalType.SubmitSelection2:
            case RuntimeModalType.SubmitSelection3:
                return RuntimeModalFlowGroup.Handbook;
            case RuntimeModalType.Dialog:
                return RuntimeModalFlowGroup.Dialog;
            case RuntimeModalType.Spirit:
                return RuntimeModalFlowGroup.Spirit;
            case RuntimeModalType.Stage:
                return RuntimeModalFlowGroup.Stage;
            case RuntimeModalType.Album:
                return RuntimeModalFlowGroup.Album;
            default:
                return RuntimeModalFlowGroup.None;
        }
    }

    private static RuntimeModalType GetSubmitModalType(int buildingIndex)
    {
        switch (buildingIndex)
        {
            case 0:
                return RuntimeModalType.SubmitSelection1;
            case 1:
                return RuntimeModalType.SubmitSelection2;
            case 2:
                return RuntimeModalType.SubmitSelection3;
            default:
                return RuntimeModalType.SubmitSelection1;
        }
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static Canvas EnsureModalCanvas(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = target.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder;

        if (target.GetComponent<GraphicRaycaster>() == null)
        {
            target.AddComponent<GraphicRaycaster>();
        }

        return canvas;
    }

    private static bool IsCanvasGroupOpen(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null || !canvasGroup.gameObject.activeInHierarchy)
        {
            return false;
        }

        return canvasGroup.alpha > 0.01f && canvasGroup.blocksRaycasts;
    }
}

public class RuntimePauseMenu : MonoBehaviour
{
    private const string CanvasName = "RuntimePauseMenuCanvas";
    private const int SortingOrder = 280;
    private const string PauseReason = "RuntimePauseMenu";

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.76f);
    private static readonly Color PanelColor = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    private static readonly Color BorderColor = new Color(0.33f, 0.45f, 0.55f, 1f);
    private static readonly Color ButtonColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    private static readonly Color ButtonTextColor = new Color(0.14f, 0.09f, 0.05f, 1f);
    private static readonly Color TitleColor = new Color(0.95f, 0.97f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.78f, 0.83f, 0.90f, 1f);

    public static RuntimePauseMenu Instance { get; private set; }
    public static bool IsPauseOpen => Instance != null && Instance.isOpen;

    private static int suppressOpenUntilFrame = -1;

    private RuntimeSettingsPanel settingsPanel;
    private bool isOpen;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        suppressOpenUntilFrame = -1;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        suppressOpenUntilFrame = -1;
        EnsureInstance();
        if (Instance != null && !Instance.visible)
        {
            Instance.HideImmediate();
        }
    }

    public static void ConsumeOpenHotkey()
    {
        suppressOpenUntilFrame = Time.frameCount + 1;
    }

    public static RuntimePauseMenu EnsureInstance()
    {
        bool supportedScene = GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name);

        if (Instance != null)
        {
            Instance.SetVisible(supportedScene);
            return Instance;
        }

        RuntimePauseMenu existing = FindObjectOfType<RuntimePauseMenu>(true);
        if (existing != null)
        {
            Instance = existing;
            Instance.SetVisible(supportedScene);
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimePauseMenu");
        Instance = runtimeObject.AddComponent<RuntimePauseMenu>();
        Instance.SetVisible(supportedScene);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetVisible(GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name));
        HideImmediate();
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        if (GameplayStageIntroDirector.IsIntroActive)
        {
            return;
        }

        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        if (!Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
        {
            return;
        }

        if (isOpen && settingsPanel != null && settingsPanel.IsCapturingBinding)
        {
            return;
        }

        if (!isOpen && Time.frameCount <= suppressOpenUntilFrame)
        {
            return;
        }

        if (isOpen)
        {
            ConsumeOpenHotkey();
            if (settingsPanel != null)
            {
                settingsPanel.RequestContinueGame();
            }
            else
            {
                ResumeGame();
            }

            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        PauseGame();
    }

    private void OnDestroy()
    {
        if (settingsPanel != null)
        {
            settingsPanel.ContinueRequested -= ResumeGame;
        }

        RuntimeGameplayPauseController.ReleasePause(PauseReason);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void PauseGame()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        RuntimeGameplayPauseController.RequestPause(PauseReason);
        ApplyVisibility(true);
    }

    private void ResumeGame()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        RuntimeGameplayPauseController.ReleasePause(PauseReason);

        if (settingsPanel != null && settingsPanel.IsShown)
        {
            settingsPanel.HideImmediate();
        }
    }

    private void HideImmediate()
    {
        isOpen = false;
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        ApplyVisibility(false);
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (settingsPanel != null)
        {
            settingsPanel.SetVisible(shouldShow);
        }

        if (!shouldShow)
        {
            HideImmediate();
        }
    }

    private void EnsureUi()
    {
        if (settingsPanel != null)
        {
            return;
        }

        settingsPanel = RuntimeSettingsPanel.EnsureInstance();
        settingsPanel.ContinueRequested -= ResumeGame;
        settingsPanel.ContinueRequested += ResumeGame;
        settingsPanel.SetVisible(visible);
        settingsPanel.HideImmediate();
    }

    private void ApplyVisibility(bool show)
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (show)
        {
            settingsPanel.Show();
            return;
        }

        settingsPanel.HideImmediate();
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border);
        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(buttonImage, backgroundColor, 14, 14);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 28f;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = text.GetComponent<RectTransform>();
        StretchRect(textRect);

        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        return text;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

public static class TmpRuntimeFontFallback
{
    private static readonly string[] PreferredFontAssetPaths =
    {
        "Assets/File/Fonts/NotoSansSC-Black SDF.asset",
        "Assets/File/Fonts/Fonts_1 SDF.asset"
    };

    private static readonly string[] PreferredSourceFontPaths =
    {
        "Assets/File/Fonts/NotoSansSC-Black.ttf",
        "Assets/File/Fonts/Fonts_1.ttf"
    };

    private static readonly string[] PreferredFontKeywords =
    {
        "NotoSansSC",
        "Noto Sans SC",
        "PingFang",
        "Hiragino Sans GB"
    };

    private const string RequiredCharacters =
        "按住或轻点查看大地图松开预览收起继续游戏设置返回基地关卡暂停分辨率显示模式窗口全屏比例当前地图交互攻击点击继续返回总音量音乐音量控制全部游戏声音背景音乐单独强度分辨率显示模式当前比例屏幕适配自动根据窗口大小匹配视野生命构筑建筑结构图鉴背包专用材料普通结构解锁消耗数量剩余详情说明近战远程耐久防御速度倍率0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-+/():.% x";

    private static readonly string[] RuntimeFontNames =
    {
        "Noto Sans SC",
        "PingFang SC",
        "Hiragino Sans GB",
        "Songti SC",
        "Arial Unicode MS"
    };

    private static TMP_FontAsset runtimeFallbackFont;
    private static bool ensured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureChineseFallback();
    }

    public static TMP_FontAsset EnsureChineseFallback()
    {
        if (ensured)
        {
            return runtimeFallbackFont;
        }

        ensured = true;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return null;
        }

        PrepareFontAsset(defaultFont);

        runtimeFallbackFont = CreateDynamicFontAsset(defaultFont.sourceFontFile);
        if (!IsUsablePreferredFont(runtimeFallbackFont))
        {
            runtimeFallbackFont = null;
        }

        if (runtimeFallbackFont == null)
        {
            runtimeFallbackFont = ResolveProjectFallback();
        }

        if (runtimeFallbackFont == null)
        {
            runtimeFallbackFont = ResolveLoadedFallback();
        }

        if (runtimeFallbackFont == null && IsUsablePreferredFont(defaultFont))
        {
            runtimeFallbackFont = defaultFont;
        }

        if (runtimeFallbackFont == null)
        {
            runtimeFallbackFont = ResolveSystemFallback();
        }

        if (runtimeFallbackFont == null)
        {
            return defaultFont;
        }

        PrepareFontAsset(runtimeFallbackFont);
        AttachFallback(defaultFont, runtimeFallbackFont);

        return runtimeFallbackFont;
    }

    public static TMP_FontAsset WarmupCharacters(string text)
    {
        TMP_FontAsset primaryFont = EnsureChineseFallback();
        if (string.IsNullOrEmpty(text))
        {
            return primaryFont;
        }

        PrepareFontAsset(primaryFont);
        WarmupCharactersInternal(primaryFont, text);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont != primaryFont)
        {
            PrepareFontAsset(defaultFont);
            WarmupCharactersInternal(defaultFont, text);
        }

        if (defaultFont != null && defaultFont.fallbackFontAssetTable != null)
        {
            for (int i = 0; i < defaultFont.fallbackFontAssetTable.Count; i++)
            {
                TMP_FontAsset fallbackFont = defaultFont.fallbackFontAssetTable[i];
                PrepareFontAsset(fallbackFont);
                WarmupCharactersInternal(fallbackFont, text);
            }
        }

        return primaryFont;
    }

    private static TMP_FontAsset ResolveLoadedFallback()
    {
        TMP_FontAsset[] loadedFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFontAssets.Length; i++)
        {
            TMP_FontAsset fontAsset = loadedFontAssets[i];
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            PrepareFontAsset(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static TMP_FontAsset ResolveProjectFallback()
    {
#if UNITY_EDITOR
        for (int i = 0; i < PreferredSourceFontPaths.Length; i++)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(PreferredSourceFontPaths[i]);
            TMP_FontAsset fontAsset = CreateDynamicFontAsset(sourceFont);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            PrepareFontAsset(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        for (int i = 0; i < PreferredFontAssetPaths.Length; i++)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PreferredFontAssetPaths[i]);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            PrepareFontAsset(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }
#endif

        return null;
    }

    private static TMP_FontAsset ResolveSystemFallback()
    {
        for (int i = 0; i < RuntimeFontNames.Length; i++)
        {
            Font font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(RuntimeFontNames[i], 90);
            }
            catch (Exception)
            {
                continue;
            }

            TMP_FontAsset fontAsset = CreateDynamicFontAsset(font);
            if (!IsPreferredFont(fontAsset))
            {
                continue;
            }

            PrepareFontAsset(fontAsset);
            if (IsUsablePreferredFont(fontAsset))
            {
                return fontAsset;
            }
        }

        return null;
    }

    private static TMP_FontAsset CreateDynamicFontAsset(Font font)
    {
        if (font == null)
        {
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.hideFlags = HideFlags.DontUnloadUnusedAsset;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        TryWarmupFontCharacters(fontAsset);
        return fontAsset;
    }

    private static void PrepareFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        if (fontAsset.fallbackFontAssetTable == null)
        {
            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
        {
            fontAsset.isMultiAtlasTexturesEnabled = true;
            TryWarmupFontCharacters(fontAsset);
        }
    }

    private static void AttachFallback(TMP_FontAsset targetFont, TMP_FontAsset fallbackFont)
    {
        if (targetFont == null || fallbackFont == null || targetFont == fallbackFont)
        {
            return;
        }

        if (targetFont.fallbackFontAssetTable == null)
        {
            targetFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!targetFont.fallbackFontAssetTable.Contains(fallbackFont))
        {
            targetFont.fallbackFontAssetTable.Add(fallbackFont);
        }
    }

    private static bool IsPreferredFont(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return false;
        }

        if (ContainsPreferredKeyword(fontAsset.name))
        {
            return true;
        }

        if (ContainsPreferredKeyword(fontAsset.faceInfo.familyName))
        {
            return true;
        }

        return ContainsPreferredKeyword(fontAsset.sourceFontFile != null ? fontAsset.sourceFontFile.name : string.Empty);
    }

    private static bool IsUsablePreferredFont(TMP_FontAsset fontAsset)
    {
        if (!IsPreferredFont(fontAsset))
        {
            return false;
        }

        if (fontAsset.HasCharacters(RequiredCharacters))
        {
            return true;
        }

        return fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic;
    }

    private static bool ContainsPreferredKeyword(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
        {
            return false;
        }

        for (int i = 0; i < PreferredFontKeywords.Length; i++)
        {
            if (fontName.IndexOf(PreferredFontKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void TryWarmupFontCharacters(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null || fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            return;
        }

        fontAsset.TryAddCharacters(RequiredCharacters);
    }

    private static void WarmupCharactersInternal(TMP_FontAsset fontAsset, string text)
    {
        if (fontAsset == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            return;
        }

        fontAsset.TryAddCharacters(text);
    }
}

public static class RuntimeTextFontRepair
{
    private static readonly string[] RuntimeFontNames =
    {
        "Noto Sans SC",
        "PingFang SC",
        "Hiragino Sans GB",
        "Songti SC",
        "Arial Unicode MS"
    };

    private static Font runtimeUiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        runtimeUiFont = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RepairAllLoadedText();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RepairAllLoadedText();
    }

    public static void RepairAllLoadedText()
    {
        RepairLegacyTexts();
        RepairTmpTexts();
    }

    public static void RepairLegacyText(Text text)
    {
        Font preferredFont = EnsureLegacyChineseFont();
        if (text == null || preferredFont == null)
        {
            return;
        }

        if (!ShouldOverrideLegacyFont(text, preferredFont))
        {
            return;
        }

        text.font = preferredFont;
    }

    public static void RepairTmpText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset preferredFont = !string.IsNullOrEmpty(text.text)
            ? TmpRuntimeFontFallback.WarmupCharacters(text.text)
            : TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;

        if (preferredFont == null)
        {
            return;
        }

        if (!ShouldOverrideTmpFont(text, preferredFont))
        {
            return;
        }

        text.font = preferredFont;
    }

    public static Font EnsureLegacyChineseFont()
    {
        if (runtimeUiFont != null)
        {
            return runtimeUiFont;
        }

        TMP_FontAsset fallbackFont = TmpRuntimeFontFallback.EnsureChineseFallback();
        if (fallbackFont != null && fallbackFont.sourceFontFile != null)
        {
            runtimeUiFont = fallbackFont.sourceFontFile;
            return runtimeUiFont;
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont.sourceFontFile != null)
        {
            runtimeUiFont = defaultFont.sourceFontFile;
            return runtimeUiFont;
        }

        for (int i = 0; i < RuntimeFontNames.Length; i++)
        {
            try
            {
                runtimeUiFont = Font.CreateDynamicFontFromOSFont(RuntimeFontNames[i], 32);
            }
            catch
            {
                runtimeUiFont = null;
            }

            if (runtimeUiFont != null)
            {
                return runtimeUiFont;
            }
        }

        return null;
    }

    private static void RepairLegacyTexts()
    {
        Text[] texts = UnityEngine.Object.FindObjectsOfType<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            RepairLegacyText(texts[i]);
        }
    }

    private static void RepairTmpTexts()
    {
        TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            RepairTmpText(texts[i]);
        }
    }

    private static bool ShouldOverrideLegacyFont(Text text, Font preferredFont)
    {
        if (text.font == null)
        {
            return true;
        }

        if (text.font == preferredFont)
        {
            return false;
        }

        if (IsBuiltinArial(text.font))
        {
            return true;
        }

        return ContainsCjk(text.text);
    }

    private static bool ShouldOverrideTmpFont(TMP_Text text, TMP_FontAsset preferredFont)
    {
        if (text.font == null)
        {
            return true;
        }

        if (text.font == preferredFont)
        {
            return false;
        }

        if (!ContainsCjk(text.text))
        {
            return false;
        }

        return !text.font.HasCharacters(text.text);
    }

    private static bool IsBuiltinArial(Font font)
    {
        return font != null && font.name == "Arial";
    }

    private static bool ContainsCjk(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char value = text[i];
            if ((value >= 0x3400 && value <= 0x9FFF) || (value >= 0xF900 && value <= 0xFAFF))
            {
                return true;
            }
        }

        return false;
    }
}
