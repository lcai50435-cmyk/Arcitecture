using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuControllerSlotPanelTests
{
    private GameObject controllerObject;
    private GameObject parentObject;

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

        Text detailText = card.Find("Content/Left/Detail")?.GetComponent<Text>();
        Assert.IsNotNull(detailText);
        Assert.AreEqual(VerticalWrapMode.Truncate, detailText.verticalOverflow);

        foreach (Text text in card.GetComponentsInChildren<Text>(true))
        {
            Assert.IsFalse(text.raycastTarget, $"{text.name} should not intercept save-slot clicks.");
        }
    }

    [Test]
    public void SlotDetailLeavesStateToBadgeAndUsesAtMostTwoLines()
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
        Assert.LessOrEqual(detail.Split('\n').Length, 2);
    }

    [Test]
    public void DeleteConfirmationHintNamesSecondClick()
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

        Assert.That(hint, Does.Contain("再次点击"));
        Assert.That(hint, Does.Contain("确认删除"));
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
}
