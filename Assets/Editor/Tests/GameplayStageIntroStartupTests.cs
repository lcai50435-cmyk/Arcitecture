using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class GameplayStageIntroStartupTests
{
    private const string FirstPassSceneName = "FirstPass_1";
    private const string LegacyGameSceneName = "GameScene";
    private const string FirstPassScenePath = "Assets/Scenes/FirstPass_1.unity";
    private const string StartAniPath = "Assets/Animation/PlayerAni/Start/StartAni.anim";

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
    public void FirstPassSceneIsEnabledInBuildSettings()
    {
        bool enabled = EditorBuildSettings.scenes.Any(scene =>
            scene.enabled && scene.path == FirstPassScenePath);

        Assert.IsTrue(enabled, "FirstPass_1 must be enabled so the first stage can load in builds.");
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
