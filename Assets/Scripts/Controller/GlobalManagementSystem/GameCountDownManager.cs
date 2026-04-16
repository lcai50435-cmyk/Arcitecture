using TMPro;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class GameCountDownManager : MonoBehaviour
{
    [Header("总倒计时时长")]
    public float totalTime = 300f;
    [Header("是否在基地内(暂停倒计时)")]
    public bool isInBase = true;
    [Header("倒计时组件")]
    public TextMeshProUGUI timer;

    private float currentTime;
    public static GameCountDownManager Instance;

    void Awake()
    {
        // 单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentTime = totalTime;
    }

    void Update()
    {
        // 不扣时间
        if (isInBase) return;

        // 倒计时运行
        if (currentTime > 0)
        {          
            currentTime -= Time.deltaTime;
           
            // 格式化 0:00
            int min = Mathf.FloorToInt(currentTime / 60);
            int sec = Mathf.FloorToInt(currentTime % 60);
            timer.text = $"{min}:{sec:00}";

            // 时间小于60秒 则 变红
            if (currentTime <= 60)
            {
                timer.text = $"<color=red>{min}:{sec:00}</color>";
            }

        }
        else
        {
            // 倒计时归零 则 游戏结束
            GameOver();
        }
    }

    // 游戏结束逻辑
    void GameOver()
    {
        Debug.Log("倒计时归零，游戏结束！");
        // 弹窗、暂停游戏、切换场景
        // currentTime = 0;
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
        return Mathf.Max(currentTime, 0);
    }
}