using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class GameplayStageIntroStartupTests
{
    private const string FirstPassSceneName = "FirstPass_1";
    private const string LegacyGameSceneName = "GameScene";
    private const string BaseSceneName = "NewBase";
    private const string BaseScenePath = "Assets/Scenes/NewBase.unity";
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string OriginalFirstPassScenePath = "Assets/Scenes/FirstPass.unity";
    private const string FirstPassScenePath = "Assets/Scenes/FirstPass_1.unity";
    private const string SecondPassSceneName = "SecondPassSence";
    private const string SecondPassScenePath = "Assets/Scenes/SecondPassSence.unity";
    private const string LevelSelectionScenePath = "Assets/Scenes/LevelSelection.unity";
    private const string PlayerAttackProjectilePrefabPath = "Assets/File/Prefab/WeaponPrefab/MagicBall.prefab";
    private const string StartAniPath = "Assets/Animation/PlayerAni/Start/StartAni.anim";
    private const float ExpectedFirstPassActorScale = 1.2f;
    private const float ExpectedBaseCompanionScale = 0.54f;
    private const float ExpectedGameplayCompanionScale = ExpectedBaseCompanionScale * 1.35f;
    private const float ExpectedFirstPassCompanionScale = ExpectedGameplayCompanionScale * 0.8f * 0.8f;
    private const float ExpectedPlayerAttackProjectileScale = 2.5f;
    private const int ExpectedPlayerAttackProjectileSortingOrder = 12;

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
        Assert.AreEqual(SecondPassSceneName, secondStage.sceneName);
        Assert.AreEqual(CatalogueBuildingId.Building3, secondStage.stageBuildingId);
        Assert.AreEqual(CatalogueBuildingId.Building1, secondStage.gatingBuildingId);
        Assert.AreEqual("解锁福建土楼图鉴后开放", secondStage.lockedHint);
        Assert.AreSame(secondStage, GameplayStageCatalog.GetStageByScene(SecondPassSceneName));
        Assert.AreSame(secondStage, GameplayStageCatalog.GetStageByScene("GameScene_03"));
        Assert.AreSame(secondStage, GameplayStageCatalog.GetStageByScene("SecondPass"));

        GameplayStageDefinition thirdStage = stages[2];
        Assert.AreEqual("stage_03", thirdStage.stageId);
        Assert.AreEqual("第三关", thirdStage.stageLabel);
        Assert.AreEqual("赵州桥", thirdStage.mapTitle);
        Assert.AreEqual("第三关 · 赵州桥", thirdStage.displayName);
        Assert.AreEqual("GameScene_02", thirdStage.sceneName);
        Assert.AreEqual(CatalogueBuildingId.Building2, thirdStage.stageBuildingId);
        Assert.AreEqual(CatalogueBuildingId.Building3, thirdStage.gatingBuildingId);
        Assert.AreEqual("解锁安徽水乡民居图鉴后开放", thirdStage.lockedHint);
        Assert.AreSame(thirdStage, GameplayStageCatalog.GetStageByScene("GameScene_02"));
    }

    [Test]
    public void StageUnlockFollowsCatalogueBuildingUnlockInsteadOfRepair()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);
        GameplayStageDefinition secondStage = GameplayStageCatalog.GetStageById("stage_02");

        Assert.IsFalse(GameplayStageCatalog.IsStageUnlocked(secondStage, state));

        CompleteBuildingUnlock(state, CatalogueBuildingId.Building1);

        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(state.IsBuildingRepaired(CatalogueBuildingId.Building1));
        Assert.IsTrue(GameplayStageCatalog.IsStageUnlocked(secondStage, state));

        Object.DestroyImmediate(state.gameObject);
    }

    [Test]
    public void LevelSelectionPreviewSpritesFollowCurrentStageBuildings()
    {
        MethodInfo resolvePreviewPath = typeof(LevelSelectionSceneController).GetMethod(
            "ResolveStagePreviewSpritePath",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
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
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string) },
            null);
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
    public void SecondPassSceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && scene.path == SecondPassScenePath);

        Assert.IsTrue(enabled, "SecondPassSence must be enabled so the second stage can load in builds.");
    }

    [Test]
    public void LevelSelectionSceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && scene.path == LevelSelectionScenePath);

        Assert.IsTrue(enabled, "LevelSelection must be enabled so the base gate can open the authored level-select UI.");
    }

    [Test]
    public void BuildStartsInMainScene()
    {
        EditorBuildSettingsScene firstEnabledScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);

        Assert.IsNotNull(firstEnabledScene, "Build Settings must contain at least one enabled scene.");
        Assert.AreEqual(MainScenePath, firstEnabledScene.path, "玩家进入游戏以后应先进入 MainScene。");
    }

    [Test]
    public void EditorPlayModeStartsInMainScene()
    {
        EditorPlayModeStartScene.EnsureMainStartScene();

        SceneAsset startupScene = EditorSceneManager.playModeStartScene;

        Assert.IsNotNull(startupScene, "编辑器 Play Mode 必须固定从 MainScene 启动。");
        Assert.AreEqual(MainScenePath, AssetDatabase.GetAssetPath(startupScene));
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
            EnemyMove move = companion.GetComponent<EnemyMove>();
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
    public void FirstPassCompanionUsesRepeatedTwentyPercentSmallerGameplayScale()
    {
        MethodInfo resolveScale = typeof(SpriteCompanionRuntime).GetMethod(
            "ResolveCompanionScale",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolveScale);

        float firstPassScale = (float)resolveScale.Invoke(null, new object[] { FirstPassSceneName });
        float legacyFirstPassScale = (float)resolveScale.Invoke(null, new object[] { LegacyGameSceneName });
        float baseScale = (float)resolveScale.Invoke(null, new object[] { "NewBase" });

        Assert.That(firstPassScale, Is.EqualTo(ExpectedFirstPassCompanionScale).Within(0.0001f));
        Assert.That(legacyFirstPassScale, Is.EqualTo(ExpectedFirstPassCompanionScale).Within(0.0001f));
        Assert.That(firstPassScale, Is.LessThan(baseScale));
        Assert.That(firstPassScale, Is.LessThan(ExpectedGameplayCompanionScale));
    }

    [Test]
    public void LaterStageCompanionsKeepGameplayScale()
    {
        MethodInfo resolveScale = typeof(SpriteCompanionRuntime).GetMethod(
            "ResolveCompanionScale",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolveScale);

        float secondStageScale = (float)resolveScale.Invoke(null, new object[] { SecondPassSceneName });
        float legacySecondStageScale = (float)resolveScale.Invoke(null, new object[] { "GameScene_03" });
        float thirdStageScale = (float)resolveScale.Invoke(null, new object[] { "GameScene_02" });

        Assert.That(secondStageScale, Is.EqualTo(ExpectedGameplayCompanionScale).Within(0.0001f));
        Assert.That(legacySecondStageScale, Is.EqualTo(ExpectedGameplayCompanionScale).Within(0.0001f));
        Assert.That(thirdStageScale, Is.EqualTo(ExpectedGameplayCompanionScale).Within(0.0001f));
    }

    [Test]
    public void FirstPassPlayerUsesReadableGameplayActorScale()
    {
        Scene scene = EditorSceneManager.OpenScene(FirstPassScenePath, OpenSceneMode.Single);
        GameObject player = FindScenePlayer(scene);

        Assert.IsNotNull(player, "FirstPass_1 must contain an authored Player root.");
        Assert.AreEqual(Vector3.one * ExpectedFirstPassActorScale, player.transform.localScale);
    }

    [Test]
    public void FirstPassPortUsesOriginalFirstPassPlayerAttackPrefab()
    {
        Scene originalScene = EditorSceneManager.OpenScene(OriginalFirstPassScenePath, OpenSceneMode.Single);
        GameObject originalPlayer = FindScenePlayer(originalScene);
        Assert.IsNotNull(originalPlayer, "Original FirstPass must contain an authored Player root.");

        PlayerAttack originalAttack = originalPlayer.GetComponent<PlayerAttack>();
        Assert.IsNotNull(originalAttack, "Original FirstPass player must define the player attack component.");
        Assert.IsNotNull(originalAttack.inkballPrefab, "Original FirstPass player attack must use a visible projectile prefab.");
        string expectedProjectilePath = AssetDatabase.GetAssetPath(originalAttack.inkballPrefab);

        Scene firstPassPortScene = EditorSceneManager.OpenScene(FirstPassScenePath, OpenSceneMode.Single);
        GameObject firstPassPortPlayer = FindScenePlayer(firstPassPortScene);
        Assert.IsNotNull(firstPassPortPlayer, "FirstPass_1 must contain an authored Player root.");

        PlayerAttack firstPassPortAttack = firstPassPortPlayer.GetComponent<PlayerAttack>();
        Assert.IsNotNull(firstPassPortAttack, "FirstPass_1 player must use the same attack component as FirstPass.");
        Assert.IsNotNull(firstPassPortAttack.inkballPrefab, "FirstPass_1 attack must be visible in scene.");
        Assert.AreEqual(expectedProjectilePath, AssetDatabase.GetAssetPath(firstPassPortAttack.inkballPrefab));
        Assert.AreSame(firstPassPortPlayer.transform, firstPassPortAttack.inkPoint);

        PlayerAttributeManager attributeManager = firstPassPortPlayer.GetComponent<PlayerAttributeManager>();
        Assert.IsNotNull(attributeManager);
        Assert.AreSame(firstPassPortAttack, attributeManager.playerAttack);
    }

    [Test]
    public void PlayerAttackProjectilePrefabUsesHalfScale()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAttackProjectilePrefabPath);

        Assert.IsNotNull(prefab, "PlayerAttack must keep using the authored visible projectile prefab.");
        Assert.That(prefab.transform.localScale.x, Is.EqualTo(ExpectedPlayerAttackProjectileScale).Within(0.001f));
        Assert.That(prefab.transform.localScale.y, Is.EqualTo(ExpectedPlayerAttackProjectileScale).Within(0.001f));
        Assert.That(prefab.transform.localScale.z, Is.EqualTo(ExpectedPlayerAttackProjectileScale).Within(0.001f));
    }

    [Test]
    public void PlayerAttackProjectilePrefabRendersAboveFirstPassViewLayers()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAttackProjectilePrefabPath);
        SpriteRenderer projectileRenderer = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
        Scene firstPassPortScene = EditorSceneManager.OpenScene(FirstPassScenePath, OpenSceneMode.Single);
        int highestViewSortingOrder = ResolveHighestSceneViewSortingOrder(firstPassPortScene);

        Assert.IsNotNull(projectileRenderer, "PlayerAttack projectile must have a SpriteRenderer.");
        Assert.AreEqual(ExpectedPlayerAttackProjectileSortingOrder, projectileRenderer.sortingOrder);
        Assert.That(projectileRenderer.sortingOrder, Is.GreaterThan(highestViewSortingOrder));
    }

    [Test]
    public void RepairableBuildingWaitsForRepairLoopReadiness()
    {
        Assert.IsFalse(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: false,
            hasRepairMaterial: false,
            isRepaired: false));

        Assert.IsFalse(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: true,
            hasRepairMaterial: false,
            isRepaired: false));
        Assert.IsFalse(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
            isRepairReady: false,
            hasRepairMaterial: true,
            isRepaired: false));
        Assert.IsFalse(RepairableBuildingBootstrapper.ShouldSpawnRepairableBuilding(
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
    public void SecondPassNightProfileUsesGameplayCountdown()
    {
        MethodInfo resolveProfile = typeof(NightLightingController).GetMethod(
            "ResolveProfile",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolveProfile);

        SceneNightProfile profile = resolveProfile.Invoke(null, new object[] { SecondPassSceneName }) as SceneNightProfile;

        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.useCountdownProgress);
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

    private static GameObject FindScenePlayer(Scene scene)
    {
        return scene.GetRootGameObjects()
            .FirstOrDefault(root => root.CompareTag("Player") || root.name == "Player");
    }

    private static int ResolveHighestSceneViewSortingOrder(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(renderer => renderer is TilemapRenderer || renderer is SpriteRenderer)
            .Max(renderer => renderer.sortingOrder);
    }

    private static void CompleteBuildingUnlock(RuntimeProgressState state, CatalogueBuildingId buildingId)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        Assert.IsTrue(state.AddBuildingProgress(buildingId, definition.requiredProgress, out _));

        for (int i = 0; i < definition.slotDefinitions.Length; i++)
        {
            Assert.IsTrue(state.TryUnlockSlot(buildingId, i, out _, out _));
        }

        Assert.IsTrue(state.TryUnlockBuilding(buildingId, out _));
    }
}
