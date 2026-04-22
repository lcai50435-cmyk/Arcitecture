using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    private const string GameplayPauseReason = "RuntimeDialog";
    private const float DefaultRevealDurationPerWeight = 0.03f;
    private const float MinimumRevealDuration = 0.2f;
    private const float MaximumRevealDuration = 1.8f;
    private const float TextFadeInDuration = 0.22f;
    private const float TextFloatDistance = 10f;
    private const float TextStartScaleFactor = 0.985f;
    private const float TextPopStrength = 0.018f;

    [Header("UI 组件")]
    public GameObject dialogPanel;
    public Text descriptionText;

    [Header("点击关闭按钮")]
    public Button clickCloseButton;

    [Header("需要隐藏的其他 UI")]
    public GameObject[] uiToHide;

    [Header("自动关闭时间")]
    public float displayDuration = 4f;

    [Header("是否允许普通弹窗显示")]
    public bool canShow = true;

    private BackpackMananger backpackManager;
    private Coroutine currentCoroutine;
    private bool waitingForClickClose;
    private bool isClosingDialog;
    private bool isRevealPlaying;
    private bool requestedGameplayPause;
    private string activeDialogContent = string.Empty;
    private RectTransform descriptionRectTransform;
    private Vector2 descriptionTextOrigin;
    private Vector3 descriptionTextScaleOrigin;
    private bool cachedDescriptionTransform;

    private void Start()
    {
        backpackManager = BackpackMananger.Instance;
        RuntimeTextFontRepair.RepairLegacyText(descriptionText);
        CacheDescriptionTransform();

        if (backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType += ShowDialogByCrystal;
        }

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.AddListener(OnClickCloseDialog);
        }

        ForceHideImmediately();
    }

    private void OnDestroy()
    {
        if (backpackManager != null)
        {
            backpackManager.OnFirstTimePickItemType -= ShowDialogByCrystal;
        }

        if (clickCloseButton != null)
        {
            clickCloseButton.onClick.RemoveListener(OnClickCloseDialog);
        }
    }

    private void ShowDialogByCrystal(ArchitecturalCrystal crystal)
    {
        if (crystal.isUnlockMaterial) return;

        string desc = BuildSpiritIntro(crystal);
        InternalShow(desc, false);
    }

    public void ShowAutoDialog(string desc)
    {
        if (!canShow) return;
        InternalShow(desc, true);
    }

    public void ShowAutoDialogForce(string desc)
    {
        InternalShow(desc, true);
    }

    public void ShowClickCloseDialog(string desc)
    {
        InternalShow(desc, false);
    }

    private bool InternalShow(string desc, bool autoClose)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Dialog 所在对象未激活，无法显示弹窗");
            return false;
        }

        activeDialogContent = desc ?? string.Empty;

        if (descriptionText != null)
        {
            PrepareDescriptionForReveal();
        }

        HideOtherUI(true);
        isClosingDialog = false;
        PauseGameForFirstPickDialog();

        if (UIRootManager.Instance != null)
        {
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }

            UIRootManager.Instance.OpenModal(RuntimeModalType.Dialog, RuntimeModalOpenSource.None);
        }
        else if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = !autoClose;
        isRevealPlaying = descriptionText != null && activeDialogContent.Length > 0;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(waitingForClickClose);
        }

        currentCoroutine = StartCoroutine(PlayDialogSequence(autoClose));

        return true;
    }

    private IEnumerator PlayDialogSequence(bool autoClose)
    {
        yield return RevealDialogText();

        isRevealPlaying = false;

        if (!autoClose)
        {
            currentCoroutine = null;
            yield break;
        }

        yield return new WaitForSecondsRealtime(displayDuration);
        currentCoroutine = null;
        CloseDialog();
    }

    private void OnClickCloseDialog()
    {
        if (!waitingForClickClose) return;

        if (isRevealPlaying)
        {
            CompleteRevealImmediately();
            return;
        }

        CloseDialog();
    }

    public void CloseDialog()
    {
        if (isClosingDialog)
        {
            return;
        }

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = true;

        if (UIRootManager.Instance != null && UIRootManager.Instance.ActiveModalType == RuntimeModalType.Dialog)
        {
            UIRootManager.Instance.CloseModalFlow(CompleteCloseDialog);
            return;
        }

        CompleteCloseDialog();
    }

    public void ForceHideImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        waitingForClickClose = false;
        isClosingDialog = false;
        isRevealPlaying = false;
        activeDialogContent = string.Empty;

        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (descriptionText != null)
        {
            RestoreDescriptionPresentation(clearText: true);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (UIRootManager.Instance != null)
        {
            UIRootManager.Instance.HideDialog();
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
    }

    private void CompleteCloseDialog()
    {
        if (clickCloseButton != null)
        {
            clickCloseButton.gameObject.SetActive(false);
        }

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        HideOtherUI(false);
        ResumeGameAfterFirstPickDialog();
        isClosingDialog = false;
        isRevealPlaying = false;
        RestoreDescriptionPresentation(clearText: true);
    }

    private void PauseGameForFirstPickDialog()
    {
        if (requestedGameplayPause || !GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        RuntimeGameplayPauseController.RequestPause(GameplayPauseReason);
        requestedGameplayPause = true;
    }

    private void ResumeGameAfterFirstPickDialog()
    {
        if (!requestedGameplayPause)
        {
            return;
        }

        RuntimeGameplayPauseController.ReleasePause(GameplayPauseReason);
        requestedGameplayPause = false;
    }

    private string BuildSpiritIntro(ArchitecturalCrystal crystal)
    {
        string desc = string.IsNullOrEmpty(crystal.textDescription)
            ? $"发现了 {crystal.type}。它会带来 {crystal.expValue} 点结构经验。"
            : crystal.textDescription;

        return $"精灵：\n{desc}\n\n点击按钮后继续探索。";
    }

    private void HideOtherUI(bool hide)
    {
        if (uiToHide == null) return;

        foreach (GameObject ui in uiToHide)
        {
            if (ui != null)
            {
                ui.SetActive(!hide);
            }
        }
    }

    private IEnumerator RevealDialogText()
    {
        if (descriptionText == null)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(activeDialogContent))
        {
            RestoreDescriptionPresentation(clearText: true);
            yield break;
        }

        float[] cumulativeWeights = BuildCumulativeWeights(activeDialogContent);
        float totalWeight = cumulativeWeights[cumulativeWeights.Length - 1];
        float revealDuration = Mathf.Clamp(totalWeight * DefaultRevealDurationPerWeight, MinimumRevealDuration, MaximumRevealDuration);
        float elapsed = 0f;
        int lastVisibleCount = -1;

        while (elapsed < revealDuration)
        {
            float progress = Mathf.Clamp01(elapsed / revealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float visibleWeight = totalWeight * easedProgress;
            int visibleCount = ResolveVisibleCharacterCount(cumulativeWeights, visibleWeight);

            if (visibleCount != lastVisibleCount)
            {
                descriptionText.text = BuildRevealText(visibleCount);
                lastVisibleCount = visibleCount;
            }

            UpdateDescriptionPresentation(progress);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        descriptionText.text = activeDialogContent;
        UpdateDescriptionPresentation(1f);
    }

    private void CompleteRevealImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        isRevealPlaying = false;

        if (descriptionText != null)
        {
            descriptionText.text = activeDialogContent;
            UpdateDescriptionPresentation(1f);
        }
    }

    private void PrepareDescriptionForReveal()
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();
        descriptionText.supportRichText = true;
        descriptionText.canvasRenderer.SetAlpha(0f);
        descriptionText.text = BuildRevealText(0);

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = descriptionTextOrigin + Vector2.down * TextFloatDistance;
            descriptionRectTransform.localScale = MultiplyScale(descriptionTextScaleOrigin, TextStartScaleFactor);
        }
    }

    private void RestoreDescriptionPresentation(bool clearText)
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();
        descriptionText.canvasRenderer.SetAlpha(1f);
        descriptionText.text = clearText ? string.Empty : activeDialogContent;

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = descriptionTextOrigin;
            descriptionRectTransform.localScale = descriptionTextScaleOrigin;
        }
    }

    private void UpdateDescriptionPresentation(float progress)
    {
        if (descriptionText == null)
        {
            return;
        }

        CacheDescriptionTransform();

        float clampedProgress = Mathf.Clamp01(progress);
        float alphaProgress = Mathf.Clamp01(clampedProgress / TextFadeInDuration);
        float easedAlpha = Mathf.SmoothStep(0f, 1f, alphaProgress);
        float easedMotion = 1f - Mathf.Pow(1f - clampedProgress, 3f);
        float pop = Mathf.Sin(easedMotion * Mathf.PI) * TextPopStrength;
        float scaleFactor = Mathf.Lerp(TextStartScaleFactor, 1f, easedMotion) + pop;

        descriptionText.canvasRenderer.SetAlpha(easedAlpha);

        if (descriptionRectTransform != null)
        {
            descriptionRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                descriptionTextOrigin + Vector2.down * TextFloatDistance,
                descriptionTextOrigin,
                easedMotion);
            descriptionRectTransform.localScale = MultiplyScale(descriptionTextScaleOrigin, scaleFactor);
        }
    }

    private string BuildRevealText(int visibleCount)
    {
        if (string.IsNullOrEmpty(activeDialogContent))
        {
            return string.Empty;
        }

        int clampedVisibleCount = Mathf.Clamp(visibleCount, 0, activeDialogContent.Length);
        if (clampedVisibleCount >= activeDialogContent.Length)
        {
            return activeDialogContent;
        }

        string visibleContent = clampedVisibleCount > 0
            ? activeDialogContent.Substring(0, clampedVisibleCount)
            : string.Empty;
        string hiddenContent = activeDialogContent.Substring(clampedVisibleCount);

        return $"{visibleContent}{WrapHiddenText(hiddenContent)}";
    }

    // 用透明富文本保留完整排版，避免 reveal 过程中出现换行抖动。
    private string WrapHiddenText(string hiddenContent)
    {
        if (string.IsNullOrEmpty(hiddenContent))
        {
            return string.Empty;
        }

        Color hiddenColor = descriptionText != null ? descriptionText.color : Color.white;
        hiddenColor.a = 0f;
        string hiddenColorHex = ColorUtility.ToHtmlStringRGBA(hiddenColor);
        return $"<color=#{hiddenColorHex}>{hiddenContent}</color>";
    }

    private void CacheDescriptionTransform()
    {
        if (cachedDescriptionTransform || descriptionText == null)
        {
            return;
        }

        descriptionRectTransform = descriptionText.rectTransform;
        if (descriptionRectTransform == null)
        {
            return;
        }

        descriptionTextOrigin = descriptionRectTransform.anchoredPosition;
        descriptionTextScaleOrigin = descriptionRectTransform.localScale;
        cachedDescriptionTransform = true;
    }

    private static Vector3 MultiplyScale(Vector3 originalScale, float factor)
    {
        return new Vector3(
            originalScale.x * factor,
            originalScale.y * factor,
            originalScale.z * factor);
    }

    private static float[] BuildCumulativeWeights(string content)
    {
        float[] cumulativeWeights = new float[content.Length];
        float total = 0f;

        for (int i = 0; i < content.Length; i++)
        {
            total += GetRevealWeight(content[i]);
            cumulativeWeights[i] = total;
        }

        return cumulativeWeights;
    }

    private static int ResolveVisibleCharacterCount(float[] cumulativeWeights, float visibleWeight)
    {
        if (cumulativeWeights == null || cumulativeWeights.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < cumulativeWeights.Length; i++)
        {
            if (cumulativeWeights[i] > visibleWeight)
            {
                return i;
            }
        }

        return cumulativeWeights.Length;
    }

    private static float GetRevealWeight(char character)
    {
        if (character == '\n' || character == '\r')
        {
            return 0.7f;
        }

        if (char.IsWhiteSpace(character))
        {
            return 0.35f;
        }

        switch (character)
        {
            case '，':
            case '。':
            case '！':
            case '？':
            case '；':
            case '：':
            case '、':
            case ',':
            case '.':
            case '!':
            case '?':
            case ';':
            case ':':
                return 1.65f;
            default:
                return 1f;
        }
    }
}
