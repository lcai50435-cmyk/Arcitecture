using UnityEngine;

public class GameCountDownManager : MonoBehaviour
{
    [Header("总倒计时时长")]
    public float totalTime = 120f;
    [Header("是否在基地内(暂停倒计时)")]
    public bool isInBase = true;

    private float _currentTime;
    public static GameCountDownManager Instance;

    void Awake()
    {
        // 单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _currentTime = totalTime;
    }

    void Update()
    {
        // 在基地：不扣时间
        if (isInBase) return;

        // 离开基地：倒计时运行
        if (_currentTime > 0)
        {
            Debug.Log(_currentTime);
            _currentTime -= Time.deltaTime;
        }
        else
        {
            // 倒计时归零 → 游戏结束
            GameOver();
        }
    }

    // 游戏结束逻辑
    void GameOver()
    {
        Debug.Log("倒计时归零，游戏结束！");
        // 弹窗、暂停游戏、切换场景
        Time.timeScale = 0;
    }

    // 外部设置是否在基地
    public void SetInBaseState(bool state)
    {
        isInBase = state;
    }

    // 查看剩余时间
    public float GetRemainTime()
    {
        return Mathf.Max(_currentTime, 0);
    }
}