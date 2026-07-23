using UnityEngine;

[CreateAssetMenu(
    fileName = "GameplayPerformanceProfile",
    menuName = "Architecture/Performance/Gameplay Performance Profile")]
public sealed class GameplayPerformanceProfile : ScriptableObject
{
    [Header("Frame Budget")]
    [SerializeField, Min(30)] private int mTargetFrameRate = 60;

    [Header("Enemy AI")]
    [SerializeField, Min(0.02f)] private float mEnemyDecisionInterval = 0.1f;
    [SerializeField, Min(0.05f)] private float mMinimumRepathInterval = 0.35f;
    [SerializeField, Min(1)] private int mMaxPathRequestsPerFrame = 2;

    [Header("Object Pools")]
    [SerializeField, Min(1)] private int mPrefabPoolDefaultCapacity = 16;
    [SerializeField, Min(8)] private int mPrefabPoolMaxSize = 128;
    [SerializeField, Min(1)] private int mRuntimeFxPoolDefaultCapacity = 24;
    [SerializeField, Min(8)] private int mRuntimeFxPoolMaxSize = 96;

    [Header("Visual Effects")]
    [SerializeField, Min(0)] private int mMaxTransientLights = 8;
    [SerializeField, Min(1)] private int mStressEnemyCount = 20;

    public int TargetFrameRate => Mathf.Max(30, mTargetFrameRate);
    public float EnemyDecisionInterval => Mathf.Max(0.02f, mEnemyDecisionInterval);
    public float MinimumRepathInterval => Mathf.Max(0.05f, mMinimumRepathInterval);
    public int MaxPathRequestsPerFrame => Mathf.Max(1, mMaxPathRequestsPerFrame);
    public int PrefabPoolDefaultCapacity => Mathf.Max(1, mPrefabPoolDefaultCapacity);
    public int PrefabPoolMaxSize => Mathf.Max(PrefabPoolDefaultCapacity, mPrefabPoolMaxSize);
    public int RuntimeFxPoolDefaultCapacity => Mathf.Max(1, mRuntimeFxPoolDefaultCapacity);
    public int RuntimeFxPoolMaxSize => Mathf.Max(RuntimeFxPoolDefaultCapacity, mRuntimeFxPoolMaxSize);
    public int MaxTransientLights => Mathf.Max(0, mMaxTransientLights);
    public int StressEnemyCount => Mathf.Max(1, mStressEnemyCount);
}

public static class GameplayPerformanceSettings
{
    private const string PROFILE_RESOURCE_PATH = "Config/GameplayPerformanceProfile";

    private static GameplayPerformanceProfile mProfile;

    public static GameplayPerformanceProfile Profile
    {
        get
        {
            if (mProfile == null)
            {
                mProfile = Resources.Load<GameplayPerformanceProfile>(PROFILE_RESOURCE_PATH);
            }

            if (mProfile == null)
            {
                mProfile = ScriptableObject.CreateInstance<GameplayPerformanceProfile>();
                mProfile.hideFlags = HideFlags.HideAndDontSave;
            }

            return mProfile;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetRuntimeState()
    {
        mProfile = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void applyFrameBudget()
    {
        Application.targetFrameRate = Profile.TargetFrameRate;
    }
}
