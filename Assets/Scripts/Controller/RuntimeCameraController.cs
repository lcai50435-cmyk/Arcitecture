using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RuntimeCameraController : MonoBehaviour
{
    private const string ControllerObjectName = "RuntimeCameraController";
    private const string BaseSceneName = "BaseScene";

    private const float GameplayFollowSmoothBase = 0.12f;
    private const float GameplayFollowSmoothHighRisk = 0.09f;
    private const float BaseFollowSmooth = 0.18f;

    private const float GameplayLookAheadBase = 0.55f;
    private const float GameplayLookAheadHighRisk = 0.68f;
    private const float BaseLookAhead = 0.32f;
    private const float LookAheadMoveSmooth = 0.10f;
    private const float LookAheadReturnSmooth = 0.16f;

    private const float BaseFocusOffsetRatio = 0.14f;
    private const float BaseFocusZoomMultiplier = 0.93f;
    private const float BaseFocusEnterDuration = 0.22f;
    private const float BaseFocusExitDuration = 0.18f;

    private const float AttackPunchDistance = 0.18f;
    private const float AttackPunchOutDuration = 0.08f;
    private const float AttackPunchReturnDuration = 0.14f;

    private const float DamageShakeDuration = 0.18f;
    private const float DamageShakeMinAmplitude = 0.08f;
    private const float DamageShakeMaxAmplitude = 0.22f;
    private const float DamageThrottleDuration = 0.10f;
    private const float DamageRecoilScale = 0.46f;

    private const float MidDangerTension = 0.45f;
    private const float HighDangerTension = 1f;
    private const float MidDangerZoomReduction = 0.02f;
    private const float HighDangerZoomReduction = 0.05f;
    private const float DangerTensionLerpSpeed = 2.5f;
    private const float DamageNoiseFrequency = 32f;

    private static RuntimeCameraController instance;
    private static bool sceneHookRegistered;

    private Camera controlledCamera;
    private Transform followTarget;
    private Rigidbody2D followBody;
    private DirectionTracker directionTracker;
    private CharacterCore followCharacterCore;
    private CharacterAttack followCharacterAttack;
    private Transform hubFocusTarget;

    private Vector3 defaultWorldOffset = new Vector3(0f, 0f, -10f);
    private bool hasCapturedDefaultOffset;
    private bool hasDetachedCamera;
    private bool hasSmoothedFollowPose;
    private bool hasSmoothedSize;
    private bool wasExternallyDriven;
    private bool hasAdoptedExternalPose;

    private Vector3 smoothedFollowPosition;
    private Vector3 followVelocity;
    private Vector3 lookAheadOffset;
    private Vector3 lookAheadVelocity;
    private float smoothedOrthographicSize;
    private float orthographicSizeVelocity;
    private float focusWeight;
    private float currentDangerTension;
    private float targetDangerTension;

    private Vector2 lastAttackFacing = Vector2.up;
    private float attackPulseElapsed = float.PositiveInfinity;
    private float damagePulseElapsed = float.PositiveInfinity;
    private float damagePulseAmplitude;
    private Vector2 damagePulseDirection = Vector2.zero;
    private float nextDamageAllowedTime;
    private float damageNoiseSeedX;
    private float damageNoiseSeedY;

    private string activeSceneName;

    public static RuntimeCameraController EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        RuntimeCameraController existing = FindObjectOfType<RuntimeCameraController>(true);
        if (existing != null)
        {
            instance = existing;
            existing.EnsureSceneHook();
            return existing;
        }

        GameObject controllerObject = new GameObject(ControllerObjectName);
        instance = controllerObject.AddComponent<RuntimeCameraController>();
        instance.EnsureSceneHook();
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (sceneHookRegistered)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneHookRegistered = false;
        }

        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        RuntimeCameraController controller = EnsureInstance();
        controller.HandleSceneChanged(SceneManager.GetActiveScene().name);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RuntimeCameraController controller = EnsureInstance();
        controller.HandleSceneChanged(scene.name);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSceneHook();
        damageNoiseSeedX = Random.Range(0f, 1000f);
        damageNoiseSeedY = Random.Range(1000f, 2000f);
        HandleSceneChanged(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnbindCharacterHooks();
    }

    public void BindFollowTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (followTarget == target && controlledCamera != null)
        {
            RebindTargetComponents(target);
            if (ShouldDeferDefaultOffsetCapture())
            {
                return;
            }

            CaptureDefaultOffsetIfNeeded(true);
            return;
        }

        followTarget = target;
        RebindTargetComponents(target);
        ResolveCameraIfNeeded();
        if (ShouldDeferDefaultOffsetCapture())
        {
            return;
        }

        CaptureDefaultOffsetIfNeeded(true);
        SnapToDesiredPose();
    }

    public void SetHubFocusTarget(Transform target)
    {
        hubFocusTarget = target;
    }

    public void ClearHubFocus()
    {
        hubFocusTarget = null;
    }

    public void NotifyPlayerAttack(Vector2 facing)
    {
        if (!IsGameplayScene() || !ShouldAllowRuntimeCameraMotion())
        {
            return;
        }

        if (facing.sqrMagnitude <= 0.0001f)
        {
            facing = ResolveFacingDirection();
        }

        lastAttackFacing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.up;
        attackPulseElapsed = 0f;
    }

    public void NotifyPlayerDamaged(float normalizedDamage)
    {
        if (!IsGameplayScene() || !ShouldAllowRuntimeCameraMotion())
        {
            return;
        }

        if (Time.unscaledTime < nextDamageAllowedTime)
        {
            return;
        }

        float clampedDamage = Mathf.Clamp01(normalizedDamage);
        damagePulseAmplitude = Mathf.Lerp(DamageShakeMinAmplitude, DamageShakeMaxAmplitude, clampedDamage);
        damagePulseDirection = -ResolveFacingDirection();
        if (damagePulseDirection.sqrMagnitude <= 0.0001f)
        {
            damagePulseDirection = Vector2.down;
        }

        damagePulseDirection.Normalize();
        damagePulseElapsed = 0f;
        nextDamageAllowedTime = Time.unscaledTime + DamageThrottleDuration;
    }

    public void SetDangerTension(float value)
    {
        targetDangerTension = Mathf.Clamp01(value);
    }

    public bool AdoptCurrentCameraPose()
    {
        if (!IsSupportedScene())
        {
            return false;
        }

        ResolveSceneBindingsIfNeeded();
        if (controlledCamera == null || followTarget == null)
        {
            return false;
        }

        Vector3 adoptedPosition = controlledCamera.transform.position;
        float adoptedSize = controlledCamera.orthographicSize;
        Vector3 adoptedOffset = adoptedPosition - followTarget.position;
        if (Mathf.Abs(adoptedOffset.z) <= 0.01f)
        {
            adoptedOffset.z = controlledCamera.transform.position.z;
        }

        defaultWorldOffset = adoptedOffset;
        hasCapturedDefaultOffset = true;
        smoothedFollowPosition = adoptedPosition;
        smoothedOrthographicSize = adoptedSize;
        hasSmoothedFollowPose = true;
        hasSmoothedSize = true;
        followVelocity = Vector3.zero;
        lookAheadOffset = Vector3.zero;
        lookAheadVelocity = Vector3.zero;
        orthographicSizeVelocity = 0f;
        ClearTransientMotion();
        hasAdoptedExternalPose = true;
        wasExternallyDriven = true;
        return true;
    }

    public void SnapToDesiredPose()
    {
        ResolveCameraIfNeeded();
        if (controlledCamera == null || followTarget == null || !IsSupportedScene())
        {
            return;
        }

        Vector3 basePosition = ResolveBaseFollowPosition(0f, true);
        float baseSize = ResolveBaseOrthographicSize();
        float desiredSize = ResolveDesiredOrthographicSize(baseSize, true);

        smoothedFollowPosition = basePosition;
        smoothedOrthographicSize = desiredSize;
        followVelocity = Vector3.zero;
        lookAheadVelocity = Vector3.zero;
        orthographicSizeVelocity = 0f;
        hasSmoothedFollowPose = true;
        hasSmoothedSize = true;

        controlledCamera.orthographicSize = desiredSize;
        controlledCamera.transform.position = SnapCameraPosition(basePosition, desiredSize);
    }

    private void LateUpdate()
    {
        if (!IsSupportedScene())
        {
            return;
        }

        ResolveSceneBindingsIfNeeded();
        if (controlledCamera == null || followTarget == null)
        {
            return;
        }

        bool externallyDriven = IsExternallyDriven();
        if (externallyDriven)
        {
            if (!wasExternallyDriven)
            {
                ClearTransientMotion();
                wasExternallyDriven = true;
            }

            return;
        }

        if (wasExternallyDriven)
        {
            wasExternallyDriven = false;
            ClearTransientMotion();
            if (ConsumeAdoptedExternalPose())
            {
                return;
            }

            CaptureDefaultOffsetIfNeeded(true);
            SnapToDesiredPose();
            return;
        }

        SetDangerTension(IsGameplayScene() ? RunStageDirector.ActiveCameraTension : 0f);
        float deltaTime = Time.unscaledDeltaTime;
        currentDangerTension = Mathf.MoveTowards(
            currentDangerTension,
            targetDangerTension,
            deltaTime * DangerTensionLerpSpeed);

        if (ShouldFreezeGameplayCamera())
        {
            return;
        }

        Vector3 basePosition = ResolveBaseFollowPosition(deltaTime, false);
        float baseSize = ResolveBaseOrthographicSize();
        float desiredSize = ResolveDesiredOrthographicSize(baseSize, false);

        if (!hasSmoothedFollowPose)
        {
            smoothedFollowPosition = basePosition;
            hasSmoothedFollowPose = true;
        }
        else
        {
            smoothedFollowPosition = Vector3.SmoothDamp(
                smoothedFollowPosition,
                basePosition,
                ref followVelocity,
                ResolveFollowSmoothTime(),
                Mathf.Infinity,
                deltaTime);
        }

        if (!hasSmoothedSize)
        {
            smoothedOrthographicSize = desiredSize;
            hasSmoothedSize = true;
        }
        else
        {
            smoothedOrthographicSize = Mathf.SmoothDamp(
                smoothedOrthographicSize,
                desiredSize,
                ref orthographicSizeVelocity,
                ResolveFollowSmoothTime(),
                Mathf.Infinity,
                deltaTime);
        }

        controlledCamera.orthographicSize = smoothedOrthographicSize;
        controlledCamera.transform.position = SnapCameraPosition(
            smoothedFollowPosition + EvaluateTransientOffset(deltaTime),
            smoothedOrthographicSize);
    }

    private void EnsureSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private void HandleSceneChanged(string sceneName)
    {
        activeSceneName = sceneName;
        controlledCamera = null;
        hasCapturedDefaultOffset = false;
        hasDetachedCamera = false;
        hasSmoothedFollowPose = false;
        hasSmoothedSize = false;
        followVelocity = Vector3.zero;
        lookAheadOffset = Vector3.zero;
        lookAheadVelocity = Vector3.zero;
        orthographicSizeVelocity = 0f;
        hubFocusTarget = null;
        focusWeight = 0f;
        currentDangerTension = 0f;
        targetDangerTension = 0f;
        ClearTransientMotion();
        wasExternallyDriven = false;
        hasAdoptedExternalPose = false;

        if (followTarget == null || !followTarget || followTarget.gameObject.scene.name != sceneName)
        {
            followTarget = null;
            RebindTargetComponents(null);
        }
        else
        {
            RebindTargetComponents(followTarget);
        }
    }

    private void ResolveSceneBindingsIfNeeded()
    {
        ResolveCameraIfNeeded();

        if ((followTarget == null || !followTarget) && IsSupportedScene())
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                BindFollowTarget(playerObject.transform);
            }
        }

        CaptureDefaultOffsetIfNeeded(false);
    }

    private void ResolveCameraIfNeeded()
    {
        if (controlledCamera != null && controlledCamera.isActiveAndEnabled)
        {
            return;
        }

        controlledCamera = Camera.main;
        if (controlledCamera == null || !controlledCamera.isActiveAndEnabled)
        {
            Camera[] cameras = FindObjectsOfType<Camera>(true);
            float highestDepth = float.MinValue;
            Camera fallback = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                if (candidate.depth < highestDepth)
                {
                    continue;
                }

                highestDepth = candidate.depth;
                fallback = candidate;
            }

            controlledCamera = fallback;
        }

        if (controlledCamera == null || !IsSupportedScene())
        {
            return;
        }

        if (controlledCamera.transform.parent != null && !hasDetachedCamera)
        {
            controlledCamera.transform.SetParent(null, true);
            hasDetachedCamera = true;
        }
    }

    private void CaptureDefaultOffsetIfNeeded(bool forceRefresh)
    {
        if (controlledCamera == null || followTarget == null)
        {
            return;
        }

        if (hasCapturedDefaultOffset && !forceRefresh)
        {
            return;
        }

        if (ShouldDeferDefaultOffsetCapture())
        {
            return;
        }

        Vector3 currentOffset = controlledCamera.transform.position - followTarget.position;
        if (Mathf.Abs(currentOffset.z) <= 0.01f)
        {
            currentOffset.z = controlledCamera.transform.position.z;
        }

        defaultWorldOffset = currentOffset;
        hasCapturedDefaultOffset = true;
    }

    private void RebindTargetComponents(Transform target)
    {
        UnbindCharacterHooks();

        followBody = null;
        directionTracker = null;
        followCharacterCore = null;
        followCharacterAttack = null;

        if (target == null)
        {
            return;
        }

        followBody = target.GetComponent<Rigidbody2D>();
        directionTracker = target.GetComponent<DirectionTracker>();
        followCharacterCore = target.GetComponent<CharacterCore>();
        followCharacterAttack = target.GetComponent<CharacterAttack>();

        if (followCharacterCore != null)
        {
            followCharacterCore.OnTakeDamageWithValue += HandlePlayerDamageValue;
            followCharacterCore.OnDeath += HandlePlayerDeath;
        }

        if (followCharacterAttack != null)
        {
            followCharacterAttack.OnAttackStarted += HandlePlayerAttackStarted;
        }
    }

    private void UnbindCharacterHooks()
    {
        if (followCharacterCore != null)
        {
            followCharacterCore.OnTakeDamageWithValue -= HandlePlayerDamageValue;
            followCharacterCore.OnDeath -= HandlePlayerDeath;
        }

        if (followCharacterAttack != null)
        {
            followCharacterAttack.OnAttackStarted -= HandlePlayerAttackStarted;
        }
    }

    private void HandlePlayerAttackStarted()
    {
        NotifyPlayerAttack(ResolveFacingDirection());
    }

    private void HandlePlayerDamageValue(float damage)
    {
        if (followCharacterCore == null || followCharacterCore.stats == null)
        {
            return;
        }

        float maxHp = Mathf.Max(1f, followCharacterCore.stats.maxHp);
        NotifyPlayerDamaged(damage / maxHp);
    }

    private void HandlePlayerDeath()
    {
        ClearTransientMotion();
    }

    private Vector3 ResolveBaseFollowPosition(float deltaTime, bool instant)
    {
        Vector3 targetPosition = followTarget.position + defaultWorldOffset;
        targetPosition += ResolveLookAheadOffset(deltaTime, instant);
        targetPosition += ResolveHubFocusOffset(deltaTime, instant);
        return targetPosition;
    }

    private Vector3 ResolveLookAheadOffset(float deltaTime, bool instant)
    {
        Vector2 moveVector = followBody != null && followBody.simulated
            ? followBody.velocity
            : Vector2.zero;
        bool isMoving = moveVector.sqrMagnitude > 0.0004f;

        float lookAheadMagnitude = IsGameplayScene()
            ? Mathf.Lerp(GameplayLookAheadBase, GameplayLookAheadHighRisk, currentDangerTension)
            : BaseLookAhead;
        Vector3 targetOffset = isMoving
            ? (Vector3)(moveVector.normalized * lookAheadMagnitude)
            : Vector3.zero;
        float smoothTime = isMoving ? LookAheadMoveSmooth : LookAheadReturnSmooth;

        if (instant)
        {
            lookAheadOffset = targetOffset;
            lookAheadVelocity = Vector3.zero;
            return lookAheadOffset;
        }

        lookAheadOffset = Vector3.SmoothDamp(
            lookAheadOffset,
            targetOffset,
            ref lookAheadVelocity,
            smoothTime,
            Mathf.Infinity,
            deltaTime);
        return lookAheadOffset;
    }

    private Vector3 ResolveHubFocusOffset(float deltaTime, bool instant)
    {
        if (!IsBaseScene())
        {
            focusWeight = 0f;
            return Vector3.zero;
        }

        float targetWeight = hubFocusTarget != null ? 1f : 0f;
        if (instant)
        {
            focusWeight = targetWeight;
        }
        else
        {
            float duration = targetWeight > focusWeight ? BaseFocusEnterDuration : BaseFocusExitDuration;
            float step = duration > 0.001f ? deltaTime / duration : 1f;
            focusWeight = Mathf.MoveTowards(focusWeight, targetWeight, step);
        }

        if (hubFocusTarget == null || focusWeight <= 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 focusVector = hubFocusTarget.position - followTarget.position;
        focusVector.z = 0f;
        return focusVector * (BaseFocusOffsetRatio * focusWeight);
    }

    private float ResolveDesiredOrthographicSize(float adaptedBaseSize, bool instant)
    {
        float desiredSize = adaptedBaseSize;
        if (IsBaseScene() && focusWeight > 0.001f)
        {
            desiredSize *= Mathf.Lerp(1f, BaseFocusZoomMultiplier, focusWeight);
        }

        if (IsGameplayScene())
        {
            desiredSize *= 1f - ResolveDangerZoomReduction(currentDangerTension);
        }

        return desiredSize;
    }

    private float ResolveBaseOrthographicSize()
    {
        if (controlledCamera == null)
        {
            return 5f;
        }

        if (ScreenAdaptationManager.TryGetAdaptedOrthographicSize(controlledCamera, out float adaptedSize))
        {
            return adaptedSize;
        }

        return controlledCamera.orthographicSize;
    }

    private float ResolveFollowSmoothTime()
    {
        if (IsBaseScene())
        {
            return BaseFollowSmooth;
        }

        return Mathf.Lerp(GameplayFollowSmoothBase, GameplayFollowSmoothHighRisk, currentDangerTension);
    }

    private bool ConsumeAdoptedExternalPose()
    {
        if (!hasAdoptedExternalPose)
        {
            return false;
        }

        hasAdoptedExternalPose = false;
        followVelocity = Vector3.zero;
        lookAheadVelocity = Vector3.zero;
        orthographicSizeVelocity = 0f;
        return true;
    }

    private Vector3 SnapCameraPosition(Vector3 position, float orthographicSize)
    {
        if (controlledCamera == null || !controlledCamera.orthographic)
        {
            return position;
        }

        float safeScreenHeight = Mathf.Max(Screen.height, 1);
        float worldUnitsPerPixel = (orthographicSize * 2f) / safeScreenHeight;
        if (worldUnitsPerPixel <= 0.00001f)
        {
            return position;
        }

        position.x = Mathf.Round(position.x / worldUnitsPerPixel) * worldUnitsPerPixel;
        position.y = Mathf.Round(position.y / worldUnitsPerPixel) * worldUnitsPerPixel;
        return position;
    }

    private Vector3 EvaluateTransientOffset(float deltaTime)
    {
        return EvaluateAttackOffset(deltaTime) + EvaluateDamageOffset(deltaTime);
    }

    private Vector3 EvaluateAttackOffset(float deltaTime)
    {
        if (!float.IsFinite(attackPulseElapsed))
        {
            return Vector3.zero;
        }

        attackPulseElapsed += deltaTime;
        float totalDuration = AttackPunchOutDuration + AttackPunchReturnDuration;
        if (attackPulseElapsed >= totalDuration)
        {
            attackPulseElapsed = float.PositiveInfinity;
            return Vector3.zero;
        }

        Vector2 direction = lastAttackFacing.sqrMagnitude > 0.0001f ? lastAttackFacing.normalized : ResolveFacingDirection();
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        float amplitude;
        if (attackPulseElapsed <= AttackPunchOutDuration)
        {
            float t = Mathf.Clamp01(attackPulseElapsed / AttackPunchOutDuration);
            amplitude = Mathf.LerpUnclamped(0f, AttackPunchDistance, EaseOutCubic(t));
        }
        else
        {
            float t = Mathf.Clamp01((attackPulseElapsed - AttackPunchOutDuration) / AttackPunchReturnDuration);
            amplitude = Mathf.LerpUnclamped(AttackPunchDistance, 0f, EaseInOutCubic(t));
        }

        return (Vector3)(direction * amplitude);
    }

    private Vector3 EvaluateDamageOffset(float deltaTime)
    {
        if (!float.IsFinite(damagePulseElapsed))
        {
            return Vector3.zero;
        }

        damagePulseElapsed += deltaTime;
        if (damagePulseElapsed >= DamageShakeDuration)
        {
            damagePulseElapsed = float.PositiveInfinity;
            return Vector3.zero;
        }

        float progress = Mathf.Clamp01(damagePulseElapsed / DamageShakeDuration);
        float envelope = 1f - progress;
        Vector2 recoilDirection = damagePulseDirection.sqrMagnitude > 0.0001f ? damagePulseDirection : -ResolveFacingDirection();
        if (recoilDirection.sqrMagnitude <= 0.0001f)
        {
            recoilDirection = Vector2.down;
        }

        recoilDirection.Normalize();
        float recoilAmount = Mathf.Sin(progress * Mathf.PI) * damagePulseAmplitude * DamageRecoilScale;
        Vector3 recoilOffset = (Vector3)(recoilDirection * recoilAmount);

        float sampleTime = Time.unscaledTime * DamageNoiseFrequency;
        float noiseX = Mathf.PerlinNoise(damageNoiseSeedX, sampleTime) - 0.5f;
        float noiseY = Mathf.PerlinNoise(damageNoiseSeedY, sampleTime) - 0.5f;
        Vector3 shakeOffset = new Vector3(noiseX, noiseY, 0f) * (damagePulseAmplitude * 2f * envelope);
        return recoilOffset + shakeOffset;
    }

    private bool ShouldFreezeGameplayCamera()
    {
        if (!IsGameplayScene())
        {
            return false;
        }

        if (Time.timeScale <= 0.0001f)
        {
            return true;
        }

        return UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen();
    }

    private bool ShouldAllowRuntimeCameraMotion()
    {
        return !IsExternallyDriven() && !ShouldFreezeGameplayCamera();
    }

    private bool ShouldDeferDefaultOffsetCapture()
    {
        return IsGameplayScene() && IsExternallyDriven();
    }

    private bool IsExternallyDriven()
    {
        return GameplayStageIntroDirector.IsIntroActive || GameplayFailureController.IsFailureActive;
    }

    private bool IsSupportedScene()
    {
        return IsBaseScene() || IsGameplayScene();
    }

    private bool IsBaseScene()
    {
        return string.Equals(activeSceneName, BaseSceneName, System.StringComparison.Ordinal);
    }

    private bool IsGameplayScene()
    {
        return GameplayStageCatalog.IsGameplayScene(activeSceneName);
    }

    private Vector2 ResolveFacingDirection()
    {
        if (directionTracker != null)
        {
            Vector2 trackedDirection = directionTracker.LastDirection;
            if (trackedDirection.sqrMagnitude > 0.0001f)
            {
                return trackedDirection.normalized;
            }
        }

        if (followBody != null && followBody.velocity.sqrMagnitude > 0.0004f)
        {
            return followBody.velocity.normalized;
        }

        return Vector2.up;
    }

    private void ClearTransientMotion()
    {
        attackPulseElapsed = float.PositiveInfinity;
        damagePulseElapsed = float.PositiveInfinity;
        damagePulseAmplitude = 0f;
        damagePulseDirection = Vector2.zero;
    }

    private static float ResolveDangerZoomReduction(float tension)
    {
        tension = Mathf.Clamp01(tension);
        if (tension <= 0f)
        {
            return 0f;
        }

        if (tension <= MidDangerTension)
        {
            float midProgress = Mathf.InverseLerp(0f, MidDangerTension, tension);
            return Mathf.Lerp(0f, MidDangerZoomReduction, midProgress);
        }

        float highProgress = Mathf.InverseLerp(MidDangerTension, HighDangerTension, tension);
        return Mathf.Lerp(MidDangerZoomReduction, HighDangerZoomReduction, highProgress);
    }

    private static float EaseOutCubic(float value)
    {
        float inverted = 1f - Mathf.Clamp01(value);
        return 1f - inverted * inverted * inverted;
    }

    private static float EaseInOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        if (value < 0.5f)
        {
            return 4f * value * value * value;
        }

        float inverted = -2f * value + 2f;
        return 1f - (inverted * inverted * inverted) * 0.5f;
    }
}
