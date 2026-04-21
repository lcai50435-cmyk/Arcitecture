using TMPro;
using UnityEngine;

public class GameCountDownManager : MonoBehaviour
{
    [Header("总倒计时时间")]
    public float totalTime = 300f;
    [Header("是否在基地内，暂停倒计时")]
    public bool isInBase = true;
    [Header("倒计时文本")]
    public TextMeshProUGUI timer;

    private float currentTime;
    public static GameCountDownManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentTime = totalTime;
        RefreshTimerText();
    }

    private void Update()
    {
        if (isInBase) return;

        if (currentTime > 0f)
        {
            currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
            RefreshTimerText();

            if (currentTime <= 0f)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        Debug.Log("倒计时归零，游戏结束");
        RefreshTimerText();
    }

    public void SetInBaseState(bool state)
    {
        isInBase = state;
    }

    public float GetRemainTime()
    {
        return Mathf.Max(currentTime, 0f);
    }

    public void DebugAddRemainTime(float seconds)
    {
        DebugSetRemainTime(currentTime + seconds);
    }

    public void DebugSetRemainTime(float seconds)
    {
        currentTime = Mathf.Max(0f, seconds);
        RefreshTimerText();
    }

    private void RefreshTimerText()
    {
        if (timer == null) return;

        int min = Mathf.FloorToInt(currentTime / 60f);
        int sec = Mathf.FloorToInt(currentTime % 60f);
        string value = $"{min}:{sec:00}";
        timer.text = currentTime <= 60f
            ? $"<color=red>{value}</color>"
            : value;
    }
}
