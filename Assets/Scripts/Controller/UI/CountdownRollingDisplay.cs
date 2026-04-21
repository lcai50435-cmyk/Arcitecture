using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CountdownRollingDisplay : MonoBehaviour
{
    private const string OverlayRootName = "CountdownRollingOverlay";
    private const float DigitWidthPadding = 6f;
    private const float SymbolWidthPadding = 4f;
    private const float NormalRollDuration = 0.18f;
    private const float DangerRollDuration = 0.12f;
    private const float NormalBounceDistance = 3f;
    private const float DangerBounceDistance = 5f;
    private const float PulseDuration = 0.16f;
    private const float DangerPulseScale = 1.06f;

    private static readonly Color DangerColor = new Color(1f, 0.36f, 0.34f, 1f);
    private static readonly Color DangerHighlightColor = new Color(1f, 0.88f, 0.82f, 1f);

    private readonly List<CharacterSlot> slots = new List<CharacterSlot>();

    private TextMeshProUGUI anchorText;
    private RectTransform overlayRoot;
    private Image overlayMaskImage;
    private Mask overlayMask;
    private HorizontalLayoutGroup layoutGroup;
    private Coroutine pulseCoroutine;
    private string currentValue = string.Empty;
    private bool hasValue;
    private bool currentDangerState;
    private Color normalColor = Color.white;
    private float digitWidth = 28f;
    private float symbolWidth = 14f;
    private float slotHeight = 48f;

    public static CountdownRollingDisplay GetOrCreate(TextMeshProUGUI anchor)
    {
        if (anchor == null)
        {
            return null;
        }

        CountdownRollingDisplay display = anchor.GetComponent<CountdownRollingDisplay>();
        if (display == null)
        {
            display = anchor.gameObject.AddComponent<CountdownRollingDisplay>();
        }

        display.Bind(anchor);
        return display;
    }

    public void Bind(TextMeshProUGUI anchor)
    {
        if (anchor == null)
        {
            return;
        }

        anchorText = anchor;
        if (anchor.color.a > 0.01f)
        {
            normalColor = new Color(anchor.color.r, anchor.color.g, anchor.color.b, 1f);
        }

        EnsureOverlayRoot();
        SyncAppearance();
    }

    public void SetDisplay(string value, bool isDangerState)
    {
        if (string.IsNullOrEmpty(value) || !EnsureReady())
        {
            return;
        }

        if (hasValue && currentValue == value && currentDangerState == isDangerState)
        {
            return;
        }

        if (!hasValue)
        {
            SetImmediate(value, isDangerState);
            return;
        }

        SyncAppearance();
        EnsureSlotCapacity(value.Length);

        Color baseColor = ResolveBaseColor(isDangerState);
        bool anyDigitRolled = false;

        for (int i = 0; i < slots.Count; i++)
        {
            bool shouldShow = i < value.Length;
            CharacterSlot slot = slots[i];
            slot.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            char nextChar = value[i];
            slot.RefreshMetrics(GetSlotWidth(nextChar), slotHeight);

            if (slot.CurrentChar == nextChar)
            {
                slot.SetStableColor(baseColor);
                continue;
            }

            if (char.IsDigit(slot.CurrentChar) && char.IsDigit(nextChar))
            {
                slot.RollTo(nextChar, isDangerState, baseColor, DangerHighlightColor);
                anyDigitRolled = true;
                continue;
            }

            slot.SetImmediate(nextChar, baseColor);
        }

        currentValue = value;
        currentDangerState = isDangerState;
        hasValue = true;
        anchorText.text = value;

        if (isDangerState && anyDigitRolled)
        {
            StartPulse(DangerPulseScale);
        }
        else if (!isDangerState)
        {
            StopPulse();
        }
    }

    public void SetImmediate(string value, bool isDangerState)
    {
        if (string.IsNullOrEmpty(value) || !EnsureReady())
        {
            return;
        }

        SyncAppearance();
        EnsureSlotCapacity(value.Length);

        Color baseColor = ResolveBaseColor(isDangerState);
        for (int i = 0; i < slots.Count; i++)
        {
            bool shouldShow = i < value.Length;
            CharacterSlot slot = slots[i];
            slot.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            char nextChar = value[i];
            slot.RefreshMetrics(GetSlotWidth(nextChar), slotHeight);
            slot.SetImmediate(nextChar, baseColor);
        }

        currentValue = value;
        currentDangerState = isDangerState;
        hasValue = true;
        anchorText.text = value;

        if (!isDangerState)
        {
            StopPulse();
        }
    }

    public void UseFallbackText(string value, bool isDangerState)
    {
        currentValue = value ?? string.Empty;
        currentDangerState = isDangerState;
        hasValue = !string.IsNullOrEmpty(currentValue);
        StopPulse();

        if (overlayRoot != null)
        {
            overlayRoot.gameObject.SetActive(false);
        }

        if (anchorText == null)
        {
            return;
        }

        anchorText.text = currentValue;
        anchorText.color = ResolveBaseColor(isDangerState);
    }

    private bool EnsureReady()
    {
        if (anchorText == null)
        {
            anchorText = GetComponent<TextMeshProUGUI>();
        }

        if (anchorText == null)
        {
            return false;
        }

        EnsureOverlayRoot();
        return overlayRoot != null && layoutGroup != null;
    }

    private void EnsureOverlayRoot()
    {
        if (anchorText == null)
        {
            return;
        }

        if (overlayRoot == null)
        {
            Transform existing = anchorText.transform.Find(OverlayRootName);
            if (existing != null)
            {
                overlayRoot = existing as RectTransform;
            }
        }

        if (overlayRoot == null)
        {
            GameObject rootObject = new GameObject(
                OverlayRootName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Mask),
                typeof(HorizontalLayoutGroup));
            rootObject.transform.SetParent(anchorText.rectTransform, false);
            overlayRoot = rootObject.GetComponent<RectTransform>();
        }

        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;
        overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        overlayRoot.localScale = Vector3.one;
        overlayRoot.SetAsLastSibling();

        overlayMaskImage = overlayRoot.GetComponent<Image>();
        if (overlayMaskImage == null)
        {
            overlayMaskImage = overlayRoot.gameObject.AddComponent<Image>();
        }

        overlayMaskImage.color = new Color(0f, 0f, 0f, 0.01f);
        overlayMaskImage.raycastTarget = false;

        overlayMask = overlayRoot.GetComponent<Mask>();
        if (overlayMask == null)
        {
            overlayMask = overlayRoot.gameObject.AddComponent<Mask>();
        }

        overlayMask.showMaskGraphic = false;

        layoutGroup = overlayRoot.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = overlayRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layoutGroup.spacing = 0f;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.childAlignment = ResolveLayoutAlignment(anchorText.alignment);
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    private void SyncAppearance()
    {
        if (anchorText == null || overlayRoot == null || layoutGroup == null)
        {
            return;
        }

        if (anchorText.color.a > 0.01f)
        {
            normalColor = new Color(anchorText.color.r, anchorText.color.g, anchorText.color.b, 1f);
        }

        slotHeight = ResolveSlotHeight();
        digitWidth = ResolveDigitWidth();
        symbolWidth = ResolvePreferredWidth(":") + SymbolWidthPadding;

        layoutGroup.childAlignment = ResolveLayoutAlignment(anchorText.alignment);
        overlayRoot.gameObject.SetActive(true);
        overlayRoot.localScale = Vector3.one;

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ApplyTextStyle(anchorText, slotHeight);
        }

        Color hiddenColor = anchorText.color;
        hiddenColor.a = 0f;
        anchorText.color = hiddenColor;
        anchorText.raycastTarget = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(overlayRoot);
    }

    private void EnsureSlotCapacity(int count)
    {
        for (int i = slots.Count; i < count; i++)
        {
            slots.Add(new CharacterSlot(this, overlayRoot, anchorText, slotHeight));
        }
    }

    private float GetSlotWidth(char character)
    {
        if (char.IsDigit(character))
        {
            return digitWidth;
        }

        if (character == ':')
        {
            return symbolWidth;
        }

        return ResolvePreferredWidth(character.ToString()) + SymbolWidthPadding;
    }

    private float ResolveDigitWidth()
    {
        float maxWidth = 0f;
        for (char c = '0'; c <= '9'; c++)
        {
            maxWidth = Mathf.Max(maxWidth, ResolvePreferredWidth(c.ToString()));
        }

        return maxWidth + DigitWidthPadding;
    }

    private float ResolvePreferredWidth(string content)
    {
        if (anchorText == null)
        {
            return 0f;
        }

        Vector2 preferred = anchorText.GetPreferredValues(content);
        return preferred.x;
    }

    private float ResolveSlotHeight()
    {
        if (anchorText == null)
        {
            return 48f;
        }

        float rectHeight = anchorText.rectTransform.rect.height;
        if (rectHeight > 1f)
        {
            return rectHeight;
        }

        return Mathf.Max(anchorText.fontSize * 1.4f, 48f);
    }

    private void StartPulse(float pulseScale)
    {
        if (overlayRoot == null)
        {
            return;
        }

        StopPulse();
        pulseCoroutine = StartCoroutine(PulseRoutine(pulseScale));
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (overlayRoot != null)
        {
            overlayRoot.localScale = Vector3.one;
        }
    }

    private IEnumerator PulseRoutine(float pulseScale)
    {
        float elapsed = 0f;
        while (elapsed < PulseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / PulseDuration);
            float eased = progress < 0.5f
                ? EaseOutCubic(progress / 0.5f)
                : 1f - EaseInCubic((progress - 0.5f) / 0.5f);
            float scale = Mathf.LerpUnclamped(1f, pulseScale, eased);
            overlayRoot.localScale = Vector3.one * scale;
            yield return null;
        }

        overlayRoot.localScale = Vector3.one;
        pulseCoroutine = null;
    }

    private Color ResolveBaseColor(bool isDangerState)
    {
        return isDangerState ? DangerColor : normalColor;
    }

    private static TextAnchor ResolveLayoutAlignment(TextAlignmentOptions alignment)
    {
        switch (alignment)
        {
            case TextAlignmentOptions.Left:
            case TextAlignmentOptions.MidlineLeft:
            case TextAlignmentOptions.BottomLeft:
            case TextAlignmentOptions.BaselineLeft:
            case TextAlignmentOptions.CaplineLeft:
            case TextAlignmentOptions.TopLeft:
                return TextAnchor.MiddleLeft;
            case TextAlignmentOptions.Right:
            case TextAlignmentOptions.MidlineRight:
            case TextAlignmentOptions.BottomRight:
            case TextAlignmentOptions.BaselineRight:
            case TextAlignmentOptions.CaplineRight:
            case TextAlignmentOptions.TopRight:
                return TextAnchor.MiddleRight;
            default:
                return TextAnchor.MiddleCenter;
        }
    }

    private static float EaseOutCubic(float value)
    {
        float inverted = 1f - value;
        return 1f - inverted * inverted * inverted;
    }

    private static float EaseInCubic(float value)
    {
        return value * value * value;
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot.gameObject);
            overlayRoot = null;
        }

        if (anchorText != null)
        {
            anchorText.text = currentValue;
            anchorText.color = ResolveBaseColor(currentDangerState);
        }
    }

    private sealed class CharacterSlot
    {
        private readonly CountdownRollingDisplay owner;
        private readonly RectTransform root;
        private readonly LayoutElement layoutElement;
        private readonly RectTransform viewport;
        private readonly Image viewportImage;
        private readonly Mask viewportMask;
        private readonly RectTransform content;
        private readonly TextMeshProUGUI currentText;
        private readonly TextMeshProUGUI nextText;

        private Coroutine rollCoroutine;
        private float slotHeight;
        private char pendingChar;

        public CharacterSlot(CountdownRollingDisplay owner, Transform parent, TextMeshProUGUI anchor, float slotHeight)
        {
            this.owner = owner;

            GameObject rootObject = new GameObject("Slot", typeof(RectTransform), typeof(LayoutElement));
            rootObject.transform.SetParent(parent, false);
            root = rootObject.GetComponent<RectTransform>();
            layoutElement = rootObject.GetComponent<LayoutElement>();

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(root, false);
            viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = false;
            viewportMask = viewportObject.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);

            currentText = CreateText("Current", content, anchor);
            nextText = CreateText("Next", content, anchor);

            ApplyTextStyle(anchor, slotHeight);
            SetImmediate('0', Color.white);
        }

        public char CurrentChar { get; private set; }

        public void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        public void ApplyTextStyle(TextMeshProUGUI anchor, float nextSlotHeight)
        {
            slotHeight = nextSlotHeight;
            ApplyStyle(currentText, anchor);
            ApplyStyle(nextText, anchor);

            root.sizeDelta = new Vector2(layoutElement.preferredWidth, slotHeight);
            viewport.sizeDelta = new Vector2(layoutElement.preferredWidth, slotHeight);
            content.sizeDelta = new Vector2(layoutElement.preferredWidth, slotHeight * 2f);

            SetTextRect(currentText.rectTransform, layoutElement.preferredWidth, slotHeight, 0f);
            SetTextRect(nextText.rectTransform, layoutElement.preferredWidth, slotHeight, -slotHeight);
        }

        public void RefreshMetrics(float width, float nextSlotHeight)
        {
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.preferredHeight = nextSlotHeight;
            layoutElement.minHeight = nextSlotHeight;
            ApplyTextStyle(owner.anchorText, nextSlotHeight);
        }

        public void SetStableColor(Color baseColor)
        {
            currentText.color = baseColor;
            nextText.color = baseColor;
        }

        public void SetImmediate(char nextChar, Color baseColor)
        {
            StopRoll(false);
            CurrentChar = nextChar;
            pendingChar = nextChar;
            currentText.text = nextChar.ToString();
            nextText.text = nextChar.ToString();
            currentText.color = baseColor;
            nextText.color = baseColor;
            content.anchoredPosition = Vector2.zero;
            SetTextRect(currentText.rectTransform, layoutElement.preferredWidth, slotHeight, 0f);
            SetTextRect(nextText.rectTransform, layoutElement.preferredWidth, slotHeight, -slotHeight);
        }

        public void RollTo(char nextChar, bool isDangerState, Color baseColor, Color highlightColor)
        {
            if (CurrentChar == nextChar)
            {
                SetStableColor(baseColor);
                return;
            }

            StopRoll(true);
            pendingChar = nextChar;
            nextText.text = nextChar.ToString();
            currentText.color = baseColor;
            nextText.color = isDangerState ? highlightColor : baseColor;
            rollCoroutine = owner.StartCoroutine(RollRoutine(isDangerState, baseColor));
        }

        private IEnumerator RollRoutine(bool isDangerState, Color baseColor)
        {
            float duration = isDangerState ? DangerRollDuration : NormalRollDuration;
            float bounceDistance = isDangerState ? DangerBounceDistance : NormalBounceDistance;
            float mainDuration = duration * 0.78f;
            float settleDuration = duration - mainDuration;

            float elapsed = 0f;
            while (elapsed < mainDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / mainDuration);
                float eased = CountdownRollingDisplay.EaseOutCubic(progress);
                float y = Mathf.LerpUnclamped(0f, slotHeight + bounceDistance, eased);
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / settleDuration);
                float eased = CountdownRollingDisplay.EaseInCubic(progress);
                float y = Mathf.LerpUnclamped(slotHeight + bounceDistance, slotHeight, eased);
                content.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }

            CurrentChar = pendingChar;
            currentText.text = CurrentChar.ToString();
            currentText.color = baseColor;
            nextText.text = CurrentChar.ToString();
            nextText.color = baseColor;
            content.anchoredPosition = Vector2.zero;
            SetTextRect(currentText.rectTransform, layoutElement.preferredWidth, slotHeight, 0f);
            SetTextRect(nextText.rectTransform, layoutElement.preferredWidth, slotHeight, -slotHeight);
            rollCoroutine = null;
        }

        private void StopRoll(bool snapToPending)
        {
            if (rollCoroutine != null)
            {
                owner.StopCoroutine(rollCoroutine);
                rollCoroutine = null;
            }

            if (snapToPending)
            {
                CurrentChar = pendingChar;
                currentText.text = CurrentChar.ToString();
                nextText.text = CurrentChar.ToString();
            }

            content.anchoredPosition = Vector2.zero;
            SetTextRect(currentText.rectTransform, layoutElement.preferredWidth, slotHeight, 0f);
            SetTextRect(nextText.rectTransform, layoutElement.preferredWidth, slotHeight, -slotHeight);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TextMeshProUGUI anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            ApplyStyle(tmp, anchor);
            return tmp;
        }

        private static void ApplyStyle(TextMeshProUGUI target, TextMeshProUGUI anchor)
        {
            target.font = anchor.font;
            target.fontSharedMaterial = anchor.fontSharedMaterial;
            target.fontSize = anchor.fontSize;
            target.enableAutoSizing = anchor.enableAutoSizing;
            target.fontSizeMin = anchor.fontSizeMin;
            target.fontSizeMax = anchor.fontSizeMax;
            target.fontStyle = anchor.fontStyle;
            target.characterSpacing = anchor.characterSpacing;
            target.wordSpacing = anchor.wordSpacing;
            target.lineSpacing = anchor.lineSpacing;
            target.alignment = TextAlignmentOptions.Center;
            target.enableWordWrapping = false;
            target.overflowMode = TextOverflowModes.Overflow;
            target.maskable = true;
            target.raycastTarget = false;
            target.color = Color.white;
        }

        private static void SetTextRect(RectTransform rect, float width, float height, float yOffset)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, yOffset);
        }
    }
}
