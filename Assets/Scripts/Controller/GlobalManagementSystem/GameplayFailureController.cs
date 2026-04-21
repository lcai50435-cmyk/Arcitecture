using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameplayFailureReason
{
    PlayerDeath,
    TimeExpired
}

public class GameplayFailureController : MonoBehaviour
{
    private const string DefaultGameOverSceneName = "DeadScene";
    private const float DropScatterDuration = 0.72f;
    private const float DropScatterDelayStep = 0.04f;
    private const float DropScatterMinDistance = 0.7f;
    private const float DropScatterMaxDistance = 1.45f;
    private const float DropScatterArcHeight = 0.55f;
    private const float SceneTransitionDelay = 0.16f;

    public static GameplayFailureController Instance { get; private set; }

    private bool isFailureActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance(scene);
    }

    public static bool TryTriggerFailure(
        GameplayFailureReason reason,
        string gameOverSceneName = DefaultGameOverSceneName)
    {
        GameplayFailureController controller = EnsureInstance(SceneManager.GetActiveScene());
        return controller != null && controller.TryStartFailure(reason, gameOverSceneName);
    }

    private static GameplayFailureController EnsureInstance(Scene scene)
    {
        if (!GameplayStageCatalog.IsGameplayScene(scene.name))
        {
            return null;
        }

        if (Instance != null)
        {
            return Instance;
        }

        GameplayFailureController existing = FindObjectOfType<GameplayFailureController>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject runtimeObject = new GameObject("GameplayFailureController");
        Instance = runtimeObject.AddComponent<GameplayFailureController>();
        return Instance;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool TryStartFailure(GameplayFailureReason reason, string gameOverSceneName)
    {
        if (isFailureActive)
        {
            return true;
        }

        isFailureActive = true;
        StartCoroutine(HandleFailureRoutine(reason, string.IsNullOrWhiteSpace(gameOverSceneName) ? DefaultGameOverSceneName : gameOverSceneName));
        return true;
    }

    private IEnumerator HandleFailureRoutine(GameplayFailureReason reason, string gameOverSceneName)
    {
        Time.timeScale = 0f;
        HideGameplayUi();
        DisablePlayerControls();

        GameCountDownManager countdownManager = GameCountDownManager.Instance != null
            ? GameCountDownManager.Instance
            : FindObjectOfType<GameCountDownManager>();
        countdownManager?.SetInBaseState(true);

        RunStageDirector director = FindObjectOfType<RunStageDirector>();
        director?.SuspendRuntime();

        BackpackMananger backpack = BackpackMananger.Instance;
        List<ArchitecturalCrystal> droppedItems = SnapshotBackpackItems(backpack);
        Vector3 dropOrigin = ResolveDropOrigin();
        float waitDuration = PlayDropScatterAnimation(droppedItems, dropOrigin);

        if (waitDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(waitDuration);
        }

        if (backpack != null)
        {
            backpack.ClearAllItems();
        }

        yield return new WaitForSecondsRealtime(SceneTransitionDelay);

        Time.timeScale = 1f;

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(gameOverSceneName);
            yield break;
        }

        SceneManager.LoadScene(gameOverSceneName);
    }

    private static void HideGameplayUi()
    {
        if (UIRootManager.Instance == null)
        {
            return;
        }

        UIRootManager.Instance.HideHandbook();
        UIRootManager.Instance.HideAllDetail();
        UIRootManager.Instance.HideAllSubmitSelection();
        UIRootManager.Instance.HideDialog();
        UIRootManager.Instance.HideInteractTip();
        UIRootManager.Instance.HideBackpack();
    }

    private static void DisablePlayerControls()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        PlayerMove move = playerObject.GetComponent<PlayerMove>();
        if (move != null)
        {
            move.canMove = false;
            if (move.rb != null)
            {
                move.rb.velocity = Vector2.zero;
            }
        }

        PlayerAttack attack = playerObject.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.enabled = false;
        }

        PlayerInteraction interaction = playerObject.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.ClearCurrentInteractable();
            interaction.enabled = false;
        }
    }

    private static List<ArchitecturalCrystal> SnapshotBackpackItems(BackpackMananger backpack)
    {
        List<ArchitecturalCrystal> crystals = new List<ArchitecturalCrystal>();
        if (backpack == null || backpack.backpackItems == null)
        {
            return crystals;
        }

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            ArchitecturalCrystal? nullableItem = backpack.backpackItems[i];
            if (nullableItem.HasValue)
            {
                crystals.Add(nullableItem.Value);
            }
        }

        return crystals;
    }

    private static Vector3 ResolveDropOrigin()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Vector3 position = playerObject.transform.position;
            position.z = 0f;
            return position;
        }

        return Vector3.zero;
    }

    private float PlayDropScatterAnimation(List<ArchitecturalCrystal> droppedItems, Vector3 origin)
    {
        if (droppedItems == null || droppedItems.Count == 0)
        {
            return 0f;
        }

        float maxDuration = 0f;
        float angleStep = 360f / Mathf.Max(1, droppedItems.Count);

        for (int i = 0; i < droppedItems.Count; i++)
        {
            ArchitecturalCrystal crystal = droppedItems[i];
            float angle = angleStep * i + Random.Range(-18f, 18f);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            float distance = Random.Range(DropScatterMinDistance, DropScatterMaxDistance);
            Vector3 targetPosition = origin + (Vector3)(direction.normalized * distance);
            float startDelay = i * DropScatterDelayStep;
            float duration = DropScatterDuration + startDelay;

            GameObject dropObject = RuntimeCrystalDropFactory.CreateVisualDrop(
                crystal,
                origin,
                0.3f,
                8,
                transform,
                $"FailureDrop_{crystal.DisplayName}_{i}");

            StartCoroutine(AnimateVisualDrop(dropObject, origin, targetPosition, startDelay));
            maxDuration = Mathf.Max(maxDuration, duration);
        }

        return maxDuration;
    }

    private IEnumerator AnimateVisualDrop(GameObject dropObject, Vector3 startPosition, Vector3 targetPosition, float startDelay)
    {
        if (dropObject == null)
        {
            yield break;
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        SpriteRenderer renderer = dropObject.GetComponent<SpriteRenderer>();
        Transform dropTransform = dropObject.transform;
        Vector3 baseScale = dropTransform.localScale;
        float elapsed = 0f;

        while (elapsed < DropScatterDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / DropScatterDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            Vector3 position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            position.y += Mathf.Sin(easedProgress * Mathf.PI) * DropScatterArcHeight;
            dropTransform.position = position;
            dropTransform.localScale = baseScale * (1f + Mathf.Sin(easedProgress * Mathf.PI) * 0.12f);

            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = progress < 0.6f
                    ? 1f
                    : Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f);
                renderer.color = color;
            }

            yield return null;
        }

        Destroy(dropObject);
    }
}
