using UnityEngine;

[DisallowMultipleComponent]
public sealed class V2SceneProfile : MonoBehaviour
{
    [Header("Pixel Ink Art Direction")]
    [SerializeField] private Color mInkColor = new Color(0.10f, 0.10f, 0.12f, 1f);
    [SerializeField] private Color mPaperColor = new Color(0.88f, 0.82f, 0.66f, 1f);
    [SerializeField] private Color mEarthColor = new Color(0.55f, 0.35f, 0.20f, 1f);
    [SerializeField] private Color mDangerColor = new Color(0.72f, 0.16f, 0.12f, 1f);
    [SerializeField, Min(1)] private int mReferencePixelsPerUnit = 100;

    public Color InkColor => mInkColor;
    public Color PaperColor => mPaperColor;
    public Color EarthColor => mEarthColor;
    public Color DangerColor => mDangerColor;
    public int ReferencePixelsPerUnit => Mathf.Max(1, mReferencePixelsPerUnit);
}
