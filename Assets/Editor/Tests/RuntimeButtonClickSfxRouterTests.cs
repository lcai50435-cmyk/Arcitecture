using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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

public sealed class PlayerLoadoutRuntimeTests
{
    [TearDown]
    public void TearDown()
    {
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
    public void BackpackStructureControlsEffectiveWeaponWhenNoDebugOverride()
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

            Assert.AreEqual(WeaponType.BurstInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack));
        }
        finally
        {
            Object.DestroyImmediate(backpackObject);
        }
    }

    [Test]
    public void BackpackStructureOverridesDebugWeaponFallback()
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

            Assert.AreEqual(WeaponType.PierceInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack));
        }
        finally
        {
            Object.DestroyImmediate(backpackObject);
        }
    }
}

public sealed class PhotoAlbumRepositoryTests
{
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
    public void DebugPageCanCreateInAnyNamedScene()
    {
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "MainScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "DeadScene"));
        Assert.IsTrue(InvokePrivateStaticBool("CanCreateInScene", "IllustratedUIScene"));
    }

    [Test]
    public void DebugPanelHotkeyCannotOpenWhileBlockingGameplayUiIsOpen()
    {
        Assert.IsFalse(InvokePrivateStaticBool("CanOpenPanelFromHotkey", true));
        Assert.IsTrue(InvokePrivateStaticBool("CanOpenPanelFromHotkey", false));
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
