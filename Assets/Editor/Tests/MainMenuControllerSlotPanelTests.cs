using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuControllerSlotPanelTests
{
    private GameObject controllerObject;
    private GameObject parentObject;
    private string tempDirectory;

    [TearDown]
    public void TearDown()
    {
        if (controllerObject != null)
        {
            UnityEngine.Object.DestroyImmediate(controllerObject);
        }

        if (parentObject != null)
        {
            UnityEngine.Object.DestroyImmediate(parentObject);
        }

        if (!string.IsNullOrWhiteSpace(tempDirectory) && Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    [Test]
    public void SlotCardTextFitsCardAndDoesNotBlockButtonRaycasts()
    {
        MainMenuController controller = CreateController();

        MethodInfo createSlotCard = typeof(MainMenuController).GetMethod(
            "CreateSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createSlotCard);

        createSlotCard.Invoke(controller, new object[] { parentObject.transform, 1 });

        Transform card = parentObject.transform.Find("SlotCard_1");
        Assert.IsNotNull(card);

        LayoutElement cardLayout = card.GetComponent<LayoutElement>();
        Assert.IsNotNull(cardLayout);
        Assert.GreaterOrEqual(cardLayout.preferredHeight, 136f);

        Text detailText = card.Find("Save_1Panel/Content/Left/Detail")?.GetComponent<Text>();
        Assert.IsNotNull(detailText);
        Assert.AreEqual(VerticalWrapMode.Truncate, detailText.verticalOverflow);

        foreach (Text text in card.GetComponentsInChildren<Text>(true))
        {
            Assert.IsFalse(text.raycastTarget, $"{text.name} should not intercept save-slot clicks.");
        }
    }

    [Test]
    public void SlotCardUsesAuthoredSaveSceneGeometry()
    {
        MainMenuController controller = CreateController();

        MethodInfo createSlotCard = typeof(MainMenuController).GetMethod(
            "CreateSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createSlotCard);

        createSlotCard.Invoke(controller, new object[] { parentObject.transform, 2 });

        Transform card = parentObject.transform.Find("SlotCard_2");
        Assert.IsNotNull(card);

        RectTransform cardRect = (RectTransform)card;
        AssertVector2(cardRect.anchoredPosition, new Vector2(0f, -210f));
        AssertVector2(cardRect.sizeDelta, new Vector2(100f, 100f));

        RectTransform panelRect = card.Find("Save_1Panel") as RectTransform;
        Assert.IsNotNull(panelRect);
        AssertVector2(panelRect.sizeDelta, new Vector2(840f, 190f));
        AssertVector2(panelRect.anchoredPosition, new Vector2(0f, 247f));

        RectTransform previewRect = card.Find("Save_1Panel/Content/Preview") as RectTransform;
        Assert.IsNotNull(previewRect);
        AssertVector2(previewRect.sizeDelta, new Vector2(232.3407f, 170.6831f));

        RectTransform textRect = card.Find("Save_1Panel/Content/Left") as RectTransform;
        Assert.IsNotNull(textRect);

        RectTransform deleteRect = card.Find("Dele") as RectTransform;
        Assert.IsNotNull(deleteRect);
        AssertVector2(deleteRect.sizeDelta, new Vector2(50f, 50f));
        Assert.Less(GetRightWorldX(textRect), GetLeftWorldX(deleteRect));
    }

    [Test]
    public void SlotCardTextColumnControlsChildHeights()
    {
        MainMenuController controller = CreateController();

        MethodInfo createSlotCard = typeof(MainMenuController).GetMethod(
            "CreateSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createSlotCard);

        createSlotCard.Invoke(controller, new object[] { parentObject.transform, 1 });

        Transform textColumn = parentObject.transform.Find("SlotCard_1/Save_1Panel/Content/Left");
        Assert.IsNotNull(textColumn);

        VerticalLayoutGroup layoutGroup = textColumn.GetComponent<VerticalLayoutGroup>();
        Assert.IsNotNull(layoutGroup);
        Assert.IsTrue(layoutGroup.childControlHeight);
        Assert.IsFalse(layoutGroup.childForceExpandHeight);

        float availableHeight = ((RectTransform)textColumn).rect.height;
        float requiredHeight = layoutGroup.spacing * 2f;
        requiredHeight += GetPreferredHeight(textColumn.Find("State"));
        requiredHeight += GetPreferredHeight(textColumn.Find("Title"));
        requiredHeight += GetPreferredHeight(textColumn.Find("Detail"));

        Assert.LessOrEqual(requiredHeight, availableHeight);
    }

    [Test]
    public void EmptySlotPreviewIsTransparent()
    {
        MainMenuController controller = CreateController();

        MethodInfo createSlotCard = typeof(MainMenuController).GetMethod(
            "CreateSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo refreshSlotCard = typeof(MainMenuController).GetMethod(
            "RefreshSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createSlotCard);
        Assert.IsNotNull(refreshSlotCard);

        object slotCardView = createSlotCard.Invoke(controller, new object[] { parentObject.transform, 1 });
        SaveSlotSummary emptySummary = new SaveSlotSummary
        {
            slotId = 1,
            hasSave = false
        };

        refreshSlotCard.Invoke(controller, new[] { slotCardView, emptySummary });

        Transform previewImageTransform = parentObject.transform.Find("SlotCard_1/Save_1Panel/Content/Preview/Image");
        Assert.IsNotNull(previewImageTransform);
        Image previewImage = previewImageTransform.GetComponent<Image>();
        Assert.IsNotNull(previewImage);
        Assert.IsNull(previewImage.sprite);
        Assert.AreEqual(0f, previewImage.color.a);
    }

    [Test]
    public void SavedSlotPreviewUsesPreviewImagePath()
    {
        MainMenuController controller = CreateController();

        MethodInfo createSlotCard = typeof(MainMenuController).GetMethod(
            "CreateSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo refreshSlotCard = typeof(MainMenuController).GetMethod(
            "RefreshSlotCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createSlotCard);
        Assert.IsNotNull(refreshSlotCard);

        object slotCardView = createSlotCard.Invoke(controller, new object[] { parentObject.transform, 1 });
        SaveSlotSummary savedSummary = new SaveSlotSummary
        {
            slotId = 1,
            hasSave = true,
            previewImagePath = CreateTempPreviewPng()
        };

        refreshSlotCard.Invoke(controller, new[] { slotCardView, savedSummary });

        Image previewImage = parentObject.transform
            .Find("SlotCard_1/Save_1Panel/Content/Preview/Image")
            ?.GetComponent<Image>();
        Assert.IsNotNull(previewImage);
        Assert.IsTrue(previewImage.enabled);
        Assert.IsNotNull(previewImage.sprite);
        Assert.AreEqual(1f, previewImage.color.a);
        Assert.IsTrue(previewImage.preserveAspect);

        MethodInfo releaseSlotPreview = typeof(MainMenuController).GetMethod(
            "ReleaseSlotPreview",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(releaseSlotPreview);
        releaseSlotPreview.Invoke(null, new[] { slotCardView });
    }

    [Test]
    public void SlotDetailLeavesStateToBadgeAndUsesCompactLines()
    {
        MainMenuController controller = CreateController();
        SaveSlotSummary summary = new SaveSlotSummary
        {
            slotId = 1,
            hasSave = true,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            selectedStageId = "stage_01",
            currentWeaponType = WeaponType.DirectInk,
            progressPercent = 23f
        };

        string detail = InvokeBuildSlotDetail(controller, summary);

        Assert.That(detail, Does.Not.Contain("状态："));
        Assert.LessOrEqual(detail.Split('\n').Length, 3);
    }

    [Test]
    public void DeleteConfirmationHintNamesPrompt()
    {
        MainMenuController controller = CreateController();
        SetPrivateField(controller, "currentPanelMode", MainMenuSlotPanelMode.Continue);
        SetPrivateField(controller, "armedDeleteSlotId", 2);

        SaveSlotSummary summary = new SaveSlotSummary
        {
            slotId = 2,
            hasSave = true
        };

        MethodInfo method = typeof(MainMenuController).GetMethod(
            "BuildSelectionHint",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        string hint = (string)method.Invoke(controller, new object[] { summary, true });

        Assert.That(hint, Does.Contain("确认弹窗"));
        Assert.That(hint, Does.Contain("删除存档"));
    }

    [Test]
    public void NewGameMenuButtonUsesSettingSceneFrameSprite()
    {
        _ = CreateController();

        MethodInfo createMenuButton = typeof(MainMenuController).GetMethod(
            "CreateMenuButton",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(createMenuButton);

        Button button = (Button)createMenuButton.Invoke(
            null,
            new object[]
            {
                parentObject.transform,
                "NewGameButton",
                "新游戏",
                (UnityEngine.Events.UnityAction)(() => { })
            });

        AssertSettingSceneMenuButton(button, new Vector2(520f, 96f), new Vector2(0f, 220f));
    }

    [Test]
    public void ContinueMenuButtonUsesSettingSceneFrameSprite()
    {
        _ = CreateController();

        MethodInfo createMenuButton = typeof(MainMenuController).GetMethod(
            "CreateMenuButton",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(createMenuButton);

        Button button = (Button)createMenuButton.Invoke(
            null,
            new object[]
            {
                parentObject.transform,
                "ContinueButton",
                "继续游戏",
                (UnityEngine.Events.UnityAction)(() => { })
            });

        AssertSettingSceneMenuButton(button, new Vector2(520f, 96f), new Vector2(0f, 96f));
    }

    [Test]
    public void UtilityMainMenuButtonsUseSettingSceneFrameSpriteAndCompactUnifiedGeometry()
    {
        _ = CreateController();

        MethodInfo createMenuButton = typeof(MainMenuController).GetMethod(
            "CreateMenuButton",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(createMenuButton);

        Button handbookButton = (Button)createMenuButton.Invoke(
            null,
            new object[]
            {
                parentObject.transform,
                "HandbookButton",
                "图鉴/手册",
                (UnityEngine.Events.UnityAction)(() => { })
            });
        AssertSettingSceneMenuButton(handbookButton, new Vector2(520f, 96f), new Vector2(0f, -28f));

        Button settingsButton = (Button)createMenuButton.Invoke(
            null,
            new object[]
            {
                parentObject.transform,
                "SettingsButton",
                "设置",
                (UnityEngine.Events.UnityAction)(() => { })
            });
        AssertSettingSceneMenuButton(settingsButton, new Vector2(520f, 96f), new Vector2(0f, -152f));

        Button exitButton = (Button)createMenuButton.Invoke(
            null,
            new object[]
            {
                parentObject.transform,
                "ExitButton",
                "退出",
                (UnityEngine.Events.UnityAction)(() => { })
            });
        AssertSettingSceneMenuButton(exitButton, new Vector2(520f, 96f), new Vector2(0f, -276f));
    }

    private MainMenuController CreateController()
    {
        controllerObject = new GameObject("MainMenuControllerTestHost");
        parentObject = new GameObject("SlotCardParent", typeof(RectTransform));
        return controllerObject.AddComponent<MainMenuController>();
    }

    private static string InvokeBuildSlotDetail(MainMenuController controller, SaveSlotSummary summary)
    {
        MethodInfo method = typeof(MainMenuController).GetMethod(
            "BuildSlotDetail",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (string)method.Invoke(controller, new object[] { summary });
    }

    private static void SetPrivateField(MainMenuController controller, string fieldName, object value)
    {
        FieldInfo field = typeof(MainMenuController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(controller, value);
    }

    private static void AssertVector2(Vector2 actual, Vector2 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
    }

    private static void AssertMainMenuButtonSize(Button button, Vector2 expectedSize)
    {
        Assert.IsNotNull(button);
        RectTransform rectTransform = button.transform as RectTransform;
        Assert.IsNotNull(rectTransform);
        AssertVector2(rectTransform.sizeDelta, expectedSize);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        Assert.IsNotNull(layoutElement);
        Assert.That(layoutElement.preferredWidth, Is.EqualTo(expectedSize.x).Within(0.001f));
        Assert.That(layoutElement.preferredHeight, Is.EqualTo(expectedSize.y).Within(0.001f));
    }

    private static void AssertMainMenuButtonGeometry(Button button, Vector2 expectedSize, Vector2 expectedPosition)
    {
        AssertMainMenuButtonSize(button, expectedSize);

        RectTransform rectTransform = button.transform as RectTransform;
        Assert.IsNotNull(rectTransform);
        AssertVector2(rectTransform.anchoredPosition, expectedPosition);
    }

    private static void AssertSettingSceneMenuButton(Button button, Vector2 expectedSize, Vector2 expectedPosition)
    {
        Image image = button.GetComponent<Image>();
        Assert.IsNotNull(image);
        Assert.IsNotNull(image.sprite);
        Assert.AreEqual("SavePanelFrameSprite", image.sprite.name);
        Assert.AreEqual(Image.Type.Sliced, image.type);
        Assert.IsFalse(image.preserveAspect);
        Assert.IsNull(button.transform.Find("Accent"));
        Transform label = button.transform.Find("Label");
        Assert.IsNotNull(label);
        Text labelText = label.GetComponent<Text>();
        Assert.IsNotNull(labelText);
        Assert.IsFalse(labelText.raycastTarget);
        AssertMainMenuButtonGeometry(button, expectedSize, expectedPosition);
    }

    private static float GetLeftWorldX(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return corners[0].x;
    }

    private static float GetRightWorldX(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return corners[2].x;
    }

    private static float GetPreferredHeight(Transform transform)
    {
        LayoutElement layoutElement = transform?.GetComponent<LayoutElement>();
        Assert.IsNotNull(layoutElement);
        return layoutElement.preferredHeight;
    }

    private string CreateTempPreviewPng()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"arcitecture-save-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.red);
        texture.SetPixel(1, 0, Color.green);
        texture.SetPixel(0, 1, Color.blue);
        texture.SetPixel(1, 1, Color.white);
        texture.Apply();

        string path = Path.Combine(tempDirectory, "slot_preview.png");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        return path;
    }
}
