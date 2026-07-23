using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class EnemyNavigationSceneContext
{
    private static int mSceneHandle = int.MinValue;
    private static Tilemap[] mTilemaps;
    private static Collider2D[] mColliders;
    private static GridLayout[] mGrids;

    public static Tilemap[] Tilemaps
    {
        get
        {
            ensureScanned();
            return mTilemaps;
        }
    }

    public static Collider2D[] Colliders
    {
        get
        {
            ensureScanned();
            return mColliders;
        }
    }

    public static GridLayout[] Grids
    {
        get
        {
            ensureScanned();
            return mGrids;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetRuntimeState()
    {
        mSceneHandle = int.MinValue;
        mTilemaps = null;
        mColliders = null;
        mGrids = null;
    }

    private static void ensureScanned()
    {
        int sceneHandle = SceneManager.GetActiveScene().handle;
        if (mSceneHandle == sceneHandle && mTilemaps != null && mColliders != null && mGrids != null)
        {
            return;
        }

        mSceneHandle = sceneHandle;
        mTilemaps = Object.FindObjectsOfType<Tilemap>(true);
        mColliders = Object.FindObjectsOfType<Collider2D>(true);
        mGrids = Object.FindObjectsOfType<GridLayout>(true);
    }
}
