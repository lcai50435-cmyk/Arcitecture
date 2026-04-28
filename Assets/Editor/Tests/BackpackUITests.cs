using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackpackUITests
{
    private GameObject rootObject;

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }
    }

    [Test]
    public void TryGetSlotScreenPositionUsesCurrentRuntimeLayoutSlot()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();

        RectTransform staleSurface = CreateRect(rootRect, "RuntimeBackpackSurface", new Vector2(-740f, -360f), new Vector2(80f, 80f));
        RectTransform staleSlot = CreateRect(staleSurface, "slot_1", Vector2.zero, new Vector2(80f, 80f));
        BackpackSlot staleSlotBehaviour = staleSlot.gameObject.AddComponent<BackpackSlot>();
        staleSlotBehaviour.slotIndex = 0;

        RectTransform itemPanel = CreateRect(rootRect, "ItemPanel", new Vector2(180f, 72f), new Vector2(600f, 120f));
        RectTransform visibleSlot = CreateRect(itemPanel, "slot_1", new Vector2(260f, 12f), new Vector2(80f, 80f));

        backpackUi.ConfigureRuntimeLayout();
        Canvas.ForceUpdateCanvases();

        Assert.IsFalse(staleSurface.gameObject.activeSelf);
        Assert.IsTrue(backpackUi.TryGetSlotScreenPosition(0, out Vector2 actualScreenPosition, out Vector2 slotSize));

        Vector2 visibleSlotScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            visibleSlot.TransformPoint(visibleSlot.rect.center));
        Vector2 staleSlotScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            staleSlot.TransformPoint(staleSlot.rect.center));

        Assert.That(Vector2.Distance(actualScreenPosition, visibleSlotScreenPosition), Is.LessThan(0.5f));
        Assert.That(Vector2.Distance(actualScreenPosition, staleSlotScreenPosition), Is.GreaterThan(1f));
        Assert.That(slotSize, Is.EqualTo(new Vector2(80f, 80f)));
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = rectObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;

        return rectTransform;
    }
}
