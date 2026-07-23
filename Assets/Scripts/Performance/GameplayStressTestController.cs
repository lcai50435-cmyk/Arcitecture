using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayStressTestController : MonoBehaviour
{
    [SerializeField] private RunStageDirector mRunStageDirector;
    [SerializeField] private bool mbSpawnOnStart;
    [SerializeField] private KeyCode mSpawnKey = KeyCode.F8;
    [SerializeField, Min(1)] private int mEnemyCount = 20;

    private void Start()
    {
        resolveDirector();
        if (mbSpawnOnStart)
        {
            SpawnStressWave();
        }
    }

    private void Update()
    {
        if (mSpawnKey != KeyCode.None && Input.GetKeyDown(mSpawnKey))
        {
            SpawnStressWave();
        }
    }

    [ContextMenu("Spawn Stress Wave")]
    public void SpawnStressWave()
    {
        resolveDirector();
        if (mRunStageDirector == null)
        {
            Debug.LogWarning("未找到 RunStageDirector，无法生成压力测试怪物。", this);
            return;
        }

        int count = Mathf.Max(1, mEnemyCount);
        mRunStageDirector.DebugSpawnEnemy(null, count);
    }

    public void ApplyProfileDefaults()
    {
        mEnemyCount = GameplayPerformanceSettings.Profile.StressEnemyCount;
    }

    private void resolveDirector()
    {
        if (mRunStageDirector == null)
        {
            mRunStageDirector = FindObjectOfType<RunStageDirector>();
        }
    }
}
