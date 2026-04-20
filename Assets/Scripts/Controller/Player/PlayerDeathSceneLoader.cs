using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家死亡后切换到 GameOver 场景
/// </summary>
public class PlayerDeathSceneLoader : MonoBehaviour
{
    [Header("玩家生命核心")]
    public CharacterCore characterCore;

    [Header("死亡后跳转的场景名")]
    public string gameOverSceneName = "GameOverScene";

    private void Awake()
    {
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }
    }

    private void OnEnable()
    {
        if (characterCore != null)
        {
            characterCore.OnDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (characterCore != null)
        {
            characterCore.OnDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("玩家死亡，进入 GameOver 场景");

        Time.timeScale = 1f; // 防止你之前暂停过时间，导致新场景按钮不能点
        SceneManager.LoadScene(gameOverSceneName);
    }
}