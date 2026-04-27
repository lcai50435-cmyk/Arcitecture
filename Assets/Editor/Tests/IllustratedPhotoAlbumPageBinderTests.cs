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
