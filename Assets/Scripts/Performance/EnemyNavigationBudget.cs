using UnityEngine;

public static class EnemyNavigationBudget
{
    private static int mBudgetFrame = -1;
    private static int mRequestsThisFrame;

    public static bool TryConsumePathRequest()
    {
        int frame = Time.frameCount;
        if (mBudgetFrame != frame)
        {
            mBudgetFrame = frame;
            mRequestsThisFrame = 0;
        }

        int limit = GameplayPerformanceSettings.Profile.MaxPathRequestsPerFrame;
        if (mRequestsThisFrame >= limit)
        {
            return false;
        }

        mRequestsThisFrame++;
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetRuntimeState()
    {
        mBudgetFrame = -1;
        mRequestsThisFrame = 0;
    }
}
