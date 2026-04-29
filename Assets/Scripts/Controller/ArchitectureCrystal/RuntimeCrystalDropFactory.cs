using System.Collections.Generic;
using UnityEngine;

public enum RuntimeDropPresentation
{
    DirectCrystal,
    ClosedLootBag
}

public static class RuntimeCrystalDropFactory
{
    public const float ClosedLootBagWorldScale = 0.0875f;

    private const string ClosedLootBagSpritePath = "Assets/File/Prop/Prop/ItemBag_1.png";
    private static readonly Dictionary<string, Sprite> fallbackSpriteCache = new Dictionary<string, Sprite>();
    private static Sprite closedLootBagSprite;

    public static GameObject CreateInteractiveDrop(
        ArchitecturalCrystal crystal,
        Vector3 position,
        float scale = 0.35f,
        int sortingOrder = 4,
        Transform parent = null,
        string objectName = null,
        RuntimeDropPresentation presentation = RuntimeDropPresentation.DirectCrystal)
    {
        return CreateDropInternal(crystal, position, scale, sortingOrder, true, parent, objectName, presentation);
    }

    public static GameObject CreateVisualDrop(
        ArchitecturalCrystal crystal,
        Vector3 position,
        float scale = 0.3f,
        int sortingOrder = 8,
        Transform parent = null,
        string objectName = null)
    {
        return CreateDropInternal(crystal, position, scale, sortingOrder, false, parent, objectName);
    }

    public static Sprite ResolveSprite(ArchitecturalCrystal crystal)
    {
        if (crystal.icon != null)
        {
            return crystal.icon;
        }

        if (crystal.IsSpecialStructure)
        {
            return ArchitecturalCrystalFactory.CreateSpecialStructureMaterial().icon;
        }

        if (crystal.IsRepairMaterial)
        {
            return ArchitecturalCrystalFactory.CreateRepairMaterial(crystal.repairBuildingId).icon;
        }

        if (crystal.IsInkSupply)
        {
            return ArchitecturalCrystalFactory.CreateInkSupply(
                crystal.type == ArchitecturalType.LargeInkBottle || crystal.inkRestoreValue >= 50).icon;
        }

        ArchitecturalCrystalVisualSet visuals = ArchitecturalCrystalVisualResolver.Resolve(
            crystal.type,
            crystal.Category,
            crystal.icon,
            crystal.backIcon);
        if (visuals.icon != null)
        {
            return visuals.icon;
        }

        string key = $"{crystal.Category}_{crystal.type}";
        if (fallbackSpriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Color color = crystal.IsSpecialStructure
            ? new Color(0.98f, 0.82f, 0.26f, 1f)
            : crystal.IsRepairMaterial
                ? new Color(0.34f, 0.88f, 0.68f, 1f)
                : crystal.IsInkSupply
                    ? new Color(0.24f, 0.74f, 0.92f, 1f)
                    : new Color(0.92f, 0.92f, 0.92f, 1f);

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= 6.8f ? color : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        sprite.name = $"RuntimeDropSprite_{key}";
        fallbackSpriteCache[key] = sprite;
        return sprite;
    }

    private static GameObject CreateDropInternal(
        ArchitecturalCrystal crystal,
        Vector3 position,
        float scale,
        int sortingOrder,
        bool interactive,
        Transform parent,
        string objectName,
        RuntimeDropPresentation presentation = RuntimeDropPresentation.DirectCrystal)
    {
        string runtimeName = string.IsNullOrEmpty(objectName)
            ? (interactive ? $"Drop_{crystal.DisplayName}" : $"VisualDrop_{crystal.DisplayName}")
            : objectName;

        GameObject dropObject = new GameObject(runtimeName);
        if (parent != null)
        {
            dropObject.transform.SetParent(parent, false);
        }

        dropObject.transform.position = position;
        dropObject.transform.localScale = new Vector3(scale, scale, 1f);

        Sprite revealedSprite = ResolveSprite(crystal);
        Sprite dropSprite = interactive && presentation == RuntimeDropPresentation.ClosedLootBag
            ? ResolveClosedLootBagSprite() ?? revealedSprite
            : revealedSprite;
        Sprite sanitizedDropSprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(dropSprite);
        Sprite sanitizedRevealedSprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(revealedSprite);

        SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sanitizedDropSprite != null ? sanitizedDropSprite : dropSprite;
        renderer.sortingOrder = sortingOrder;

        if (interactive)
        {
            CircleCollider2D collider = dropObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;

            CrystalInteractHandler handler = dropObject.AddComponent<CrystalInteractHandler>();
            handler.type = crystal.type;
            handler.expValue = crystal.expValue;
            handler.buildProgressPercent = crystal.buildProgressPercent;
            handler.icon = crystal.icon != null
                ? RuntimeSpriteDisplaySanitizer.GetDisplaySprite(crystal.icon)
                : (sanitizedRevealedSprite != null ? sanitizedRevealedSprite : revealedSprite);
            handler.backIcon = crystal.backIcon != null
                ? RuntimeSpriteDisplaySanitizer.GetDisplaySprite(crystal.backIcon)
                : handler.icon;
            handler.bonusType = crystal.bonusType;
            handler.bonusValue = crystal.bonusValue;
            handler.subBonusType = crystal.subBonusType;
            handler.subBonusValue = crystal.subBonusValue;
            handler.isUnlockMaterial = crystal.isUnlockMaterial;
            handler.resourceCategory = crystal.resourceCategory;
            handler.repairBuildingId = crystal.repairBuildingId;
            handler.inkRestoreValue = crystal.inkRestoreValue;
            handler.textDescription = crystal.textDescription;
            handler.persistCollectedAcrossSceneLoads = false;
            handler.startClosedAsLootBag = presentation == RuntimeDropPresentation.ClosedLootBag;
            handler.closedLootBagSprite = sanitizedDropSprite != null ? sanitizedDropSprite : dropSprite;
            handler.revealedLootSprite = sanitizedRevealedSprite != null ? sanitizedRevealedSprite : revealedSprite;
        }

        return dropObject;
    }

    private static Sprite ResolveClosedLootBagSprite()
    {
        if (closedLootBagSprite != null)
        {
            return closedLootBagSprite;
        }

        closedLootBagSprite = LoadProjectSprite(ClosedLootBagSpritePath);
        return closedLootBagSprite;
    }

    private static Sprite LoadProjectSprite(string assetPath)
    {
        return RuntimeProjectSpriteLoader.LoadSprite(assetPath, true);
    }
}
