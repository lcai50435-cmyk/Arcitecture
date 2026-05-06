using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimeButtonClickSfxRouterTests
{
    private GameObject buttonObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
        {
            Object.DestroyImmediate(buttonObject);
        }
    }

    [Test]
    public void ShouldPlayClickForButtonReturnsTrueOnlyForActiveInteractableButtons()
    {
        buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        Button button = buttonObject.GetComponent<Button>();

        Assert.IsTrue(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        button.interactable = false;
        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        button.interactable = true;
        buttonObject.SetActive(false);
        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(button));

        Assert.IsFalse(RuntimeButtonClickSfxRouter.ShouldPlayClickForButton(null));
    }
}

public sealed class FirstPassRuntimePortTests
{
    private readonly List<GameObject> roots = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] != null)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        roots.Clear();
        InkBall[] inkBalls = Object.FindObjectsOfType<InkBall>();
        for (int i = 0; i < inkBalls.Length; i++)
        {
            if (inkBalls[i] != null)
            {
                Object.DestroyImmediate(inkBalls[i].gameObject);
            }
        }

        DestroyRuntimeObject("GameplayStatusHudCanvas");
        DestroyRuntimeObject("RuntimeMiniMapHud");
        DestroyRuntimeObject("RuntimeMiniMapOverlayCanvas");
        DestroyRuntimeObject("PackBagCanvas");
    }

    [Test]
    public void GameplayStageRuntimeBootstrapperExistsForFirstPassPort()
    {
        System.Type bootstrapperType = System.Type.GetType("GameplayStageRuntimeBootstrapper, Assembly-CSharp");

        Assert.IsNotNull(bootstrapperType);
        Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(bootstrapperType));
    }

    [Test]
    public void StatusHudCreatesFallbackGaugesWhenFirstPassHasNoSceneShowPanel()
    {
        ValueTrans healthGauge = GameplayStatusHudRuntime.EnsureHealthGauge(null);
        ValueTrans weaponGauge = GameplayStatusHudRuntime.EnsureWeaponGauge(null);

        Assert.IsNotNull(healthGauge);
        Assert.IsNotNull(weaponGauge);
        Assert.IsNotNull(GameObject.Find("GameplayStatusHudRoot"));
        Assert.IsNotNull(GameObject.Find("HealthRow"));
        Assert.IsNotNull(GameObject.Find("InkRow"));
        Assert.IsNotNull(GameObject.Find("StructureRow"));
    }

    [Test]
    public void StatusHudFallbackUsesCompactFirstPassLayout()
    {
        GameplayStatusHudRuntime.EnsureHealthGauge(null);

        RectTransform root = GameObject.Find("GameplayStatusHudRoot").GetComponent<RectTransform>();
        RectTransform healthRow = GameObject.Find("HealthRow").GetComponent<RectTransform>();
        RectTransform inkRow = GameObject.Find("InkRow").GetComponent<RectTransform>();
        RectTransform structureRow = GameObject.Find("StructureRow").GetComponent<RectTransform>();

        Assert.AreEqual(new Vector2(18f, -16f), root.anchoredPosition);
        Assert.AreEqual(new Vector2(360f, 166f), root.sizeDelta);
        Assert.AreEqual(new Vector2(352f, 52f), healthRow.sizeDelta);
        Assert.AreEqual(new Vector2(352f, 52f), inkRow.sizeDelta);
        Assert.AreEqual(new Vector2(352f, 52f), structureRow.sizeDelta);
        Assert.AreEqual(new Vector2(0f, 0f), healthRow.anchoredPosition);
        Assert.AreEqual(new Vector2(0f, -50f), inkRow.anchoredPosition);
        Assert.AreEqual(new Vector2(0f, -100f), structureRow.anchoredPosition);

        RectTransform healthIcon = healthRow.Find("Icon").GetComponent<RectTransform>();
        Image healthIconImage = healthIcon.GetComponent<Image>();
        RectTransform healthBar = healthRow.Find("Bar").GetComponent<RectTransform>();
        TextMeshProUGUI healthValue = healthRow.GetComponentInChildren<TextMeshProUGUI>(true);
        Assert.AreEqual(new Vector2(58f, 58f), healthIcon.sizeDelta);
        Assert.AreEqual(new Vector2(252f, 22f), healthBar.sizeDelta);
        Assert.AreEqual(new Vector2(78f, 28f), healthValue.rectTransform.sizeDelta);

        Image inkIcon = inkRow.Find("Icon").GetComponent<Image>();
        Image structureIcon = structureRow.Find("Icon").GetComponent<Image>();
        Assert.IsNotNull(healthIconImage.sprite);
        Assert.IsNotNull(inkIcon.sprite);
        Assert.IsNotNull(structureIcon.sprite);
        Assert.AreEqual("NewUI_1_0", healthIconImage.sprite.name);
        Assert.AreEqual("NewUI_0", inkIcon.sprite.name);
        Assert.AreEqual("NewUI_1", structureIcon.sprite.name);
    }

    [Test]
    public void RuntimeCountdownFallbackRestoresAuthoredTimeBackground()
    {
        TextMeshProUGUI countdown = GameplayStatusHudRuntime.EnsureCountdownText(null);

        GameObject frameObject = GameObject.Find("GameplayCountdownFrame");
        Assert.IsNotNull(countdown);
        Assert.IsNotNull(frameObject);

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        Image frameImage = frameObject.GetComponent<Image>();
        Assert.AreEqual(new Vector2(336f, 72f), frameRect.sizeDelta);
        Assert.AreEqual(new Vector2(0f, -12f), frameRect.anchoredPosition);
        Assert.IsNotNull(frameImage.sprite);
        Assert.AreEqual("time", frameImage.sprite.name);
        Assert.IsFalse(frameImage.raycastTarget);
    }

    [Test]
    public void StatusHudSkinsSceneShowPanelSlidersWithPixelBars()
    {
        GameObject panel = new GameObject("ShowPanel", typeof(RectTransform));
        roots.Add(panel);

        Slider healthSlider = CreateSceneStatusSlider(panel.transform, "HealthSlider");
        Slider inkSlider = CreateSceneStatusSlider(panel.transform, "InkSlider");
        Slider structureSlider = CreateSceneStatusSlider(panel.transform, "StructureSlider");

        GameplayStatusHudRuntime.EnsureHealthGauge(null);

        AssertSceneStatusSliderSkinned(healthSlider, new Color(0.82f, 0.19f, 0.16f, 1f));
        AssertSceneStatusSliderSkinned(inkSlider, new Color(0.08f, 0.08f, 0.08f, 1f));
        AssertSceneStatusSliderSkinned(structureSlider, new Color(0.20f, 0.78f, 0.82f, 1f));
    }

    [Test]
    public void PlayerAttackCreatesRuntimeInkBallWhenPrefabIsMissing()
    {
        GameObject player = new GameObject(
            "Player",
            typeof(Rigidbody2D),
            typeof(SpriteRenderer),
            typeof(Animator),
            typeof(CharacterCore),
            typeof(DirectionTracker),
            typeof(PlayerAttack));
        roots.Add(player);
        player.tag = "Player";

        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        attack.inkballPrefab = null;
        attack.inkPoint = player.transform;
        attack.ink = 100f;
        attack.maxInk = 100f;
        attack.baseMaxInk = 100f;

        attack.TriggerAttack();

        InkBall[] spawnedInkBalls = Object.FindObjectsOfType<InkBall>();
        Assert.That(spawnedInkBalls.Length, Is.GreaterThan(0));
        Assert.IsNotNull(spawnedInkBalls[0].GetComponent<Rigidbody2D>());
        Assert.IsNotNull(spawnedInkBalls[0].GetComponent<Collider2D>());
        Assert.IsNotNull(spawnedInkBalls[0].GetComponent<SpriteRenderer>());
    }

    [Test]
    public void PlayerAttackOffsetsFallbackRuntimeInkBallOutsideOwnerBody()
    {
        GameObject player = new GameObject(
            "Player",
            typeof(Rigidbody2D),
            typeof(BoxCollider2D),
            typeof(SpriteRenderer),
            typeof(Animator),
            typeof(CharacterCore),
            typeof(DirectionTracker),
            typeof(PlayerAttack));
        roots.Add(player);
        player.tag = "Player";

        DirectionTracker tracker = player.GetComponent<DirectionTracker>();
        tracker.defaultDirection = Vector2.right;

        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        attack.inkballPrefab = null;
        attack.inkPoint = player.transform;

        InvokeSpawnInkBalls(attack, Vector2.right, InkAttackRuntimeConfig.Default);

        InkBall[] spawnedInkBalls = Object.FindObjectsOfType<InkBall>();
        Assert.That(spawnedInkBalls.Length, Is.EqualTo(1));

        BoxCollider2D ownerCollider = player.GetComponent<BoxCollider2D>();
        Assert.Greater(spawnedInkBalls[0].transform.position.x, ownerCollider.bounds.max.x + 0.05f);
        Assert.AreEqual(0f, spawnedInkBalls[0].transform.position.y, 0.001f);
    }

    [Test]
    public void InkBallIgnoresOwnerColliderTrigger()
    {
        GameObject player = new GameObject(
            "Player",
            typeof(Rigidbody2D),
            typeof(BoxCollider2D),
            typeof(CharacterCore));
        roots.Add(player);
        player.tag = "Player";

        GameObject projectile = new GameObject(
            "Projectile",
            typeof(Rigidbody2D),
            typeof(CircleCollider2D),
            typeof(SpriteRenderer),
            typeof(InkBall));
        roots.Add(projectile);

        CircleCollider2D projectileCollider = projectile.GetComponent<CircleCollider2D>();
        projectileCollider.isTrigger = true;

        InkBall inkBall = projectile.GetComponent<InkBall>();
        inkBall.character = player.GetComponent<CharacterCore>();
        inkBall.Init(InkAttackRuntimeConfig.Default);

        InvokeInkBallTrigger(inkBall, player.GetComponent<BoxCollider2D>());

        Assert.IsTrue(projectileCollider.enabled);
        Assert.IsNull(GameObject.Find("InkImpactPulse"));
    }

    private static void DestroyRuntimeObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static Slider CreateSceneStatusSlider(Transform parent, string name)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.GetComponent<Slider>();

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject icon = new GameObject("StatusBackGround", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(sliderObject.transform, false);

        GameObject number = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
        number.transform.SetParent(sliderObject.transform, false);

        return slider;
    }

    private static void AssertSceneStatusSliderSkinned(Slider slider, Color expectedFillColor)
    {
        Assert.IsNotNull(slider);

        Image background = slider.transform.Find("Background").GetComponent<Image>();
        Image fill = slider.fillRect.GetComponent<Image>();

        Assert.IsNotNull(background.sprite);
        Assert.That(background.sprite.name, Does.StartWith("pixel_frame_"));
        Assert.AreEqual(Image.Type.Sliced, background.type);
        Assert.AreEqual(Color.white, background.color);

        Assert.IsNotNull(fill.sprite);
        Assert.AreEqual("pixel_gauge_fill", fill.sprite.name);
        Assert.AreEqual(Image.Type.Sliced, fill.type);
        Assert.AreEqual(expectedFillColor, fill.color);
    }

    private static void InvokeSpawnInkBalls(PlayerAttack attack, Vector2 direction, InkAttackRuntimeConfig config)
    {
        MethodInfo method = typeof(PlayerAttack).GetMethod(
            "SpawnInkBalls",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(attack, new object[] { direction, config });
    }

    private static void InvokeInkBallTrigger(InkBall inkBall, Collider2D collider)
    {
        MethodInfo method = typeof(InkBall).GetMethod(
            "OnTriggerEnter2D",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        method.Invoke(inkBall, new object[] { collider });
    }
}

public sealed class PlayerLoadoutRuntimeTests
{
    [TearDown]
    public void TearDown()
    {
        PlayerLoadoutRuntime.ClearRuntimeWeaponOverride();
        PlayerLoadoutRuntime.ClearDebugWeaponOverride();
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;
        PlayerLoadoutRuntime.AllowBaseAttack = false;
    }

    [Test]
    public void DebugWeaponOverrideControlsEffectiveWeaponWithoutPersistingSelection()
    {
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;

        PlayerLoadoutRuntime.SetDebugWeaponOverride(WeaponType.FlowInk);
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();

        Assert.AreEqual(WeaponType.DirectInk, PlayerLoadoutRuntime.CurrentWeaponType);
        Assert.AreEqual(WeaponType.FlowInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(null));
    }

    [Test]
    public void RuntimeWeaponOverrideDoesNotBypassLockedSelectionValidation()
    {
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;

        PlayerLoadoutRuntime.SetRuntimeWeaponOverride(WeaponType.PierceInk);
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();

        Assert.AreEqual(WeaponType.DirectInk, PlayerLoadoutRuntime.CurrentWeaponType);
        Assert.AreEqual(WeaponType.DirectInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(null));
    }

    [Test]
    public void BackpackStructureDoesNotOverrideSelectedWeapon()
    {
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.FlowInk;
        GameObject backpackObject = new GameObject("BackpackManager");
        BackpackMananger backpack = backpackObject.AddComponent<BackpackMananger>();
        try
        {
            backpack.backpackItems[0] = new ArchitecturalCrystal
            {
                type = ArchitecturalType.Brackets,
                resourceCategory = ArchitecturalResourceCategory.CommonStructure,
                runtimePickupOrder = 10
            };

            Assert.AreEqual(WeaponType.FlowInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack));
            Assert.IsFalse(RuntimeWeaponTypeResolver.TryGetActiveWeaponOverride(
                backpack,
                out ArchitecturalCrystal _,
                out WeaponType _,
                out int _));
        }
        finally
        {
            Object.DestroyImmediate(backpackObject);
        }
    }

    [Test]
    public void DebugWeaponOverrideControlsEffectiveWeaponEvenWithBackpackStructure()
    {
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;
        PlayerLoadoutRuntime.SetDebugWeaponOverride(WeaponType.FlowInk);
        GameObject backpackObject = new GameObject("BackpackManager");
        BackpackMananger backpack = backpackObject.AddComponent<BackpackMananger>();
        try
        {
            backpack.backpackItems[0] = new ArchitecturalCrystal
            {
                type = ArchitecturalType.MortiseAndTenonJoint,
                resourceCategory = ArchitecturalResourceCategory.CommonStructure,
                runtimePickupOrder = 10
            };

            Assert.AreEqual(WeaponType.FlowInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack));
            Assert.IsFalse(RuntimeWeaponTypeResolver.TryGetActiveWeaponOverride(
                backpack,
                out ArchitecturalCrystal _,
                out WeaponType _,
                out int _));
        }
        finally
        {
            Object.DestroyImmediate(backpackObject);
        }
    }
}

public sealed class PhotoAlbumRepositoryTests
{
    [SetUp]
    public void SetUp()
    {
        WebGLPersistentFileSystemBridge.ResetSyncRequestCountForTests();
    }

    [Test]
    public void DeleteEntryRemovesPhotoFileAndIndexEntry()
    {
        string tempAlbumDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArcitecturePhotoAlbumTests",
            System.Guid.NewGuid().ToString("N"));

        try
        {
            using (PhotoAlbumRepository.UseAlbumDirectoryForTests(tempAlbumDirectory))
            {
                PhotoAlbumEntry first = PhotoAlbumRepository.SaveCapture(
                    new byte[] { 1, 2, 3 },
                    160,
                    90,
                    "GameScene",
                    "stage_01");
                PhotoAlbumEntry second = PhotoAlbumRepository.SaveCapture(
                    new byte[] { 4, 5, 6 },
                    160,
                    90,
                    "GameScene",
                    "stage_02");

                Assert.IsTrue(File.Exists(PhotoAlbumRepository.GetPhotoPath(first)));

                Assert.IsTrue(PhotoAlbumRepository.DeleteEntry(first));

                Assert.IsFalse(File.Exists(Path.Combine(tempAlbumDirectory, first.fileName)));
                Assert.IsTrue(File.Exists(Path.Combine(tempAlbumDirectory, second.fileName)));

                var entries = PhotoAlbumRepository.LoadEntries();
                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(second.id, entries[0].id);
            }
        }
        finally
        {
            if (Directory.Exists(tempAlbumDirectory))
            {
                Directory.Delete(tempAlbumDirectory, true);
            }
        }
    }

    [Test]
    public void AlbumMutationsRequestPersistentFileSystemSync()
    {
        string tempAlbumDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArcitecturePhotoAlbumSyncTests",
            System.Guid.NewGuid().ToString("N"));

        try
        {
            using (PhotoAlbumRepository.UseAlbumDirectoryForTests(tempAlbumDirectory))
            {
                PhotoAlbumEntry entry = PhotoAlbumRepository.SaveCapture(
                    new byte[] { 1, 2, 3 },
                    160,
                    90,
                    "GameScene",
                    "stage_01");
                Assert.IsNotNull(entry);
                Assert.AreEqual(1, WebGLPersistentFileSystemBridge.SyncRequestCountForTests);

                Assert.IsTrue(PhotoAlbumRepository.DeleteEntry(entry));
                Assert.AreEqual(2, WebGLPersistentFileSystemBridge.SyncRequestCountForTests);

                PhotoAlbumRepository.SaveCapture(
                    new byte[] { 4, 5, 6 },
                    160,
                    90,
                    "GameScene",
                    "stage_01");
                Assert.AreEqual(3, WebGLPersistentFileSystemBridge.SyncRequestCountForTests);

                PhotoAlbumRepository.ClearAll();
                Assert.AreEqual(4, WebGLPersistentFileSystemBridge.SyncRequestCountForTests);
            }
        }
        finally
        {
            if (Directory.Exists(tempAlbumDirectory))
            {
                Directory.Delete(tempAlbumDirectory, true);
            }
        }
    }
}

public sealed class GameDebugPageBootstrapperAttributeTests
{
    private GameObject debugObject;
    private GameObject playerObject;

    [TearDown]
    public void TearDown()
    {
        if (debugObject != null)
        {
            Object.DestroyImmediate(debugObject);
        }

        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }

        if (RuntimeProgressState.Instance != null)
        {
            Object.DestroyImmediate(RuntimeProgressState.Instance.gameObject);
        }

        if (BackpackMananger.Instance != null)
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }

        GameObject hudCanvas = GameObject.Find("GameplayStatusHudCanvas");
        if (hudCanvas != null)
        {
            Object.DestroyImmediate(hudCanvas);
        }
    }

    [Test]
    public void SetMaxHpRefreshesBoundHealthGauge()
    {
        GameDebugPageBootstrapper debugPage = CreateDebugPage();
        CharacterCore core = CreatePlayerCore();
        ValueTrans healthGauge = GameplayStatusHudRuntime.EnsureHealthGauge(null);
        Slider healthSlider = healthGauge.slider;
        healthSlider.maxValue = 100f;
        healthSlider.value = 100f;

        InvokePrivate(debugPage, "SetMaxHp", 300f);

        Assert.AreEqual(300f, core.stats.maxHp);
        Assert.AreEqual(300f, healthSlider.maxValue);
        Assert.AreEqual(100f, healthSlider.value);
    }

    [Test]
    public void StructureGaugeTracksSpecialStructuresInBackpack()
    {
        BackpackMananger backpack = new GameObject("RuntimeBackpackManager").AddComponent<BackpackMananger>();
        GameplayStatusHudRuntime.EnsureHealthGauge(null);

        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        GameplayStatusHudRuntime.RefreshStructureProgressText();

        GameObject structureRow = GameObject.Find("StructureRow");
        Assert.IsNotNull(structureRow);

        Slider structureSlider = structureRow.GetComponentInChildren<Slider>(true);
        TextMeshProUGUI valueText = structureRow.GetComponentInChildren<TextMeshProUGUI>(true);

        Assert.IsNotNull(structureSlider);
        Assert.IsNotNull(valueText);
        Assert.AreEqual(BackpackMananger.MaxSpecialStructureMaterialCount, structureSlider.maxValue);
        Assert.AreEqual(2f, structureSlider.value);
        Assert.AreEqual("2/3", valueText.text);
    }

    [Test]
    public void DebugPageCanCreateInBaseAndExistingRuntimeScenes()
    {
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "NewBase"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "BaseScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "MainScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "DeadScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "IllustratedUIScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "GameScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "FirstPass_1"));
    }

    [Test]
    public void DebugPanelHotkeyCanOpenOverBlockingGameplayUi()
    {
        Assert.IsTrue(InvokePrivateStaticBool("CanOpenPanelFromHotkey", true));
        Assert.IsTrue(InvokePrivateStaticBool("CanOpenPanelFromHotkey", false));
    }

    [Test]
    public void BaseDebugPanelKeepsGeneralDebugTools()
    {
        Scene originalScene = SceneManager.GetActiveScene();
        Scene baseScene = SceneManager.CreateScene("NewBase");

        try
        {
            Assert.IsTrue(SceneManager.SetActiveScene(baseScene));
            GameDebugPageBootstrapper debugPage = CreateDebugPage();

            InvokePrivate(debugPage, "Build");

            Transform content = debugPage.transform.Find("RuntimeDebugCanvas/DebugPanel/ScrollView/Viewport/Content");
            Assert.NotNull(content);
            Assert.NotNull(content.Find("基地调试"));
            Assert.NotNull(content.Find("玩家属性"));
            Assert.NotNull(content.Find("临时构筑"));
            Assert.NotNull(content.Find("时间与倒计时"));
            Assert.NotNull(content.Find("场景与敌人"));
        }
        finally
        {
            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalScene);
            }

            if (baseScene.IsValid() && baseScene.isLoaded)
            {
                EditorSceneManager.CloseScene(baseScene, true);
            }
        }
    }

    [Test]
    public void RuntimeSceneSystemsArePreparedOnlyForGameplayScopes()
    {
        Assert.IsFalse(InvokePrivateStaticBool("ShouldEnsureBackpackForScene", "MainScene"));
        Assert.IsFalse(InvokePrivateStaticBool("ShouldEnsureCountdownForScene", "MainScene"));
        Assert.IsTrue(InvokePrivateStaticBool("ShouldEnsureBackpackForScene", "NewBase"));
        Assert.IsFalse(InvokePrivateStaticBool("ShouldEnsureCountdownForScene", "NewBase"));
        Assert.IsTrue(InvokePrivateStaticBool("ShouldEnsureBackpackForScene", "GameScene"));
        Assert.IsTrue(InvokePrivateStaticBool("ShouldEnsureCountdownForScene", "GameScene"));
    }

    [Test]
    public void ManualScrollDeltaMovesScrollableDebugContent()
    {
        GameObject scrollObject = new GameObject("DebugScroll", typeof(RectTransform));
        try
        {
            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            RectTransform viewport = CreateRectTransform("Viewport", scrollObject.transform, new Vector2(100f, 100f));
            RectTransform content = CreateRectTransform("Content", viewport, new Vector2(100f, 300f));
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalNormalizedPosition = 0.5f;

            InvokePrivateStatic("ApplyManualScrollDelta", scrollRect, -10f);
            Assert.AreEqual(0f, scrollRect.verticalNormalizedPosition);

            InvokePrivateStatic("ApplyManualScrollDelta", scrollRect, 10f);
            Assert.AreEqual(0.8f, scrollRect.verticalNormalizedPosition, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(scrollObject);
        }
    }

    [Test]
    public void ClearPhotoAlbumRemovesAlbumData()
    {
        string tempAlbumDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArcitectureDebugAlbumTests",
            System.Guid.NewGuid().ToString("N"));

        try
        {
            using (PhotoAlbumRepository.UseAlbumDirectoryForTests(tempAlbumDirectory))
            {
                PhotoAlbumRepository.SaveCapture(
                    new byte[] { 1, 2, 3 },
                    160,
                    90,
                    "GameScene",
                    "stage_01");
                Assert.IsTrue(PhotoAlbumRepository.HasEntries());

                GameDebugPageBootstrapper debugPage = CreateDebugPage();
                InvokePrivate(debugPage, "ClearPhotoAlbum");

                Assert.IsFalse(PhotoAlbumRepository.HasEntries());
            }
        }
        finally
        {
            if (Directory.Exists(tempAlbumDirectory))
            {
                Directory.Delete(tempAlbumDirectory, true);
            }
        }
    }

    [Test]
    public void SkillDebugRowAddsSpecialStructureBackpackItems()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        runtimeState.ResetProgress(false);
        BackpackMananger backpack = new GameObject("RuntimeBackpackManager").AddComponent<BackpackMananger>();

        GameDebugPageBootstrapper debugPage = CreateDebugPage();
        GameObject contentObject = new GameObject("DebugContent", typeof(RectTransform));

        try
        {
            InvokePrivate(debugPage, "BuildSkillSection", contentObject.transform);

            Transform section = contentObject.transform.Find("临时构筑");
            Assert.NotNull(section);
            Assert.IsNull(section.Find("通用材料"));

            Transform row = section.Find("专用结构");
            Assert.NotNull(row);

            Button[] buttons = row.GetComponentsInChildren<Button>(true);
            Assert.GreaterOrEqual(buttons.Length, 2);

            buttons[0].onClick.Invoke();
            Assert.AreEqual(1, backpack.GetSpecialStructureMaterialCount());
            Assert.AreEqual(0, runtimeState.AvailableSpecialStructureInventory);

            buttons[1].onClick.Invoke();
            Assert.AreEqual(3, backpack.GetSpecialStructureMaterialCount());
            Assert.AreEqual(0, runtimeState.AvailableSpecialStructureInventory);
        }
        finally
        {
            Object.DestroyImmediate(contentObject);
        }
    }

    [Test]
    public void ProgressDebugRowCanFillAllDedicatedProgressSlots()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        runtimeState.ResetProgress(false);

        GameDebugPageBootstrapper debugPage = CreateDebugPage();
        GameObject contentObject = new GameObject("DebugContent", typeof(RectTransform));

        try
        {
            InvokePrivate(debugPage, "BuildProgressSection", contentObject.transform);

            Transform section = contentObject.transform.Find("图鉴进度");
            Assert.NotNull(section);

            Transform dedicatedRow = section.Find("专用进度");
            Assert.NotNull(dedicatedRow);

            Button[] buttons = dedicatedRow.GetComponentsInChildren<Button>(true);
            Assert.AreEqual(1, buttons.Length);

            buttons[0].onClick.Invoke();

            foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
            {
                int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
                int commonCap = Mathf.Clamp(
                    Mathf.RoundToInt(definition.requiredProgress * 0.7f),
                    0,
                    definition.requiredProgress);
                int expectedDedicatedProgress = Mathf.Max(0, definition.requiredProgress - commonCap);

                Assert.AreEqual(slotCount, runtimeState.GetUnlockedSlotCount(definition.buildingId));
                Assert.AreEqual(expectedDedicatedProgress, runtimeState.GetBuildingProgress(definition.buildingId));
                Assert.IsFalse(runtimeState.IsBuildingUnlocked(definition.buildingId));
            }
        }
        finally
        {
            Object.DestroyImmediate(contentObject);
        }
    }

    private GameDebugPageBootstrapper CreateDebugPage()
    {
        debugObject = new GameObject("RuntimeDebugPage");
        return debugObject.AddComponent<GameDebugPageBootstrapper>();
    }

    private CharacterCore CreatePlayerCore()
    {
        playerObject = new GameObject("Player");
        CharacterCore core = playerObject.AddComponent<CharacterCore>();
        core.stats.maxHp = 100f;
        core.currentHp = 100f;
        return core;
    }

    private static RectTransform CreateRectTransform(string name, Transform parent, Vector2 size)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        return rectTransform;
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(target, args);
    }

    private static bool InvokePrivateStaticBool(string methodName, string sceneName)
    {
        MethodInfo method = typeof(GameDebugPageBootstrapper).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, new object[] { sceneName });
    }

    private static bool InvokePrivateStaticBool(string methodName, bool value)
    {
        MethodInfo method = typeof(GameDebugPageBootstrapper).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, new object[] { value });
    }

    private static void InvokePrivateStatic(string methodName, params object[] args)
    {
        MethodInfo method = typeof(GameDebugPageBootstrapper).GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(null, args);
    }
}
