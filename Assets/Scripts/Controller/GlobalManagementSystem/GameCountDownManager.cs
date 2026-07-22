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
    private bool hasFinished;
    private CountdownRollingDisplay rollingDisplay;
    public static GameCountDownManager Instance;

    public event System.Action<float> OnRemainingTimeChanged;
    public event System.Action OnCountdownFinished;

    public float CurrentTime => currentTime;
    public bool HasFinished => hasFinished;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        timer = GameplayStatusHudRuntime.EnsureCountdownText(timer);
        currentTime = totalTime;
        hasFinished = false;
        RefreshTimerText();
        OnRemainingTimeChanged?.Invoke(currentTime);
    }

    private void Update()
    {
        if (isInBase || hasFinished) return;

        if (currentTime > 0f)
        {
            currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
            RefreshTimerText();
            OnRemainingTimeChanged?.Invoke(currentTime);

            if (currentTime <= 0f)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;
        Debug.Log("倒计时归零，游戏结束");
        RefreshTimerText();
        OnCountdownFinished?.Invoke();
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
        hasFinished = false;
        RefreshTimerText();
        OnRemainingTimeChanged?.Invoke(currentTime);

        if (currentTime <= 0f)
        {
            GameOver();
        }
    }

    private void RefreshTimerText()
    {
        timer = GameplayStatusHudRuntime.EnsureCountdownText(timer);
        if (timer == null)
        {
            return;
        }

        int min = Mathf.FloorToInt(currentTime / 60f);
        int sec = Mathf.FloorToInt(currentTime % 60f);
        string value = $"{min}:{sec:00}";
        bool isDangerState = currentTime <= 60f;

        CountdownRollingDisplay display = EnsureRollingDisplay();
        if (display != null)
        {
            try
            {
                display.SetDisplay(value, isDangerState);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"倒计时滚动显示初始化失败，回退到普通文本。{ex.Message}");
                display.UseFallbackText(value, isDangerState);
                display.enabled = false;
                rollingDisplay = null;
            }
        }

        timer.color = isDangerState ? new Color(1f, 0.36f, 0.34f, 1f) : Color.white;
        timer.text = value;
    }

    private CountdownRollingDisplay EnsureRollingDisplay()
    {
        if (timer == null)
        {
            return null;
        }

        CountdownRollingDisplay existing = timer.GetComponent<CountdownRollingDisplay>();
        if (existing != null && !existing.enabled)
        {
            return null;
        }

        if (rollingDisplay != null && rollingDisplay.enabled && rollingDisplay.gameObject == timer.gameObject)
        {
            return rollingDisplay;
        }

        if (existing != null && existing.enabled)
        {
            rollingDisplay = existing;
            return rollingDisplay;
        }

        try
        {
            rollingDisplay = CountdownRollingDisplay.GetOrCreate(timer);
            return rollingDisplay;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"创建倒计时滚动显示组件失败，回退到普通文本。{ex.Message}");
            rollingDisplay = null;
            return null;
        }
    }
}
