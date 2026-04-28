using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class StageCardView
{
    public GameplayStageDefinition definition;
    public RectTransform root;
    public Image background;
    public Button selectButton;
    public Button enterButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI sceneNameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI lockHintText;
}

public class StageSelectionPanelUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
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
    private bool isDragging;
    private bool isSnapping;
    private float snapVelocity;

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
        BindCardListeners(cardViews.Count - 1);
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Open();
    }

    private void Update()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            if (!isDragging &&
                Input.GetMouseButtonDown(0) &&
                RectTransformUtility.RectangleContainsScreenPoint(scrollRect.viewport, Input.mousePosition, null))
            {
                isDragging = true;
                isSnapping = false;
            }
            else if (isDragging && Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                SelectStage(GetNearestIndex(), false);
            }
        }

        if (!isSnapping || scrollRect == null || isDragging)
        {
            return;
        }

        float target = GetNormalizedPositionForIndex(selectedIndex);
        float nextValue = Mathf.SmoothDamp(
            scrollRect.horizontalNormalizedPosition,
            target,
            ref snapVelocity,
            0.12f,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        scrollRect.horizontalNormalizedPosition = nextValue;

        if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - target) <= 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = target;
            isSnapping = false;
            snapVelocity = 0f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapping = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SelectStage(GetNearestIndex(), false);
    }

    public void Open()
    {
        EnsureInitialized();
        GameplayStageRuntime.EnsureSelectedStageUnlocked();

        int runtimeIndex = GameplayStageCatalog.GetStageIndex(GameplayStageRuntime.SelectedStageId);
        selectedIndex = Mathf.Clamp(runtimeIndex >= 0 ? runtimeIndex : 0, 0, Mathf.Max(0, cardViews.Count - 1));

        RefreshView();
        SnapToSelection(true);
    }

    private void SelectPreviousStage()
    {
        SelectStage(selectedIndex - 1, false);
    }

    private void SelectNextStage()
    {
        SelectStage(selectedIndex + 1, false);
    }

    private void SelectStage(int index, bool immediate)
    {
        if (cardViews.Count == 0)
        {
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, cardViews.Count - 1);
        RefreshView();
        SnapToSelection(immediate);
    }

    private void EnterSelectedStage(GameplayStageDefinition definition)
    {
        if (definition == null || !GameplayStageCatalog.IsStageUnlocked(definition))
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

            if (view.background != null)
            {
                view.background.color = !unlocked
                    ? new Color(0.16f, 0.16f, 0.16f, 0.92f)
                    : selected
                        ? new Color(0.44f, 0.33f, 0.16f, 0.96f)
                        : new Color(0.18f, 0.22f, 0.18f, 0.92f);
            }

            if (view.titleText != null)
            {
                view.titleText.text = view.definition.displayName;
            }

            if (view.sceneNameText != null)
            {
                view.sceneNameText.text = $"地图 Scene：{view.definition.sceneName}";
            }

            if (view.statusText != null)
            {
                view.statusText.text = unlocked
                    ? (selected ? "当前选中" : "已解锁")
                    : "未解锁";
            }

            if (view.lockHintText != null)
            {
                view.lockHintText.text = unlocked
                    ? "可进入该关卡"
                    : view.definition.lockedHint;
            }

            if (view.enterButton != null)
            {
                view.enterButton.interactable = unlocked;
            }
        }

        GameplayStageDefinition selectedStage = cardViews.Count > 0 ? cardViews[selectedIndex].definition : null;
        bool isSelectedStageUnlocked = GameplayStageCatalog.IsStageUnlocked(selectedStage, runtimeState);
        if (headerTitleText != null)
        {
            headerTitleText.text = selectedStage != null ? selectedStage.displayName : "选择关卡";
        }

        if (headerStatusText != null)
        {
            headerStatusText.text = selectedStage == null
                ? string.Empty
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

    private void SnapToSelection(bool immediate)
    {
        if (scrollRect == null)
        {
            return;
        }

        float target = GetNormalizedPositionForIndex(selectedIndex);
        if (immediate)
        {
            scrollRect.horizontalNormalizedPosition = target;
            isSnapping = false;
            snapVelocity = 0f;
            return;
        }

        isSnapping = true;
    }

    private int GetNearestIndex()
    {
        if (cardViews.Count <= 1 || scrollRect == null)
        {
            return 0;
        }

        return Mathf.Clamp(
            Mathf.RoundToInt(scrollRect.horizontalNormalizedPosition * (cardViews.Count - 1)),
            0,
            cardViews.Count - 1);
    }

    private float GetNormalizedPositionForIndex(int index)
    {
        if (cardViews.Count <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01(index / (float)(cardViews.Count - 1));
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

            RegisterCard(new StageCardView
            {
                definition = definition,
                root = cardRoot,
                background = cardRoot.GetComponent<Image>(),
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
