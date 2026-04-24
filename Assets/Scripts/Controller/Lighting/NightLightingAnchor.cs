using UnityEngine;

[DisallowMultipleComponent]
public sealed class NightLightingAnchor : MonoBehaviour
{
    [SerializeField] private Color lightColor = new Color(1f, 0.82f, 0.58f, 1f);
    [SerializeField] private float radius = 1.6f;
    [SerializeField] private float baseIntensity = 0.08f;
    [SerializeField] private float nightBoost = 0.03f;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private bool scaleWithSceneLightMultiplier = true;
    [SerializeField] private NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource;
    [SerializeField] private bool attachProjectedShadow = false;

    public void ApplyLighting()
    {
        NightLightingController.EnsureLocalLight(
            gameObject,
            radius,
            baseIntensity,
            nightBoost,
            localOffset,
            lightColor,
            scaleWithSceneLightMultiplier,
            sortingMode);

        if (attachProjectedShadow)
        {
            NightLightingController.EnsureProjectedShadow(gameObject);
        }
    }
}
