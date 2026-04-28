using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class IllustratedPhotoAlbumPageBinderTests
{
    private GameObject rootObject;
    private readonly List<Texture2D> generatedTextures = new List<Texture2D>();

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            UnityEngine.Object.DestroyImmediate(rootObject);
        }

        for (int i = 0; i < generatedTextures.Count; i++)
        {
            if (generatedTextures[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(generatedTextures[i]);
            }
        }

        generatedTextures.Clear();
    }

    [Test]
    public void RefreshFillsSceneAuthoredSlotsPreviewAndPageNumber()
    {
        RectTransform root = CreateRoot();
        RectTransform preview = CreateImage(root, "Photo", new Vector2(-420f, 70f), new Vector2(260f, 200f));
        TMP_Text title = CreateText(root, "Name", "Name");
        TMP_Text time = CreateText(root, "Time", "拍摄时间 : 0000/00/00 00:00");
        TMP_Text scene = CreateText(root, "Scene", "拍摄地点 : location");
        TMP_Text pageNumber = CreateText(root, "PageNumber", "0/0");

        CreateImage(root, "Dropdown", new Vector2(120f, 290f), new Vector2(160f, 52f));

        GameObject rightPanelObject = new GameObject("RightPanel", typeof(RectTransform));
        RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
        rightPanel.SetParent(root, false);
        rightPanel.sizeDelta = new Vector2(520f, 520f);

        List<RectTransform> slots = new List<RectTransform>();
        for (int i = 0; i < 12; i++)
        {
            int row = i / 3;
            int column = i % 3;
            slots.Add(CreateImage(rightPanel, $"Image_{i + 1}", new Vector2(80f + column * 130f, 150f - row * 120f), new Vector2(96f, 96f)));
        }

        List<PhotoAlbumEntry> entries = CreateEntries(13);
        IllustratedPhotoAlbumPageBinder binder = new IllustratedPhotoAlbumPageBinder(
            () => entries,
            entry => CreateTexture(entry.fileName),
            false);

        binder.Bind(root);
        binder.Refresh();

        Assert.AreEqual("1/2", pageNumber.text.Trim());
        Assert.That(title.text, Does.Contain("第 1 张"));
        Assert.That(time.text, Does.Contain("拍摄时间"));
        Assert.That(scene.text, Does.Contain("GameScene"));

        RawImage previewImage = preview.GetComponentInChildren<RawImage>(true);
        Assert.IsNotNull(previewImage);
        Assert.IsNotNull(previewImage.texture);

        RawImage[] slotImages = slots
            .Select(slot => slot.GetComponentInChildren<RawImage>(true))
            .ToArray();
        Assert.AreEqual(12, slotImages.Length);
        Assert.IsTrue(slotImages.All(image => image != null && image.texture != null));
    }

    [Test]
    public void RefreshFillsNestedSceneAuthoredRightPageSlots()
    {
        RectTransform root = CreateRoot();
        RectTransform preview = CreateImage(root, "Photo", new Vector2(-420f, 70f), new Vector2(260f, 200f));
        CreateText(root, "Name", "Name");
        CreateText(root, "Time", "拍摄时间 : 0000/00/00 00:00");
        CreateText(root, "Scene", "拍摄地点 : location");
        CreateText(root, "PageNumber", "0/0");

        GameObject rightPanelObject = new GameObject("RightPanel", typeof(RectTransform));
        RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
        rightPanel.SetParent(root, false);
        rightPanel.sizeDelta = new Vector2(520f, 520f);

        GameObject slotContainerObject = new GameObject("Photo", typeof(RectTransform));
        RectTransform slotContainer = slotContainerObject.GetComponent<RectTransform>();
        slotContainer.SetParent(rightPanel, false);
        slotContainer.sizeDelta = new Vector2(520f, 520f);

        List<RectTransform> slots = new List<RectTransform>();
        for (int i = 0; i < 12; i++)
        {
            int row = i / 3;
            int column = i % 3;
            slots.Add(CreateImage(slotContainer, $"Image_{i + 1}", new Vector2(80f + column * 130f, 150f - row * 120f), new Vector2(96f, 96f)));
        }

        List<PhotoAlbumEntry> entries = CreateEntries(12);
        IllustratedPhotoAlbumPageBinder binder = new IllustratedPhotoAlbumPageBinder(
            () => entries,
            entry => CreateTexture(entry.fileName),
            false);

        binder.Bind(root);
        binder.Refresh();

        RawImage previewImage = preview.GetComponentInChildren<RawImage>(true);
        Assert.IsNotNull(previewImage);
        Assert.AreEqual("photo_1.png", previewImage.texture.name);

        RawImage[] slotImages = slots
            .Select(slot => slot.GetComponentInChildren<RawImage>(true))
            .ToArray();
        Assert.AreEqual(12, slotImages.Length);
        Assert.IsTrue(slotImages.All(image => image != null && image.texture != null));

        Button secondSlotButton = slots[1].GetComponent<Button>();
        Assert.IsNotNull(secondSlotButton);
        secondSlotButton.onClick.Invoke();

        Assert.AreEqual("photo_2.png", previewImage.texture.name);
    }

    [Test]
    public void ReleaseResetsSelectionSoNextOpenDefaultsToFirstPhoto()
    {
        RectTransform root = CreateRoot();
        RectTransform preview = CreateImage(root, "Photo", new Vector2(-420f, 70f), new Vector2(260f, 200f));
        CreateText(root, "Name", "Name");
        CreateText(root, "Time", "拍摄时间 : 0000/00/00 00:00");
        CreateText(root, "Scene", "拍摄地点 : location");
        CreateText(root, "PageNumber", "0/0");

        GameObject rightPanelObject = new GameObject("RightPanel", typeof(RectTransform));
        RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
        rightPanel.SetParent(root, false);
        rightPanel.sizeDelta = new Vector2(520f, 520f);

        List<RectTransform> slots = new List<RectTransform>();
        for (int i = 0; i < 3; i++)
        {
            slots.Add(CreateImage(rightPanel, $"Image_{i + 1}", new Vector2(80f + i * 130f, 150f), new Vector2(96f, 96f)));
        }

        List<PhotoAlbumEntry> entries = CreateEntries(3);
        IllustratedPhotoAlbumPageBinder binder = new IllustratedPhotoAlbumPageBinder(
            () => entries,
            entry => CreateTexture(entry.fileName),
            false);

        binder.Bind(root);
        binder.Refresh();
        slots[1].GetComponent<Button>().onClick.Invoke();

        RawImage previewImage = preview.GetComponentInChildren<RawImage>(true);
        Assert.AreEqual("photo_2.png", previewImage.texture.name);

        binder.Release();
        binder.Bind(root);
        binder.Refresh();

        Assert.AreEqual("photo_1.png", previewImage.texture.name);
    }

    [Test]
    public void DeleteSelectedButtonRemovesCurrentPhotoAndRefreshesPreview()
    {
        RectTransform root = CreateRoot();
        RectTransform preview = CreateImage(root, "Photo", new Vector2(-420f, 70f), new Vector2(260f, 200f));
        CreateText(root, "Name", "Name");
        CreateText(root, "Time", "拍摄时间 : 0000/00/00 00:00");
        CreateText(root, "Scene", "拍摄地点 : location");
        CreateText(root, "PageNumber", "0/0");

        GameObject rightPanelObject = new GameObject("RightPanel", typeof(RectTransform));
        RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
        rightPanel.SetParent(root, false);
        rightPanel.sizeDelta = new Vector2(520f, 520f);

        List<RectTransform> slots = new List<RectTransform>();
        for (int i = 0; i < 3; i++)
        {
            slots.Add(CreateImage(rightPanel, $"Image_{i + 1}", new Vector2(80f + i * 130f, 150f), new Vector2(96f, 96f)));
        }

        List<PhotoAlbumEntry> entries = CreateEntries(3);
        string deletedEntryId = null;
        string deletedFileName = null;
        IllustratedPhotoAlbumPageBinder binder = new IllustratedPhotoAlbumPageBinder(
            () => entries,
            entry => CreateTexture(entry.fileName),
            entry =>
            {
                deletedEntryId = entry.id;
                deletedFileName = entry.fileName;
                return entries.Remove(entry);
            },
            false);

        binder.Bind(root);
        binder.Refresh();
        slots[1].GetComponent<Button>().onClick.Invoke();

        Button deleteButton = FindButton(root, "RuntimePhotoAlbumDeleteSelected");
        Assert.IsNotNull(deleteButton);
        deleteButton.onClick.Invoke();

        RawImage previewImage = preview.GetComponentInChildren<RawImage>(true);
        Assert.AreEqual("photo_3.png", previewImage.texture.name);
        Assert.AreEqual(2, entries.Count);
        Assert.IsNotNull(deletedEntryId);
        Assert.AreEqual("photo_2.png", deletedFileName);
    }

    [Test]
    public void DeleteSelectedButtonMatchesShareButtonSizeAndAlignsToItsLeft()
    {
        RectTransform root = CreateRoot();
        RectTransform preview = CreateImage(root, "Photo", new Vector2(-420f, 70f), new Vector2(260f, 200f));
        CreateText(root, "Name", "Name");
        CreateText(root, "Time", "拍摄时间 : 0000/00/00 00:00");
        CreateText(root, "Scene", "拍摄地点 : location");
        CreateText(root, "PageNumber", "0/0");
        RectTransform shareButton = CreateImage(root, "ShareButton", new Vector2(79f, -89.5f), new Vector2(15f, 15f));

        GameObject rightPanelObject = new GameObject("RightPanel", typeof(RectTransform));
        RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
        rightPanel.SetParent(root, false);
        rightPanel.sizeDelta = new Vector2(520f, 520f);
        CreateImage(rightPanel, "Image_1", new Vector2(80f, 150f), new Vector2(96f, 96f));

        List<PhotoAlbumEntry> entries = CreateEntries(1);
        IllustratedPhotoAlbumPageBinder binder = new IllustratedPhotoAlbumPageBinder(
            () => entries,
            entry => CreateTexture(entry.fileName),
            false);

        binder.Bind(root);
        binder.Refresh();

        Button deleteButton = FindButton(root, "RuntimePhotoAlbumDeleteSelected");
        Assert.IsNotNull(deleteButton);
        RectTransform deleteRect = deleteButton.GetComponent<RectTransform>();

        Assert.AreEqual(shareButton.sizeDelta, deleteRect.sizeDelta);
        Assert.AreEqual(shareButton.anchoredPosition.y, deleteRect.anchoredPosition.y, 0.001f);
        Assert.AreEqual(
            shareButton.anchoredPosition.x - shareButton.sizeDelta.x - 8f,
            deleteRect.anchoredPosition.x,
            0.001f);
    }

    private RectTransform CreateRoot()
    {
        rootObject = new GameObject("PhotoAlbumCanvas", typeof(RectTransform));
        RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1280f, 720f);
        return rectTransform;
    }

    private static RectTransform CreateImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = new Vector2(300f, 40f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        return text;
    }

    private static Button FindButton(Transform root, string name)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == name)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private static List<PhotoAlbumEntry> CreateEntries(int count)
    {
        List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>();
        for (int i = 0; i < count; i++)
        {
            entries.Add(new PhotoAlbumEntry
            {
                id = Guid.NewGuid().ToString("N"),
                fileName = $"photo_{i + 1}.png",
                savedAtUtc = new DateTime(2026, 4, 24, 14, i, 0, DateTimeKind.Utc).ToString("O"),
                sceneName = "GameScene",
                stageId = "stage_01",
                width = 160,
                height = 90
            });
        }

        return entries;
    }

    private Texture2D CreateTexture(string name)
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            name = name
        };
        Color[] pixels = Enumerable.Repeat(Color.white, 16).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();
        generatedTextures.Add(texture);
        return texture;
    }
}
