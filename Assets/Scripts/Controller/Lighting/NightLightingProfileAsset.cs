using UnityEngine;

[CreateAssetMenu(menuName = "Arcitecture/Lighting/Night Lighting Profile")]
public sealed class NightLightingProfileAsset : ScriptableObject
{
    [SerializeField] private string targetSceneName = string.Empty;
    [SerializeField] private bool useCountdownProgress = false;
    [SerializeField, Range(0f, 1f)] private float fixedNightProgress = 0.3f;
    [SerializeField] private Color overlayTint = new Color(0.05f, 0.08f, 0.12f, 1f);
    [SerializeField, Range(0f, 1f)] private float overlayAlphaAtStart = 0.04f;
    [SerializeField, Range(0f, 1f)] private float overlayAlphaAtEnd = 0.18f;
    [SerializeField] private Color cameraBackgroundNight = new Color(0.04f, 0.06f, 0.09f, 1f);
    [SerializeField] private float localLightMultiplierAtStart = 1f;
    [SerializeField] private float localLightMultiplierAtEnd = 1.12f;
    [SerializeField] private Color shadowColor = new Color(0.03f, 0.04f, 0.06f, 0.32f);
    [SerializeField] private Vector3 shadowOffset = new Vector3(0.1f, -0.16f, 0f);
    [SerializeField] private Vector3 shadowScale = new Vector3(1.02f, 0.36f, 1f);

    public string TargetSceneName => targetSceneName;

    public SceneNightProfile CreateProfile(string fallbackSceneName)
    {
        string sceneName = string.IsNullOrWhiteSpace(targetSceneName) ? fallbackSceneName : targetSceneName;
        return new SceneNightProfile(
            sceneName,
            useCountdownProgress,
            fixedNightProgress,
            overlayTint,
            overlayAlphaAtStart,
            overlayAlphaAtEnd,
            cameraBackgroundNight,
            localLightMultiplierAtStart,
            localLightMultiplierAtEnd,
            shadowColor,
            shadowOffset,
            shadowScale);
    }
}
