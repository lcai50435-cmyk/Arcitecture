using System.Runtime.InteropServices;

public static class WebGLPersistentFileSystemBridge
{
#if UNITY_INCLUDE_TESTS
    private static int syncRequestCountForTests;

    public static int SyncRequestCountForTests => syncRequestCountForTests;

    public static void ResetSyncRequestCountForTests()
    {
        syncRequestCountForTests = 0;
    }
#endif

    public static void RequestSync()
    {
#if UNITY_INCLUDE_TESTS
        syncRequestCountForTests++;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        ArcitectureSyncPersistentFileSystem();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int ArcitectureSyncPersistentFileSystem();
#endif
}
