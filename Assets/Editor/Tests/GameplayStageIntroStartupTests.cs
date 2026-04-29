using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameplayStageIntroStartupTests
{
    private const string FirstPassSceneName = "FirstPass_1";
    private const string LegacyGameSceneName = "GameScene";
    private const string BaseSceneName = "NewBase";
    private const string BaseScenePath = "Assets/Scenes/NewBase.unity";
    private const string FirstPassScenePath = "Assets/Scenes/FirstPass_1.unity";
    private const string LevelSelectionScenePath = "Assets/Scenes/LevelSelection.unity";
    private const string StartAniPath = "Assets/Animation/PlayerAni/Start/StartAni.anim";
    private const float ExpectedFirstPassActorScale = 1.2f;

    [Test]
    public void FirstStageUsesFirstPassScene()
    {
        GameplayStageDefinition firstStage = GameplayStageCatalog.GetDefaultStage();

        Assert.AreEqual(FirstPassSceneName, firstStage.sceneName);
        Assert.IsTrue(GameplayStageCatalog.IsGameplayScene(FirstPassSceneName));
        Assert.AreSame(firstStage, GameplayStageCatalog.GetStageByScene(FirstPassSceneName));
    }

    [Test]
    public void LegacyGameSceneRemainsFirstStageGameplayAlias()
    {
        GameplayStageDefinition firstStage = GameplayStageCatalog.GetDefaultStage();

        Assert.IsTrue(GameplayStageCatalog.IsGameplayScene(LegacyGameSceneName));
        Assert.AreSame(firstStage, GameplayStageCatalog.GetStageByScene(LegacyGameSceneName));
    }

    [Test]
    public void SecondAndThirdStagesUseCurrentDesignOrder()
    {
        System.Collections.Generic.IReadOnlyList<GameplayStageDefinition> stages = GameplayStageCatalog.GetAll();

        Assert.GreaterOrEqual(stages.Count, 3);

        GameplayStageDefinition secondStage = stages[1];
        Assert.AreEqual("stage_02", secondStage.stageId);
        Assert.AreEqual("第二关", secondStage.stageLabel);
        Assert.AreEqual("安徽水乡民居", secondStage.mapTitle);
        Assert.AreEqual("第二关 · 安徽水乡民居", secondStage.displayName);
        Assert.AreEqual("GameScene_03", secondStage.sceneName);
        Assert.AreEqual(CatalogueBuildingId.Building3, secondStage.stageBuildingId);
        Assert.AreEqual(CatalogueBuildingId.Building1, secondStage.gatingBuildingId);
        Assert.AreEqual("修复福建土楼后开放", secondStage.lockedHint);
        Assert.AreSame(secondStage, GameplayStageCatalog.GetStageByScene("GameScene_03"));

        GameplayStageDefinition thirdStage = stages[2];
        Assert.AreEqual("stage_03", thirdStage.stageId);
        Assert.AreEqual("第三关", thirdStage.stageLabel);
        Assert.AreEqual("赵州桥", thirdStage.mapTitle);
        Assert.AreEqual("第三关 · 赵州桥", thirdStage.displayName);
        Assert.AreEqual("GameScene_02", thirdStage.sceneName);
        Assert.AreEqual(CatalogueBuildingId.Building2, thirdStage.stageBuildingId);
        Assert.AreEqual(CatalogueBuildingId.Building3, thirdStage.gatingBuildingId);
        Assert.AreEqual("修复安徽水乡民居后开放", thirdStage.lockedHint);
        Assert.AreSame(thirdStage, GameplayStageCatalog.GetStageByScene("GameScene_02"));
    }

    [Test]
    public void LevelSelectionPreviewSpritesFollowCurrentStageBuildings()
    {
        MethodInfo resolvePreviewPath = typeof(LevelSelectionSceneController).GetMethod(
            "ResolveStagePreviewSpritePath",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolvePreviewPath);

        GameplayStageDefinition secondStage = GameplayStageCatalog.GetStageById("stage_02");
        GameplayStageDefinition thirdStage = GameplayStageCatalog.GetStageById("stage_03");

        Assert.AreEqual(
            "Assets/File/UIResources/ShuiXiang.png",
            resolvePreviewPath.Invoke(null, new object[] { secondStage.stageId }));
        Assert.AreEqual(
            "Assets/File/UIResources/ZhaoGouBridge.png",
            resolvePreviewPath.Invoke(null, new object[] { thirdStage.stageId }));
    }

    [Test]
    public void LevelSelectionPlaceholderStagesUseLockInsteadOfPreviewSprites()
    {
        MethodInfo resolvePreviewPath = typeof(LevelSelectionSceneController).GetMethod(
            "ResolveStagePreviewSpritePath",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo resolveLegacyRoot = typeof(LevelSelectionSceneController).GetMethod(
            "ResolveLegacyStageRootName",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolvePreviewPath);
        Assert.IsNotNull(resolveLegacyRoot);

        Assert.AreEqual(string.Empty, resolvePreviewPath.Invoke(null, new object[] { "stage_04" }));
        Assert.AreEqual(string.Empty, resolvePreviewPath.Invoke(null, new object[] { "stage_05" }));
        Assert.AreEqual(string.Empty, resolveLegacyRoot.Invoke(null, new object[] { "stage_04" }));
        Assert.AreEqual(string.Empty, resolveLegacyRoot.Invoke(null, new object[] { "stage_05" }));
    }

    [Test]
    public void FirstPassSceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && scene.path == FirstPassScenePath);

        Assert.IsTrue(enabled, "FirstPass_1 must be enabled so the first stage can load in builds.");
    }

    [Test]
    public void LevelSelectionSceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && scene.path == LevelSelectionScenePath);

        Assert.IsTrue(enabled, "LevelSelection must be enabled so the base gate can open the authored level-select UI.");
    }

    [Test]
    public void BuildStartsInBaseScene()
    {
        EditorBuildSettingsScene firstEnabledScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);

        Assert.IsNotNull(firstEnabledScene, "Build Settings must contain at least one enabled scene.");
        Assert.AreEqual(BaseScenePath, firstEnabledScene.path, "玩家开始游戏应直接进入基地场景。");
    }

    [Test]
    public void EditorPlayModeStartsInBaseScene()
    {
        EditorPlayModeStartScene.EnsureBaseStartScene();

        SceneAsset startupScene = EditorSceneManager.playModeStartScene;

        Assert.IsNotNull(startupScene, "编辑器 Play Mode 必须固定从基地场景启动，避免直接从关卡场景出生。");
        Assert.AreEqual(BaseScenePath, AssetDatabase.GetAssetPath(startupScene));
    }

    [Test]
    public void FailureRestartReturnsPlayerToBaseScene()
    {
        MethodInfo resolveRestartSceneName = typeof(GameOverUI).GetMethod(
            "ResolveRestartSceneName",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolveRestartSceneName);
        Assert.AreEqual(BaseSceneName, resolveRestartSceneName.Invoke(null, null));
    }

    [Test]
    public void PlayerStartAnimationDoesNotLoop()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StartAniPath);

        Assert.IsNotNull(clip);
        Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(clip).loopTime);
    }

    [Test]
    public void SpriteCompanionSupportsIntroPause()
    {
        GameObject player = new GameObject("Player", typeof(SpriteRenderer), typeof(CharacterCore));
        GameObject companion = new GameObject("Companion");
        SpriteCompanionFollowController followController = null;

        try
        {
            companion.AddComponent<Rigidbody2D>().gravityScale = 0f;
            companion.AddComponent<BoxCollider2D>();
            CharacterCore core = companion.AddComponent<CharacterCore>();
            core.stats = new CharacterStats { maxHp = 1f, moveSpeed = 4f };
            core.baseStats = core.stats.Clone();
            companion.AddComponent<EnemyAvoidObstacle>();
            EnemyMove move = companion.AddComponent<EnemyMove>();
            companion.AddComponent<SpriteRenderer>();
            followController = companion.AddComponent<SpriteCompanionFollowController>();
            followController.Bind(player.transform, player.GetComponent<CharacterCore>());
            companion.transform.localScale = Vector3.one * 2f;

            followController.SetIntroPaused(true);

            Assert.IsTrue(followController.IsIntroPaused);
            Assert.AreEqual(Vector2.zero, move.moveDirection);
            Assert.Greater(companion.transform.localScale.y, 2f);
            Assert.LessOrEqual(companion.transform.localScale.y, 2.4f);

            followController.SetIntroPaused(false);

            Assert.IsFalse(followController.IsIntroPaused);
            Assert.AreEqual(Vector3.one * 2f, companion.transform.localScale);
        }
        finally
        {
            if (followController != null)
            {
                Object.DestroyImmediate(followController);
            }

            Object.DestroyImmediate(companion);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void FirstPassCompanionUsesGameplayScaleCloseToPlayer()
    {
        MethodInfo resolveScale = typeof(SpriteCompanionRuntime).GetMethod(
            "ResolveCompanionScale",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolveScale);

        float firstPassScale = (float)resolveScale.Invoke(null, new object[] { FirstPassSceneName });
        float baseScale = (float)resolveScale.Invoke(null, new object[] { "NewBase" });

        Assert.That(firstPassScale, Is.InRange(0.72f, 0.74f));
        Assert.That(firstPassScale, Is.GreaterThan(baseScale));
        Assert.That(firstPassScale, Is.LessThan(1f));
    }

    [Test]
    public void FirstPassPlayerUsesReadableGameplayActorScale()
    {
        Scene scene = EditorSceneManager.OpenScene(FirstPassScenePath, OpenSceneMode.Single);
        GameObject player = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.CompareTag("Player") || root.name == "Player");

        Assert.IsNotNull(player, "FirstPass_1 must contain an authored Player root.");
        Assert.AreEqual(Vector3.one * ExpectedFirstPassActorScale, player.transform.localScale);
    }

    [Test]
    public void RepairableBuildingWaitsForRepairLoopReadiness()
    {
        Assert.IsFalse(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: false,
            hasRepairMaterial: false,
            isRepaired: false));

        Assert.IsTrue(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: true,
            hasRepairMaterial: false,
            isRepaired: false));
        Assert.IsTrue(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: false,
            hasRepairMaterial: true,
            isRepaired: false));
        Assert.IsTrue(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: false,
            hasRepairMaterial: false,
            isRepaired: true));
    }

    [Test]
    public void FirstPassNightProfileUsesCountdownAndDarkensAfterThirtySeconds()
    {
        MethodInfo resolveProfile = typeof(NightLightingController).GetMethod(
            "ResolveProfile",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolveProfile);

        SceneNightProfile profile = resolveProfile.Invoke(null, new object[] { FirstPassSceneName }) as SceneNightProfile;

        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.useCountdownProgress);
        Assert.Greater(profile.EvaluateOverlayAlpha(0.1f), profile.EvaluateOverlayAlpha(0f));
    }

    [Test]
    public void PlayerStartAnimationHoldWaitsUntilOneShotClipCompletes()
    {
        Assert.AreEqual(
            0.45f,
            GameplayStageIntroDirector.ResolvePlayerStartAnimationRemainingDuration(1.35f, 0.9f),
            0.001f);
        Assert.AreEqual(
            0f,
            GameplayStageIntroDirector.ResolvePlayerStartAnimationRemainingDuration(1.35f, 1.4f),
            0.001f);
    }
}
