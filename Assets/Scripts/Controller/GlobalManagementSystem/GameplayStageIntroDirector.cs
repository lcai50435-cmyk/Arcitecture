using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameplayStageIntroDirector : MonoBehaviour
{
    private const string DirectorObjectName = "GameplayStageIntroDirector";
    private const string OverlayCanvasName = "GameplayStageIntroCanvas";
    private const string RevealGraphicName = "StageRevealOverlay";
    private const string TitleRootName = "StageTitleRoot";
    private const int OverlaySortingOrder = 12020;
    private const float IntroDurationScale = 0.8f;

    private const float TitleFadeInDuration = 0.36f * IntroDurationScale;
    private const float TitleHoldDuration = 1.28f * IntroDurationScale;
    private const float TitleFadeOutDuration = 0.56f * IntroDurationScale;
    private const float OverviewRevealDuration = 0.82f * IntroDurationScale;
    private const float OverviewHoldDuration = 0.28f * IntroDurationScale;
    private const float CameraTravelDuration = 1.24f * IntroDurationScale;
    private const float PortalAppearDuration = 0.24f * IntroDurationScale;
    private const float PortalEjectDuration = 0.88f * IntroDurationScale;
    private const float PortalFadeDuration = 0.22f * IntroDurationScale;
    private const float GameplayUiFadeInDuration = 0.52f * IntroDurationScale;
    private const float BackpackFadeInDelay = 0.16f * IntroDurationScale;
    private const float BlackoutFallbackFadeDuration = 0.14f * IntroDurationScale;
    private const float TitleRevealStartDuringFade = 0.38f;
    private const float PlayerStartAnimationFallbackDuration = 1.35f;
    private const string PlayerStartAnimationStateName = "StartAni";
    private const string PlayerIdleAnimationStateName = "Idle";

    private const float PortalSideOffset = 1.42f;
    private const float PortalVerticalOffset = -0.08f;
    private const float PlayerPortalEmbedDepth = 0.42f;
    private const float PlayerPortalBurstHeight = 1.56f;
    private const float PlayerPortalCameraLift = 0.22f;
    private const float PortalMaskReleaseProgress = 0.24f;
    private const float CameraOverviewPadding = 1.2f;
    private const float CameraApproachSideOffset = 0.92f;
    private const float CameraApproachLift = 0.18f;
    private const float CameraApproachSizeMultiplier = 1f;
    private const float CameraTravelCurveStrength = 1.48f;
    private const float CameraTravelCurveLift = 0.28f;
    private const float CameraOverviewRawSizeMultiplier = 2.96f;
    private const float CameraOverviewMinLandingSizeMultiplier = 5f;
    private const float CameraOverviewMaxLandingSizeMultiplier = 6.08f;
    private const float CameraOverviewLandingBias = 0.15f;
    private const float CameraOverviewDownwardBiasRatio = 0.04f;
    private const float TitleRevealLeadProgress = 0.34f;

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.97f);
    private static readonly Color BlackoutColor = new Color(0f, 0f, 0f, 1f);
    private static readonly Color TitlePrimaryColor = new Color(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color TitleSecondaryColor = new Color(0.83f, 0.93f, 1f, 1f);
    private static readonly Color PortalCoreColor = new Color(0.60f, 0.93f, 1f, 0.95f);
    private static readonly Color PortalGlowColor = new Color(0.20f, 0.72f, 1f, 0.68f);
    private static readonly Vector2 RevealStartClearRadii = new Vector2(1f, 1f);
    private static readonly Vector2 RevealStartFeatherRadii = new Vector2(18f, 14f);
    private static readonly Vector2 RevealLeadClearScale = new Vector2(0.18f, 0.07f);
    private static readonly Vector2 RevealLeadFeatherScale = new Vector2(0.16f, 0.09f);
    private static readonly Vector2 RevealFinishClearScale = new Vector2(0.68f, 0.42f);
    private static readonly Vector2 RevealFinishFeatherScale = new Vector2(0.30f, 0.18f);
    private static readonly int PlayerStartAnimationStateHash = Animator.StringToHash("Base Layer.StartAni");
    private static readonly int PlayerStartAnimationShortHash = Animator.StringToHash(PlayerStartAnimationStateName);
    private static readonly int PlayerIdleAnimationStateHash = Animator.StringToHash("Base Layer.Idle");
    private static readonly int PlayerIdleAnimationShortHash = Animator.StringToHash(PlayerIdleAnimationStateName);

    public static bool IsIntroActive { get; private set; }
    public static bool HasOverlayCoverage { get; private set; }

    private GameplayStageDefinition stageDefinition;
    private Transform playerTransform;
    private GameObject playerObject;
    private Rigidbody2D playerBody;
    private PlayerMove playerMove;
    private PlayerAttack playerAttack;
    private PlayerInteraction playerInteraction;
    private SpriteCompanionFollowController spriteCompanion;

    private RunStageDirector runStageDirector;
    private GameCountDownManager countdownManager;

    private Camera mainCamera;
    private Transform cameraTransform;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Vector3 originalCameraLocalScale;
    private Vector3 originalCameraWorldOffset;
    private float originalCameraOrthographicSize;
    private bool cameraStateCaptured;
    private bool cameraDetached;

    private Canvas overlayCanvas;
    private RectTransform overlayRect;
    private Image blackoutImage;
    private CanvasGroup titleCanvasGroup;
    private RectTransform titleRoot;
    private TextMeshProUGUI stageLabelText;
    private TextMeshProUGUI mapTitleText;

    private readonly List<PlayerRendererState> playerRenderers = new List<PlayerRendererState>();
    private PortalFxInstance portalFx;

    private Vector3 playerLandingPosition;
    private bool playerLandingCaptured;
    private bool stateRestored;
    private bool playerStartAnimationPlayed;
    private float playerStartAnimationStartedAt = -1f;
    private float playerStartAnimationLength;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (!GameplayStageCatalog.IsGameplayScene(scene.name))
        {
            IsIntroActive = false;
            HasOverlayCoverage = false;
            return;
        }

        if (FindObjectOfType<GameplayStageIntroDirector>() != null)
        {
            IsIntroActive = true;
            return;
        }

        IsIntroActive = true;

        GameObject directorObject = new GameObject(DirectorObjectName);
        directorObject.AddComponent<GameplayStageIntroDirector>();
    }

    private void Awake()
    {
        ResolveStageDefinition();
        EnsureOverlay();
        TryResolveSceneReferences();
        FreezeGameplayImmediate();
    }

    private void Start()
    {
        StartCoroutine(RunIntroRoutine());
    }

    private void OnDestroy()
    {
        CleanupPortalFx();
        SetCompanionIntroPaused(false);

        if (!stateRestored)
        {
            RestoreGameplayState();
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
        }

        IsIntroActive = false;
        HasOverlayCoverage = false;
    }

    private IEnumerator RunIntroRoutine()
    {
        const int maxResolveFrames = 12;

        int attempts = 0;
        while (attempts < maxResolveFrames && !TryResolveSceneReferences())
        {
            ApplyRuntimeUiSuppression();
            FreezeGameplayImmediate();
            attempts++;
            yield return null;
        }

        if (!TryResolveSceneReferences() || playerTransform == null)
        {
            RestoreAndFinish();
            yield break;
        }

        FreezeGameplayImmediate();
        EnsureOverlay();

        CameraPose landingPose = ResolveLandingCameraPose();
        CameraPose overviewPose = ResolveOverviewCameraPose(landingPose);
        float portalSideSign = ResolvePortalSideSign(overviewPose, landingPose);
        ApplyCameraPose(overviewPose);

        TmpRuntimeFontFallback.WarmupCharacters(
            $"{stageDefinition?.stageLabel}{stageDefinition?.mapTitle}{stageDefinition?.displayName}");

        yield return PlayTitleIntro();
        yield return PlayCameraTravel(overviewPose, landingPose, portalSideSign);
        yield return PlayPortalEject(landingPose, portalSideSign);
        yield return WaitForPlayerStartAnimationCompletion();
        RestoreGameplayUiState(false);
        yield return PlayGameplayUiFadeIn();

        bool adoptedRuntimeCamera = TryAdoptRuntimeCameraPose();
        IsIntroActive = false;
        RestoreGameplayRuntimeState(!adoptedRuntimeCamera);
        stateRestored = true;
        Destroy(gameObject);
    }

    private IEnumerator PlayTitleIntro()
    {
        float elapsed = 0f;
        while (elapsed < TitleFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / TitleFadeInDuration));
            titleCanvasGroup.alpha = t;
            titleRoot.localScale = Vector3.one * Mathf.Lerp(1.08f, 1f, t);
            yield return null;
        }

        titleCanvasGroup.alpha = 1f;
        titleRoot.localScale = Vector3.one;

        elapsed = 0f;
        while (elapsed < TitleHoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyRuntimeUiSuppression();
            ApplyFullBlackOverlay();
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < TitleFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / TitleFadeOutDuration);
            float titleFade = EaseInOutCubic(t);

            titleCanvasGroup.alpha = 1f - titleFade;
            titleRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.02f, t);
            ApplyRuntimeUiSuppression();

            if (t < TitleRevealStartDuringFade)
            {
                ApplyFullBlackOverlay();
            }
            else
            {
                float revealT = Mathf.InverseLerp(TitleRevealStartDuringFade, 1f, t);
                float blackoutFade = 1f - EaseOutCubic(Mathf.Clamp01(revealT / BlackoutFallbackFadeDuration));

                SetBlackoutAlpha(blackoutFade);
            }

            yield return null;
        }

        SetBlackoutAlpha(0f);
        titleCanvasGroup.alpha = 0f;
    }

    private IEnumerator PlayGameplayUiFadeIn()
    {
        GameplayStatusHudRuntime.SetVisible(true);
        GameplayStatusHudRuntime.SetAlpha(0f);
        RuntimeMiniMapHud.SetExternallyHidden(false);
        RuntimeMiniMapHud.SetExternalAlpha(0f);

        UIRootManager uiRoot = UIRootManager.Instance;
        if (uiRoot != null && uiRoot.backpackUI != null)
        {
            uiRoot.HideBackpack(true);
        }

        float elapsed = 0f;
        bool backpackShown = false;
        while (elapsed < GameplayUiFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / GameplayUiFadeInDuration);
            float eased = EaseOutCubic(t);

            GameplayStatusHudRuntime.SetAlpha(eased);
            RuntimeMiniMapHud.SetExternalAlpha(eased);

            if (!backpackShown && elapsed >= BackpackFadeInDelay && uiRoot != null && uiRoot.backpackUI != null)
            {
                uiRoot.ShowBackpack(false);
                backpackShown = true;
            }

            yield return null;
        }

        GameplayStatusHudRuntime.SetAlpha(1f);
        RuntimeMiniMapHud.SetExternalAlpha(1f);

        if (!backpackShown && uiRoot != null && uiRoot.backpackUI != null)
        {
            uiRoot.ShowBackpack(false);
        }
    }

    private IEnumerator HoldOverviewShot(CameraPose overviewPose)
    {
        float elapsed = 0f;
        while (elapsed < OverviewHoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyRuntimeUiSuppression();
            ApplyCameraPose(overviewPose);
            yield return null;
        }
    }

    private IEnumerator PlayCameraTravel(CameraPose overviewPose, CameraPose landingPose, float portalSideSign)
    {
        CameraPose approachPose = ResolveCameraApproachPose(landingPose, portalSideSign);
        Vector3 controlPoint = Vector3.Lerp(overviewPose.position, approachPose.position, 0.52f)
            + new Vector3(portalSideSign * CameraTravelCurveStrength, CameraTravelCurveLift, 0f);

        float elapsed = 0f;
        while (elapsed < CameraTravelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / CameraTravelDuration);
            ApplyRuntimeUiSuppression();

            if (t < 0.8f)
            {
                float approachT = EaseInOutCubic(t / 0.8f);
                Vector3 travelPosition = EvaluateQuadraticBezier(
                    overviewPose.position,
                    controlPoint,
                    approachPose.position,
                    approachT);
                float travelSize = Mathf.LerpUnclamped(
                    overviewPose.orthographicSize,
                    approachPose.orthographicSize,
                    approachT);
                ApplyCameraPose(new CameraPose(travelPosition, travelSize));
            }
            else
            {
                float settleT = EaseOutCubic((t - 0.8f) / 0.2f);
                ApplyCameraPose(CameraPose.Lerp(approachPose, landingPose, settleT));
            }

            yield return null;
        }

        ApplyCameraPose(landingPose);
    }

    private IEnumerator PlayPortalEject(CameraPose landingPose, float portalSideSign)
    {
        Vector3 portalPosition = playerLandingPosition
            + Vector3.right * (portalSideSign * PortalSideOffset)
            + Vector3.up * PortalVerticalOffset;
        portalFx = CreatePortalFx(portalPosition);

        float elapsed = 0f;
        while (elapsed < PortalAppearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / PortalAppearDuration));
            UpdatePortalFx(portalFx, t, 0f);
            yield return null;
        }

        UpdatePortalFx(portalFx, 1f, 0f);

        Vector3 playerStartPosition = portalPosition + Vector3.down * PlayerPortalEmbedDepth;
        Vector3 playerArcControlPoint = Vector3.Lerp(playerStartPosition, playerLandingPosition, 0.5f)
            + Vector3.up * PlayerPortalBurstHeight
            + Vector3.right * (portalSideSign * 0.22f);
        SetPlayerPosition(playerStartPosition);
        ShowPlayerRenderers(null);
        PlayPlayerStartAnimation();

        elapsed = 0f;
        bool releasedMask = false;
        while (elapsed < PortalEjectDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PortalEjectDuration);
            float eased = EaseOutCubic(t);

            Vector3 throwPosition = EvaluateQuadraticBezier(
                playerStartPosition,
                playerArcControlPoint,
                playerLandingPosition,
                eased);
            SetPlayerPosition(throwPosition);
            ApplyCameraPose(CameraPose.Lerp(
                landingPose.Offset(new Vector3(portalSideSign * 0.34f, PlayerPortalCameraLift, 0f)),
                landingPose,
                eased));
            UpdatePortalFx(portalFx, 1f, t);
            ApplyRuntimeUiSuppression();

            if (!releasedMask && t >= PortalMaskReleaseProgress)
            {
                ShowPlayerRenderers(null);
                releasedMask = true;
            }

            yield return null;
        }

        SetPlayerPosition(playerLandingPosition);
        ApplyCameraPose(landingPose);

        elapsed = 0f;
        while (elapsed < PortalFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / PortalFadeDuration);
            UpdatePortalFx(portalFx, t, 1f);
            yield return null;
        }

        CleanupPortalFx();
        ShowPlayerRenderers(null);
    }

    private void FreezeGameplayImmediate()
    {
        ApplyRuntimeUiSuppression();

        if (!TryResolveSceneReferences())
        {
            return;
        }

        if (!playerLandingCaptured && playerTransform != null)
        {
            playerLandingPosition = playerTransform.position;
            playerLandingCaptured = true;
        }

        CapturePlayerRenderers();
        HidePlayerRenderers();

        if (playerMove != null)
        {
            playerMove.canMove = false;
            if (playerMove.rb != null)
            {
                playerMove.rb.velocity = Vector2.zero;
            }
        }

        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }

        if (playerInteraction != null)
        {
            playerInteraction.ClearCurrentInteractable();
            playerInteraction.enabled = false;
        }

        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
        }

        SetCompanionIntroPaused(true);
        runStageDirector?.SuspendRuntime();
        countdownManager?.SetInBaseState(true);

        DetachMainCamera();
        TrySnapCameraToImmediateOverview();
    }

    private void RestoreAndFinish(bool showUiImmediately = true)
    {
        RestoreGameplayState(showUiImmediately);
        stateRestored = true;
        Destroy(gameObject);
    }

    private void RestoreGameplayState(bool showUiImmediately = true)
    {
        RestoreGameplayUiState(showUiImmediately);
        RestoreGameplayRuntimeState(true);

        if (showUiImmediately)
        {
            IsIntroActive = false;
        }
    }

    private void RestoreGameplayUiState(bool showUiImmediately = true)
    {
        ShowPlayerRenderers(null);

        if (showUiImmediately)
        {
            GameplayStatusHudRuntime.SetVisible(true);
            GameplayStatusHudRuntime.SetAlpha(1f);
            RuntimeMiniMapHud.SetExternallyHidden(false);
            RuntimeMiniMapHud.SetExternalAlpha(1f);

            if (UIRootManager.Instance != null && UIRootManager.Instance.backpackUI != null)
            {
                UIRootManager.Instance.ShowBackpack(true);
            }

            return;
        }

        GameplayStatusHudRuntime.SetVisible(true);
        GameplayStatusHudRuntime.SetAlpha(0f);
        RuntimeMiniMapHud.SetExternallyHidden(false);
        RuntimeMiniMapHud.SetExternalAlpha(0f);

        if (UIRootManager.Instance != null && UIRootManager.Instance.backpackUI != null)
        {
            UIRootManager.Instance.HideBackpack(true);
        }
    }

    private void RestoreGameplayRuntimeState(bool restoreCameraPose)
    {
        if (playerMove != null)
        {
            playerMove.canMove = true;
            playerMove.SetExternalMoveSpeedMultiplier(1f);
        }

        if (playerBody != null)
        {
            playerBody.velocity = Vector2.zero;
        }

        if (playerAttack != null)
        {
            playerAttack.enabled = true;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
            playerInteraction.ClearCurrentInteractable();
        }

        if (restoreCameraPose)
        {
            RestoreMainCamera();
        }

        ReleasePlayerStartAnimation();
        SetCompanionIntroPaused(false);
        countdownManager?.SetInBaseState(false);
        runStageDirector?.ResumeRuntime();
    }

    private void ApplyRuntimeUiSuppression()
    {
        GameplayStatusHudRuntime.SetVisible(false);
        GameplayStatusHudRuntime.SetAlpha(0f);
        RuntimeMiniMapHud.SetExternallyHidden(true);
        RuntimeMiniMapHud.SetExternalAlpha(0f);

        if (UIRootManager.Instance == null)
        {
            return;
        }

        UIRootManager.Instance.HideHandbook();
        UIRootManager.Instance.HideAllDetail();
        UIRootManager.Instance.HideAllSubmitSelection();
        UIRootManager.Instance.HideDialog();
        UIRootManager.Instance.HideInteractTip();
        UIRootManager.Instance.HideBackpack(true);
    }

    private bool TryResolveSceneReferences()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null && playerTransform == null)
        {
            playerTransform = playerObject.transform;
        }

        if (playerObject != null)
        {
            if (playerMove == null)
            {
                playerMove = playerObject.GetComponent<PlayerMove>();
            }

            if (playerAttack == null)
            {
                playerAttack = playerObject.GetComponent<PlayerAttack>();
            }

            if (playerInteraction == null)
            {
                playerInteraction = playerObject.GetComponent<PlayerInteraction>();
            }

            if (playerBody == null)
            {
                playerBody = playerObject.GetComponent<Rigidbody2D>();
            }
        }

        if (spriteCompanion == null)
        {
            spriteCompanion = FindObjectOfType<SpriteCompanionFollowController>(true);
        }

        if (runStageDirector == null)
        {
            runStageDirector = FindObjectOfType<RunStageDirector>();
        }

        if (countdownManager == null)
        {
            countdownManager = GameCountDownManager.Instance != null
                ? GameCountDownManager.Instance
                : FindObjectOfType<GameCountDownManager>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null && playerTransform != null)
            {
                mainCamera = playerTransform.GetComponentInChildren<Camera>(true);
            }
        }

        if (mainCamera != null && cameraTransform == null)
        {
            cameraTransform = mainCamera.transform;
        }

        if (mainCamera != null && playerTransform != null && !cameraStateCaptured)
        {
            CaptureMainCameraState();
        }

        return playerTransform != null && mainCamera != null;
    }

    private void PlayPlayerStartAnimation()
    {
        Animator animator = ResolvePlayerAnimator();
        if (!CanPlayAnimatorState(animator) || !HasAnimatorState(animator, PlayerStartAnimationStateHash, PlayerStartAnimationShortHash))
        {
            return;
        }

        AnimatorParameterUtility.SetBoolIfPresent(animator, "IsMoving", false);
        animator.Play(PlayerStartAnimationStateName, 0, 0f);
        animator.Update(0f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        playerStartAnimationPlayed = true;
        playerStartAnimationStartedAt = Time.unscaledTime;
        playerStartAnimationLength = stateInfo.length > 0.01f
            ? stateInfo.length
            : PlayerStartAnimationFallbackDuration;
    }

    private void ReleasePlayerStartAnimation()
    {
        Animator animator = ResolvePlayerAnimator();
        if (!CanPlayAnimatorState(animator) || !HasAnimatorState(animator, PlayerIdleAnimationStateHash, PlayerIdleAnimationShortHash))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash == PlayerStartAnimationShortHash)
        {
            animator.Play(PlayerIdleAnimationStateName, 0, 0f);
            animator.Update(0f);
        }

        playerStartAnimationPlayed = false;
        playerStartAnimationStartedAt = -1f;
        playerStartAnimationLength = 0f;
    }

    private IEnumerator WaitForPlayerStartAnimationCompletion()
    {
        if (!playerStartAnimationPlayed)
        {
            yield break;
        }

        float elapsedSinceStarted = Time.unscaledTime - playerStartAnimationStartedAt;
        float remainingDuration = ResolvePlayerStartAnimationRemainingDuration(playerStartAnimationLength, elapsedSinceStarted);
        while (remainingDuration > 0f)
        {
            ApplyRuntimeUiSuppression();
            yield return null;
            elapsedSinceStarted = Time.unscaledTime - playerStartAnimationStartedAt;
            remainingDuration = ResolvePlayerStartAnimationRemainingDuration(playerStartAnimationLength, elapsedSinceStarted);
        }
    }

    public static float ResolvePlayerStartAnimationRemainingDuration(float clipLength, float elapsedSinceStarted)
    {
        return Mathf.Max(0f, clipLength - Mathf.Max(0f, elapsedSinceStarted));
    }

    private Animator ResolvePlayerAnimator()
    {
        if (playerMove != null && playerMove.animator != null)
        {
            return playerMove.animator;
        }

        return playerObject != null ? playerObject.GetComponent<Animator>() : null;
    }

    private static bool CanPlayAnimatorState(Animator animator)
    {
        return animator != null
            && animator.isActiveAndEnabled
            && animator.gameObject.activeInHierarchy
            && animator.runtimeAnimatorController != null;
    }

    private static bool HasAnimatorState(Animator animator, int fullPathHash, int shortNameHash)
    {
        return animator != null
            && (animator.HasState(0, fullPathHash) || animator.HasState(0, shortNameHash));
    }

    private void SetCompanionIntroPaused(bool paused)
    {
        if (spriteCompanion == null)
        {
            spriteCompanion = FindObjectOfType<SpriteCompanionFollowController>(true);
        }

        if (spriteCompanion != null)
        {
            spriteCompanion.SetIntroPaused(paused);
        }
    }

    private void ResolveStageDefinition()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        stageDefinition = GameplayStageCatalog.GetStageByScene(activeScene.name) ?? GameplayStageRuntime.SelectedStage;

        if (stageDefinition != null)
        {
            GameplayStageRuntime.SelectStage(stageDefinition.stageId);
        }
    }

    private void CaptureMainCameraState()
    {
        if (cameraTransform == null)
        {
            return;
        }

        originalCameraParent = cameraTransform.parent;
        originalCameraLocalPosition = cameraTransform.localPosition;
        originalCameraLocalRotation = cameraTransform.localRotation;
        originalCameraLocalScale = cameraTransform.localScale;
        originalCameraOrthographicSize = mainCamera != null ? mainCamera.orthographicSize : 5f;
        if (mainCamera != null)
        {
            ScreenAdaptationManager.RegisterBaseOrthographicSize(mainCamera, originalCameraOrthographicSize);
        }

        originalCameraWorldOffset = playerTransform != null
            ? cameraTransform.position - playerTransform.position
            : cameraTransform.position;
        cameraStateCaptured = true;
    }

    private void DetachMainCamera()
    {
        if (cameraTransform == null || cameraDetached)
        {
            return;
        }

        cameraTransform.SetParent(null, true);
        cameraDetached = true;
    }

    private void RestoreMainCamera()
    {
        if (!cameraStateCaptured || cameraTransform == null)
        {
            return;
        }

        if (originalCameraParent != null)
        {
            cameraTransform.SetParent(originalCameraParent, false);
            cameraTransform.localPosition = originalCameraLocalPosition;
            cameraTransform.localRotation = originalCameraLocalRotation;
            cameraTransform.localScale = originalCameraLocalScale;
        }

        if (mainCamera != null)
        {
            mainCamera.orthographicSize = originalCameraOrthographicSize;
        }

        cameraDetached = false;
    }

    private bool TryAdoptRuntimeCameraPose()
    {
        if (playerTransform == null)
        {
            return false;
        }

        RuntimeCameraController controller = RuntimeCameraController.EnsureInstance();
        controller.BindFollowTarget(playerTransform);
        return controller.AdoptCurrentCameraPose();
    }

    private void EnsureOverlay()
    {
        if (overlayCanvas != null && blackoutImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            OverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        overlayRect = canvasObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        GameObject blackoutObject = new GameObject("StageBlackout", typeof(RectTransform), typeof(Image));
        blackoutObject.transform.SetParent(overlayRect, false);
        RectTransform blackoutRect = blackoutObject.GetComponent<RectTransform>();
        blackoutRect.anchorMin = Vector2.zero;
        blackoutRect.anchorMax = Vector2.one;
        blackoutRect.offsetMin = Vector2.zero;
        blackoutRect.offsetMax = Vector2.zero;
        blackoutImage = blackoutObject.GetComponent<Image>();
        blackoutImage.color = BlackoutColor;
        blackoutImage.raycastTarget = false;
        blackoutObject.transform.SetAsFirstSibling();

        ApplyFullBlackOverlay();
        HasOverlayCoverage = true;

        GameObject titleObject = new GameObject(TitleRootName, typeof(RectTransform), typeof(CanvasGroup));
        titleObject.transform.SetParent(overlayRect, false);
        titleRoot = titleObject.GetComponent<RectTransform>();
        titleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        titleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        titleRoot.pivot = new Vector2(0.5f, 0.5f);
        titleRoot.anchoredPosition = new Vector2(0f, 24f);
        titleRoot.sizeDelta = new Vector2(1700f, 520f);
        titleCanvasGroup = titleObject.GetComponent<CanvasGroup>();
        titleCanvasGroup.alpha = 0f;
        titleObject.transform.SetAsLastSibling();

        TMP_FontAsset titleFont = TmpRuntimeFontFallback.WarmupCharacters(
            $"{stageDefinition?.stageLabel}{stageDefinition?.mapTitle}{stageDefinition?.displayName}")
            ?? TMP_Settings.defaultFontAsset;

        stageLabelText = CreateTitleText(
            "StageLabelText",
            titleRoot,
            stageDefinition != null && !string.IsNullOrWhiteSpace(stageDefinition.stageLabel)
                ? stageDefinition.stageLabel
                : stageDefinition != null ? stageDefinition.displayName : "关卡",
            titleFont,
            96f,
            TitleSecondaryColor,
            new Vector2(0f, 68f),
            new Vector2(1600f, 120f));

        mapTitleText = CreateTitleText(
            "MapTitleText",
            titleRoot,
            stageDefinition != null && !string.IsNullOrWhiteSpace(stageDefinition.mapTitle)
                ? stageDefinition.mapTitle
                : stageDefinition != null ? stageDefinition.displayName : "地图",
            titleFont,
            158f,
            TitlePrimaryColor,
            new Vector2(0f, -54f),
            new Vector2(1700f, 220f));

        Canvas.ForceUpdateCanvases();
    }

    private void ApplyFullBlackOverlay()
    {
        SetBlackoutAlpha(1f);
    }

    private void SetBlackoutAlpha(float alpha)
    {
        if (blackoutImage == null)
        {
            return;
        }

        Color color = BlackoutColor;
        color.a = Mathf.Clamp01(alpha);
        blackoutImage.color = color;

        bool shouldShow = color.a > 0.001f;
        if (blackoutImage.gameObject.activeSelf != shouldShow)
        {
            blackoutImage.gameObject.SetActive(shouldShow);
        }
    }

    private static TextMeshProUGUI CreateTitleText(
        string objectName,
        Transform parent,
        string content,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Normal;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private void CapturePlayerRenderers()
    {
        if (playerTransform == null || playerRenderers.Count > 0)
        {
            return;
        }

        SpriteRenderer[] renderers = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            playerRenderers.Add(new PlayerRendererState(renderer, renderer.enabled, renderer.maskInteraction));
        }
    }

    private void HidePlayerRenderers()
    {
        for (int i = 0; i < playerRenderers.Count; i++)
        {
            SpriteRenderer renderer = playerRenderers[i].renderer;
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = false;
            renderer.maskInteraction = SpriteMaskInteraction.None;
        }
    }

    private void ShowPlayerRenderers(SpriteMask clipMask)
    {
        bool useMask = clipMask != null;

        for (int i = 0; i < playerRenderers.Count; i++)
        {
            PlayerRendererState state = playerRenderers[i];
            if (state.renderer == null)
            {
                continue;
            }

            state.renderer.enabled = state.wasEnabled;
            state.renderer.maskInteraction = useMask
                ? SpriteMaskInteraction.VisibleInsideMask
                : state.originalMaskInteraction;
        }
    }

    private void SetPlayerPosition(Vector3 position)
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerBody != null)
        {
            playerBody.position = new Vector2(position.x, position.y);
            playerBody.velocity = Vector2.zero;
        }

        playerTransform.position = position;
    }

    private CameraPose ResolveOverviewCameraPose(CameraPose landingPose)
    {
        if (mainCamera == null)
        {
            return landingPose;
        }

        if (!TryResolveTilemapWorldBounds(out Bounds bounds))
        {
            return landingPose;
        }

        CameraPose rawOverviewPose = ResolveRawOverviewCameraPose(bounds);
        float size = Mathf.Clamp(
            rawOverviewPose.orthographicSize * CameraOverviewRawSizeMultiplier,
            landingPose.orthographicSize * CameraOverviewMinLandingSizeMultiplier,
            landingPose.orthographicSize * CameraOverviewMaxLandingSizeMultiplier);

        Vector3 position = Vector3.Lerp(rawOverviewPose.position, landingPose.position, CameraOverviewLandingBias);
        position.y -= size * CameraOverviewDownwardBiasRatio;
        position.z = rawOverviewPose.position.z;
        return new CameraPose(position, size);
    }

    private CameraPose ResolveRawOverviewCameraPose(Bounds bounds)
    {
        float verticalSize = bounds.extents.y + CameraOverviewPadding;
        float horizontalSize = (bounds.extents.x + CameraOverviewPadding * 1.35f) / Mathf.Max(0.1f, mainCamera.aspect);
        float size = Mathf.Max(verticalSize, horizontalSize);
        Vector3 position = new Vector3(bounds.center.x, bounds.center.y, cameraTransform.position.z);
        return new CameraPose(position, size);
    }

    private CameraPose ResolveLandingCameraPose()
    {
        Vector3 basePosition = playerLandingCaptured ? playerLandingPosition : playerTransform.position;
        Vector3 cameraPosition = basePosition + originalCameraWorldOffset;
        cameraPosition.z = cameraTransform != null ? cameraTransform.position.z : cameraPosition.z;
        return new CameraPose(cameraPosition, originalCameraOrthographicSize);
    }

    private static float ResolvePortalSideSign(CameraPose overviewPose, CameraPose landingPose)
    {
        float deltaX = landingPose.position.x - overviewPose.position.x;
        if (Mathf.Abs(deltaX) < 0.35f)
        {
            return -1f;
        }

        return deltaX >= 0f ? -1f : 1f;
    }

    private CameraPose ResolveCameraApproachPose(CameraPose landingPose, float portalSideSign)
    {
        return new CameraPose(
            landingPose.position + new Vector3(
                portalSideSign * CameraApproachSideOffset,
                CameraApproachLift,
                0f),
            landingPose.orthographicSize * CameraApproachSizeMultiplier);
    }

    private bool TryResolveTilemapWorldBounds(out Bounds bounds)
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>(true);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null || !tilemap.gameObject.activeInHierarchy)
            {
                continue;
            }

            Renderer renderer = tilemap.GetComponent<Renderer>();
            if (renderer != null && !renderer.enabled)
            {
                continue;
            }

            Bounds localBounds = tilemap.localBounds;
            if (localBounds.size.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Vector3 min = tilemap.transform.TransformPoint(localBounds.min);
            Vector3 max = tilemap.transform.TransformPoint(localBounds.max);
            Bounds worldBounds = new Bounds((min + max) * 0.5f, new Vector3(
                Mathf.Abs(max.x - min.x),
                Mathf.Abs(max.y - min.y),
                Mathf.Abs(max.z - min.z)));

            if (!hasBounds)
            {
                bounds = worldBounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(worldBounds.min);
            bounds.Encapsulate(worldBounds.max);
        }

        return hasBounds;
    }

    private void ApplyCameraPose(CameraPose pose)
    {
        if (cameraTransform == null || mainCamera == null)
        {
            return;
        }

        cameraTransform.position = pose.position;
        mainCamera.orthographicSize = pose.orthographicSize;
    }

    private void TrySnapCameraToImmediateOverview()
    {
        if (mainCamera == null || cameraTransform == null)
        {
            return;
        }

        CameraPose landingPose = ResolveLandingCameraPose();
        CameraPose overviewPose = ResolveOverviewCameraPose(landingPose);
        ApplyCameraPose(overviewPose);
    }

    private PortalFxInstance CreatePortalFx(Vector3 position)
    {
        GameObject root = new GameObject("GameplayIntroPortalFx");
        root.transform.SetParent(transform, false);
        root.transform.position = position;
        root.SetActive(false);

        return new PortalFxInstance(root);
    }

    private void UpdatePortalFx(PortalFxInstance fx, float appearProgress, float pulseProgress)
    {
        if (fx == null || fx.root == null)
        {
            return;
        }

        if (!fx.root.activeSelf && appearProgress > 0.01f)
        {
            fx.root.SetActive(true);
        }
    }

    private void CleanupPortalFx()
    {
        if (portalFx == null)
        {
            return;
        }

        if (portalFx.root != null)
        {
            Destroy(portalFx.root);
        }

        portalFx = null;
    }

    private static float EaseOutCubic(float value)
    {
        float t = Mathf.Clamp01(value);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    private static float EaseInOutCubic(float value)
    {
        float t = Mathf.Clamp01(value);
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float eased = Mathf.Clamp01(t);
        float inv = 1f - eased;
        return inv * inv * start
            + 2f * inv * eased * control
            + eased * eased * end;
    }

    private readonly struct PlayerRendererState
    {
        public readonly SpriteRenderer renderer;
        public readonly bool wasEnabled;
        public readonly SpriteMaskInteraction originalMaskInteraction;

        public PlayerRendererState(SpriteRenderer renderer, bool wasEnabled, SpriteMaskInteraction originalMaskInteraction)
        {
            this.renderer = renderer;
            this.wasEnabled = wasEnabled;
            this.originalMaskInteraction = originalMaskInteraction;
        }
    }

    private readonly struct CameraPose
    {
        public readonly Vector3 position;
        public readonly float orthographicSize;

        public CameraPose(Vector3 position, float orthographicSize)
        {
            this.position = position;
            this.orthographicSize = orthographicSize;
        }

        public CameraPose Offset(Vector3 delta)
        {
            return new CameraPose(position + delta, orthographicSize);
        }

        public static CameraPose Lerp(CameraPose from, CameraPose to, float t)
        {
            float eased = Mathf.Clamp01(t);
            return new CameraPose(
                Vector3.LerpUnclamped(from.position, to.position, eased),
                Mathf.LerpUnclamped(from.orthographicSize, to.orthographicSize, eased));
        }
    }

    private sealed class PortalFxInstance
    {
        public readonly GameObject root;

        public PortalFxInstance(GameObject root)
        {
            this.root = root;
        }
    }
}
