using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class StageCardView
{
    public GameplayStageDefinition definition;
    public RectTransform root;
    public Image background;
    public CanvasGroup selectionGroup;
    public RectTransform pointer;
    public Vector2 pointerBasePosition;
    public Button selectButton;
    public Button enterButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI sceneNameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI lockHintText;
}

public class StageSelectionPanelUI : MonoBehaviour
{
    private const float PointerTravel = 8f;
    private const float PointerAnimationSpeed = 1.8f;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI headerTitleText;
    [SerializeField] private TextMeshProUGUI headerStatusText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    private readonly List<StageCardView> cardViews = new List<StageCardView>();

    private BaseHubUIController uiController;
    private int selectedIndex;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Configure(
        BaseHubUIController controller,
        ScrollRect stageScrollRect,
        Button close,
        Button previous,
        Button next,
        TextMeshProUGUI title,
        TextMeshProUGUI status,
        TextMeshProUGUI pageIndicator)
    {
        uiController = controller;
        scrollRect = stageScrollRect;
        closeButton = close;
        previousButton = previous;
        nextButton = next;
        headerTitleText = title;
        headerStatusText = status;
        pageIndicatorText = pageIndicator;

        EnsureInitialized();
    }

    public void RegisterCard(StageCardView view)
    {
        if (view == null || view.definition == null)
        {
            return;
        }

        cardViews.Add(view);
        if (view.pointer != null)
        {
            view.pointerBasePosition = view.pointer.anchoredPosition;
        }

        BindCardListeners(cardViews.Count - 1);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Open();
    }

    private void Update()
    {
        AnimateSelectedPointer();
    }

    public void Open()
    {
        EnsureInitialized();
        GameplayStageRuntime.EnsureSelectedStageUnlocked();

        int runtimeIndex = GameplayStageCatalog.GetStageIndex(GameplayStageRuntime.SelectedStageId);
        selectedIndex = Mathf.Clamp(runtimeIndex >= 0 ? runtimeIndex : 0, 0, Mathf.Max(0, cardViews.Count - 1));

        RefreshView();
        ScrollToSelection(true);
    }

    private void SelectPreviousStage()
    {
        SelectStage(selectedIndex - 1, true);
    }

    private void SelectNextStage()
    {
        SelectStage(selectedIndex + 1, true);
    }

    private void SelectStage(int index, bool immediate)
    {
        if (cardViews.Count == 0)
        {
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, cardViews.Count - 1);
        RefreshView();
        ScrollToSelection(immediate);
    }

    private void EnterSelectedStage(GameplayStageDefinition definition)
    {
        if (definition == null || definition.isPlaceholder || !GameplayStageCatalog.IsStageUnlocked(definition))
        {
            return;
        }

        GameplayStageRuntime.SelectStage(definition.stageId);
        GameProgressPersistence.SaveIfReady();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(definition.sceneName);
            return;
        }

        SceneManager.LoadScene(definition.sceneName);
    }

    private void RefreshView()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();

        for (int i = 0; i < cardViews.Count; i++)
        {
            StageCardView view = cardViews[i];
            bool unlocked = GameplayStageCatalog.IsStageUnlocked(view.definition, runtimeState);
            bool selected = i == selectedIndex;
            bool placeholder = view.definition != null && view.definition.isPlaceholder;

            if (view.background != null)
            {
                view.background.color = !unlocked
                    ? new Color(0.16f, 0.16f, 0.16f, 0.92f)
                    : new Color(0.18f, 0.22f, 0.18f, 0.92f);
            }

            if (view.selectionGroup != null)
            {
                view.selectionGroup.alpha = selected ? 1f : 0f;
            }

            if (view.titleText != null)
            {
                view.titleText.text = view.definition.displayName;
            }

            if (view.sceneNameText != null)
            {
                view.sceneNameText.text = placeholder
                    ? "地图：未开放"
                    : $"地图 Scene：{view.definition.sceneName}";
            }

            if (view.statusText != null)
            {
                view.statusText.text = placeholder
                    ? "未开放"
                    : unlocked
                    ? (selected ? "当前选中" : "已解锁")
                    : "未解锁";
            }

            if (view.lockHintText != null)
            {
                view.lockHintText.text = placeholder
                    ? view.definition.lockedHint
                    : unlocked
                    ? "可进入该关卡"
                    : view.definition.lockedHint;
            }

            if (view.enterButton != null)
            {
                view.enterButton.interactable = unlocked && !placeholder;
            }
        }

        GameplayStageDefinition selectedStage = cardViews.Count > 0 ? cardViews[selectedIndex].definition : null;
        bool isSelectedStageUnlocked = GameplayStageCatalog.IsStageUnlocked(selectedStage, runtimeState);
        bool isSelectedStagePlaceholder = selectedStage != null && selectedStage.isPlaceholder;
        if (headerTitleText != null)
        {
            headerTitleText.text = selectedStage != null ? selectedStage.displayName : "选择关卡";
        }

        if (headerStatusText != null)
        {
            headerStatusText.text = selectedStage == null
                ? string.Empty
                : isSelectedStagePlaceholder
                    ? $"状态：{selectedStage.lockedHint}"
                    : isSelectedStageUnlocked
                    ? $"地图：{selectedStage.sceneName}    状态：可进入"
                    : $"地图：{selectedStage.sceneName}    状态：{selectedStage.lockedHint}";
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = cardViews.Count == 0
                ? string.Empty
                : $"{selectedIndex + 1} / {cardViews.Count}";
        }

        if (previousButton != null)
        {
            previousButton.interactable = selectedIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = selectedIndex < cardViews.Count - 1;
        }
    }

    private void AnimateSelectedPointer()
    {
        if (cardViews.Count == 0 || selectedIndex < 0 || selectedIndex >= cardViews.Count)
        {
            return;
        }

        StageCardView view = cardViews[selectedIndex];
        if (view == null || view.pointer == null)
        {
            return;
        }

        float progress = Mathf.PingPong(Time.unscaledTime * PointerAnimationSpeed, 1f);
        float eased = 0.5f - Mathf.Cos(progress * Mathf.PI) * 0.5f;
        float xOffset = Mathf.Lerp(-PointerTravel, PointerTravel, eased);
        view.pointer.anchoredPosition = view.pointerBasePosition + new Vector2(xOffset, 0f);
    }

    private void ScrollToSelection(bool shouldScroll)
    {
        if (!shouldScroll || scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = GetVerticalNormalizedPositionForIndex(selectedIndex);
    }

    private float GetVerticalNormalizedPositionForIndex(int index)
    {
        if (cardViews.Count <= 1)
        {
            return 1f;
        }

        return 1f - Mathf.Clamp01(index / (float)(cardViews.Count - 1));
    }

    public void BindController(BaseHubUIController controller)
    {
        if (controller != null)
        {
            uiController = controller;
        }

        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (uiController == null)
        {
            uiController = GetComponentInParent<BaseHubUIController>();
            if (uiController == null)
            {
                uiController = FindObjectOfType<BaseHubUIController>(true);
            }
        }

        if (cardViews.Count == 0)
        {
            DiscoverCardsFromScene();
        }

        BindNavigationButtons();
    }

    private void BindNavigationButtons()
    {
        closeButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.AddListener(() => uiController?.CloseAll());

        previousButton?.onClick.RemoveAllListeners();
        previousButton?.onClick.AddListener(SelectPreviousStage);

        nextButton?.onClick.RemoveAllListeners();
        nextButton?.onClick.AddListener(SelectNextStage);

        for (int i = 0; i < cardViews.Count; i++)
        {
            BindCardListeners(i);
        }
    }

    private void BindCardListeners(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= cardViews.Count)
        {
            return;
        }

        StageCardView view = cardViews[cardIndex];
        if (view == null || view.definition == null)
        {
            return;
        }

        if (view.selectButton != null)
        {
            view.selectButton.onClick.RemoveAllListeners();
            view.selectButton.onClick.AddListener(() => SelectStage(cardIndex, false));
        }

        if (view.enterButton != null)
        {
            view.enterButton.onClick.RemoveAllListeners();
            view.enterButton.onClick.AddListener(() => EnterSelectedStage(view.definition));
        }
    }

    private void DiscoverCardsFromScene()
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            return;
        }

        IReadOnlyList<GameplayStageDefinition> stageDefinitions = GameplayStageCatalog.GetAll();
        for (int i = 0; i < scrollRect.content.childCount; i++)
        {
            RectTransform cardRoot = scrollRect.content.GetChild(i) as RectTransform;
            if (cardRoot == null)
            {
                continue;
            }

            Button selectButtonView = FindNamedComponent<Button>(cardRoot, "SelectButton");
            if (selectButtonView == null)
            {
                continue;
            }

            TextMeshProUGUI titleTextView = FindNamedComponent<TextMeshProUGUI>(cardRoot, "Title");
            TextMeshProUGUI sceneTextView = FindNamedComponent<TextMeshProUGUI>(cardRoot, "Scene");
            GameplayStageDefinition definition = ResolveDefinition(i, stageDefinitions, titleTextView, sceneTextView);
            if (definition == null)
            {
                continue;
            }

            CanvasGroup selectionGroup = FindNamedComponent<CanvasGroup>(cardRoot, "SelectedState");
            RectTransform pointer = selectionGroup != null
                ? selectionGroup.transform.Find("SelectedPointer") as RectTransform
                : null;
            RegisterCard(new StageCardView
            {
                definition = definition,
                root = cardRoot,
                background = cardRoot.GetComponent<Image>(),
                selectionGroup = selectionGroup,
                pointer = pointer,
                selectButton = selectButtonView,
                enterButton = FindStageEnterButton(cardRoot),
                titleText = titleTextView,
                sceneNameText = sceneTextView,
                statusText = FindNamedComponent<TextMeshProUGUI>(cardRoot, "Status"),
                lockHintText = FindNamedComponent<TextMeshProUGUI>(cardRoot, "LockHint")
            });
        }
    }

    private GameplayStageDefinition ResolveDefinition(
        int fallbackIndex,
        IReadOnlyList<GameplayStageDefinition> stageDefinitions,
        TextMeshProUGUI titleTextView,
        TextMeshProUGUI sceneTextView)
    {
        string sceneName = ExtractSceneName(sceneTextView != null ? sceneTextView.text : string.Empty);
        if (!string.IsNullOrEmpty(sceneName))
        {
            GameplayStageDefinition definition = GameplayStageCatalog.GetStageByScene(sceneName);
            if (definition != null)
            {
                return definition;
            }
        }

        string displayName = titleTextView != null ? titleTextView.text.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(displayName))
        {
            for (int i = 0; i < stageDefinitions.Count; i++)
            {
                GameplayStageDefinition definition = stageDefinitions[i];
                if (definition != null && string.Equals(definition.displayName, displayName, StringComparison.Ordinal))
                {
                    return definition;
                }
            }
        }

        return fallbackIndex >= 0 && fallbackIndex < stageDefinitions.Count
            ? stageDefinitions[fallbackIndex]
            : null;
    }

    private static string ExtractSceneName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        int separatorIndex = Mathf.Max(text.LastIndexOf('：'), text.LastIndexOf(':'));
        return separatorIndex >= 0 && separatorIndex + 1 < text.Length
            ? text.Substring(separatorIndex + 1).Trim()
            : text.Trim();
    }

    private static Button FindStageEnterButton(Transform root)
    {
        Button enterButton = FindNamedComponent<Button>(root, "EnterButton");
        return enterButton != null ? enterButton : FindNamedComponent<Button>(root, "GotoButton");
    }

    private static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && string.Equals(component.gameObject.name, objectName, StringComparison.Ordinal))
            {
                return component;
            }
        }

        return null;
    }
}
