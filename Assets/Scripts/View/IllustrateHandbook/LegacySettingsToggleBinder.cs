using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class LegacySettingsToggleBinder : MonoBehaviour
{
    private const string ToggleFalsePath = "Assets/File/Prop/UIProp/Album/ToggleFalse.png";
    private const string ToggleTruePath = "Assets/File/Prop/UIProp/Album/ToggleTrue.png";
    private const string VoiceSettingRootPath = "Panel/BackGround/LeftPanel/VoiceSetting";
    private const string StateTextName = "RuntimeStateText";

    private static readonly Color FallbackOffColor = new Color(0.42f, 0.40f, 0.34f, 1f);
    private static readonly Color FallbackOnColor = new Color(0.75f, 0.61f, 0.34f, 1f);
    private static readonly Color StateTextColor = new Color(0.98f, 0.95f, 0.86f, 1f);

    private static Sprite toggleFalseSprite;
    private static Sprite toggleTrueSprite;
    private static Sprite fallbackSprite;

    private readonly List<ToggleBinding> bindings = new List<ToggleBinding>();

    public void Bind()
    {
        Release();

        BindToggle("Text (TMP)_1", GameAudioToggle.MuteMode);
        BindToggle("Text (TMP)_2", GameAudioToggle.MusicCrossfade);
        BindToggle("Text (TMP)_3", GameAudioToggle.SfxDynamicRange);
        BindToggle("Text (TMP)_4", GameAudioToggle.SpatialAudio);
    }

    public void Refresh()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            ToggleBinding binding = bindings[i];
            if (binding.Toggle == null)
            {
                continue;
            }

            bool isOn = GameSettingsStore.GetAudioToggle(binding.Option);
            binding.Toggle.SetIsOnWithoutNotify(isOn);
            RefreshVisual(binding, isOn);
        }
    }

    public void Release()
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            ToggleBinding binding = bindings[i];
            if (binding.Toggle != null && binding.Listener != null)
            {
                binding.Toggle.onValueChanged.RemoveListener(binding.Listener);
            }
        }

        bindings.Clear();
    }

    private void OnDisable()
    {
        Release();
    }

    private void BindToggle(string rowName, GameAudioToggle option)
    {
        Transform voiceRoot = transform.Find(VoiceSettingRootPath);
        Transform row = voiceRoot != null ? voiceRoot.Find(rowName) : null;
        Toggle toggle = row != null ? row.GetComponentInChildren<Toggle>(true) : null;
        if (toggle == null)
        {
            return;
        }

        Image stateImage = ResolveStateImage(toggle);
        if (stateImage == null)
        {
            return;
        }

        TextMeshProUGUI stateText = EnsureStateText(toggle.transform);
        ToggleBinding binding = new ToggleBinding(option, toggle, stateImage, stateText, stateImage.sprite);
        binding.Listener = isOn => HandleToggleChanged(binding, isOn);

        ConfigureToggle(toggle, stateImage);
        bool savedValue = GameSettingsStore.GetAudioToggle(option);
        toggle.SetIsOnWithoutNotify(savedValue);
        RefreshVisual(binding, savedValue);

        toggle.onValueChanged.AddListener(binding.Listener);
        bindings.Add(binding);
    }

    private void HandleToggleChanged(ToggleBinding binding, bool isOn)
    {
        GameSettingsStore.SetAudioToggle(binding.Option, isOn);
        RefreshVisual(binding, isOn);
    }

    private static void ConfigureToggle(Toggle toggle, Graphic stateGraphic)
    {
        toggle.transition = Selectable.Transition.None;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.targetGraphic = stateGraphic;
        toggle.graphic = null;
        toggle.interactable = true;
    }

    private static Image ResolveStateImage(Toggle toggle)
    {
        Image image = toggle.GetComponentInChildren<Image>(true);
        if (image == null)
        {
            image = toggle.gameObject.AddComponent<Image>();
        }

        image.enabled = true;
        image.raycastTarget = true;
        image.preserveAspect = false;
        return image;
    }

    private static TextMeshProUGUI EnsureStateText(Transform toggleTransform)
    {
        Transform existing = toggleTransform.Find(StateTextName);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            GameObject textObject = new GameObject(StateTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(toggleTransform, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.font = TmpRuntimeFontFallback.WarmupCharacters("开关") ?? TMP_Settings.defaultFontAsset;
        text.fontSize = 8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = StateTextColor;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static void RefreshVisual(ToggleBinding binding, bool isOn)
    {
        Sprite sprite = ResolveToggleSprite(isOn, binding.InitialOffSprite);
        bool usesFallbackSprite = sprite == GetFallbackSprite();

        binding.StateImage.sprite = sprite;
        binding.StateImage.type = usesFallbackSprite ? Image.Type.Sliced : Image.Type.Simple;
        binding.StateImage.color = usesFallbackSprite
            ? (isOn ? FallbackOnColor : FallbackOffColor)
            : Color.white;
        binding.StateImage.enabled = true;
        binding.StateImage.canvasRenderer.SetAlpha(1f);

        if (binding.StateText != null)
        {
            binding.StateText.enabled = usesFallbackSprite;
            binding.StateText.text = isOn ? "开" : "关";
        }
    }

    private static Sprite ResolveToggleSprite(bool isOn, Sprite sceneSprite)
    {
        if (isOn)
        {
            return GetToggleTrueSprite() ?? GetFallbackSprite();
        }

        return sceneSprite != null ? sceneSprite : GetToggleFalseSprite() ?? GetFallbackSprite();
    }

    private static Sprite GetToggleFalseSprite()
    {
        if (toggleFalseSprite == null)
        {
            toggleFalseSprite = RuntimeProjectSpriteLoader.LoadSprite(ToggleFalsePath, true, SpriteMeshType.FullRect);
        }

        return toggleFalseSprite;
    }

    private static Sprite GetToggleTrueSprite()
    {
        if (toggleTrueSprite == null)
        {
            toggleTrueSprite = RuntimeProjectSpriteLoader.LoadSprite(ToggleTruePath, true, SpriteMeshType.FullRect);
        }

        return toggleTrueSprite;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite == null)
        {
            fallbackSprite = RuntimeUiSpriteFactory.GetRoundedSprite(64, 24, 18, 1.2f);
        }

        return fallbackSprite;
    }

    private sealed class ToggleBinding
    {
        public ToggleBinding(GameAudioToggle option, Toggle toggle, Image stateImage, TextMeshProUGUI stateText, Sprite initialOffSprite)
        {
            Option = option;
            Toggle = toggle;
            StateImage = stateImage;
            StateText = stateText;
            InitialOffSprite = initialOffSprite;
        }

        public readonly GameAudioToggle Option;
        public readonly Toggle Toggle;
        public readonly Image StateImage;
        public readonly TextMeshProUGUI StateText;
        public readonly Sprite InitialOffSprite;
        public UnityAction<bool> Listener;
    }
}
