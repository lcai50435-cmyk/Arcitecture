using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class EnemyCombatFeedback : MonoBehaviour
{
    private static Sprite runtimeSprite;

    private CharacterCore characterCore;
    private SpriteRenderer ownerRenderer;
    private Transform healthBarRoot;
    private SpriteRenderer healthBarBackground;
    private SpriteRenderer healthBarFill;
    private float barVisibleTimer;
    private bool keepHealthBarVisibleWhileAlive;
    private int feedbackSortingLayerId;
    private int feedbackBaseSortingOrder = 30;
    private float nextHealthBarPositionRefreshTime;

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        ownerRenderer = ResolveOwnerRenderer();
        ResolveFeedbackSorting();
        EnsureVisuals();
        RefreshHealthBar();
        SetHealthBarVisible(keepHealthBarVisibleWhileAlive && IsAlive());
    }

    private void OnEnable()
    {
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }

        if (characterCore != null)
        {
            characterCore.OnTakeDamage += HandleTakeDamage;
            characterCore.OnTakeDamageWithValue += HandleTakeDamageValue;
            characterCore.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (characterCore != null)
        {
            characterCore.OnTakeDamage -= HandleTakeDamage;
            characterCore.OnTakeDamageWithValue -= HandleTakeDamageValue;
            characterCore.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextHealthBarPositionRefreshTime)
        {
            nextHealthBarPositionRefreshTime = Time.unscaledTime + 0.1f;
            UpdateHealthBarPosition();
        }

        if (keepHealthBarVisibleWhileAlive && IsAlive())
        {
            SetHealthBarVisible(true);
            return;
        }

        if (barVisibleTimer > 0f)
        {
            barVisibleTimer -= Time.deltaTime;
            if (barVisibleTimer <= 0f && characterCore != null && characterCore.currentHp > 0f)
            {
                SetHealthBarVisible(false);
            }
        }
    }

    private void HandleTakeDamage()
    {
        barVisibleTimer = keepHealthBarVisibleWhileAlive ? 0f : 1.2f;
        SetHealthBarVisible(true);
        RefreshHealthBar();
    }

    private void HandleTakeDamageValue(float damage)
    {
        SpawnDamageNumber(damage);
    }

    private void HandleDeath()
    {
        SetHealthBarVisible(false);
    }

    private void EnsureVisuals()
    {
        if (healthBarRoot != null)
        {
            return;
        }

        healthBarRoot = new GameObject("EnemyHealthBar").transform;
        healthBarRoot.SetParent(transform, false);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(healthBarRoot, false);
        healthBarBackground = backgroundObject.AddComponent<SpriteRenderer>();
        healthBarBackground.sprite = GetRuntimeSprite();
        healthBarBackground.sortingOrder = 30;
        healthBarBackground.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        healthBarBackground.transform.localScale = new Vector3(0.95f, 0.12f, 1f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(healthBarRoot, false);
        healthBarFill = fillObject.AddComponent<SpriteRenderer>();
        healthBarFill.sprite = GetRuntimeSprite();
        healthBarFill.color = new Color(0.88f, 0.23f, 0.18f, 1f);

        ApplyHealthBarSorting();
    }

    private void RefreshHealthBar()
    {
        if (characterCore == null || healthBarFill == null)
        {
            return;
        }

        float ratio = characterCore.stats != null && characterCore.stats.maxHp > 0f
            ? Mathf.Clamp01(characterCore.currentHp / characterCore.stats.maxHp)
            : 0f;

        float width = Mathf.Max(0.01f, 0.9f * ratio);
        healthBarFill.transform.localScale = new Vector3(width, 0.08f, 1f);
        healthBarFill.transform.localPosition = new Vector3(-0.45f + width * 0.5f, 0f, 0f);
    }

    private void UpdateHealthBarPosition()
    {
        if (healthBarRoot == null)
        {
            return;
        }

        float offsetY = ownerRenderer != null
            ? ownerRenderer.bounds.extents.y / Mathf.Max(0.01f, transform.lossyScale.y) + 0.3f
            : 0.9f;

        healthBarRoot.localPosition = new Vector3(0f, offsetY, 0f);
    }

    private void SetHealthBarVisible(bool visible)
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.gameObject.SetActive(visible);
        }
    }

    public void SetHealthBarVisibleWhileAlive(bool visible)
    {
        keepHealthBarVisibleWhileAlive = visible;
        RefreshHealthBar();
        SetHealthBarVisible(visible && IsAlive());
    }

    public void ResetForReuse()
    {
        barVisibleTimer = 0f;
        RefreshHealthBar();
        SetHealthBarVisible(keepHealthBarVisibleWhileAlive && IsAlive());
    }

    private void SpawnDamageNumber(float damage)
    {
        GameObject numberObject = CombatObjectPool.RentRuntime(
            "Enemy.DamageNumber",
            CreateDamageNumberObject,
            GetDamageNumberSpawnPosition(),
            Quaternion.identity);
        if (numberObject == null)
        {
            return;
        }

        string damageText = Mathf.Max(0f, damage).ToString("0");
        TextMeshPro text = numberObject.GetComponent<TextMeshPro>();
        text.text = damageText;
        text.color = new Color(1f, 0.92f, 0.18f, 1f);
        text.fontSize = 3.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.sortingLayerID = feedbackSortingLayerId;
        text.sortingOrder = feedbackBaseSortingOrder + 22;

        EnemyDamageNumberMotion motion = numberObject.GetComponent<EnemyDamageNumberMotion>();
        motion.Initialize(text);
    }

    private static GameObject CreateDamageNumberObject()
    {
        GameObject numberObject = new GameObject("DamageNumber");
        TextMeshPro text = numberObject.AddComponent<TextMeshPro>();
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        numberObject.AddComponent<EnemyDamageNumberMotion>();
        return numberObject;
    }

    private void CreateDamageNumberSprites(Transform parent, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        const float digitWidth = 0.22f;
        const float digitSpacing = 0.08f;
        float totalWidth = value.Length * digitWidth + Mathf.Max(0, value.Length - 1) * digitSpacing;
        float left = -totalWidth * 0.5f + digitWidth * 0.5f;

        for (int i = 0; i < value.Length; i++)
        {
            char digit = value[i];
            if (digit < '0' || digit > '9')
            {
                continue;
            }

            Transform shadow = new GameObject($"DigitShadow_{digit}_{i}").transform;
            shadow.SetParent(parent, false);
            shadow.localPosition = new Vector3(left + i * (digitWidth + digitSpacing) + 0.035f, -0.035f, 0f);
            CreateDigit(shadow, digit, new Color(0.16f, 0.05f, 0.02f, 0.9f), feedbackBaseSortingOrder + 21);

            Transform foreground = new GameObject($"Digit_{digit}_{i}").transform;
            foreground.SetParent(parent, false);
            foreground.localPosition = new Vector3(left + i * (digitWidth + digitSpacing), 0f, 0f);
            CreateDigit(foreground, digit, new Color(1f, 0.92f, 0.18f, 1f), feedbackBaseSortingOrder + 22);
        }
    }

    private void CreateDigit(Transform parent, char digit, Color color, int sortingOrder)
    {
        bool[] segments = ResolveDigitSegments(digit);
        CreateSegmentIfEnabled(parent, "Top", segments[0], new Vector2(0f, 0.18f), new Vector2(0.18f, 0.035f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "UpperLeft", segments[1], new Vector2(-0.09f, 0.09f), new Vector2(0.035f, 0.15f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "UpperRight", segments[2], new Vector2(0.09f, 0.09f), new Vector2(0.035f, 0.15f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "Middle", segments[3], new Vector2(0f, 0f), new Vector2(0.18f, 0.035f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "LowerLeft", segments[4], new Vector2(-0.09f, -0.09f), new Vector2(0.035f, 0.15f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "LowerRight", segments[5], new Vector2(0.09f, -0.09f), new Vector2(0.035f, 0.15f), color, sortingOrder);
        CreateSegmentIfEnabled(parent, "Bottom", segments[6], new Vector2(0f, -0.18f), new Vector2(0.18f, 0.035f), color, sortingOrder);
    }

    private void CreateSegmentIfEnabled(
        Transform parent,
        string name,
        bool enabled,
        Vector2 localPosition,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        if (!enabled)
        {
            return;
        }

        GameObject segment = new GameObject(name);
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        segment.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
        renderer.color = color;
        renderer.sortingLayerID = feedbackSortingLayerId;
        renderer.sortingOrder = sortingOrder;
    }

    private static bool[] ResolveDigitSegments(char digit)
    {
        switch (digit)
        {
            case '0': return new[] { true, true, true, false, true, true, true };
            case '1': return new[] { false, false, true, false, false, true, false };
            case '2': return new[] { true, false, true, true, true, false, true };
            case '3': return new[] { true, false, true, true, false, true, true };
            case '4': return new[] { false, true, true, true, false, true, false };
            case '5': return new[] { true, true, false, true, false, true, true };
            case '6': return new[] { true, true, false, true, true, true, true };
            case '7': return new[] { true, false, true, false, false, true, false };
            case '8': return new[] { true, true, true, true, true, true, true };
            case '9': return new[] { true, true, true, true, false, true, true };
            default: return new[] { false, false, false, false, false, false, false };
        }
    }

    private Vector3 GetDamageNumberSpawnPosition()
    {
        Vector3 center = ownerRenderer != null ? ownerRenderer.bounds.center : transform.position;
        float horizontalOffset = Random.Range(-0.24f, 0.24f);
        float verticalOffset = ownerRenderer != null
            ? ownerRenderer.bounds.extents.y + 0.35f
            : 0.75f;

        return center + new Vector3(horizontalOffset, verticalOffset, 0f);
    }

    private SpriteRenderer ResolveOwnerRenderer()
    {
        SpriteRenderer directRenderer = GetComponent<SpriteRenderer>();
        if (directRenderer != null)
        {
            return directRenderer;
        }

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i] != null)
            {
                return childRenderers[i];
            }
        }

        return null;
    }

    private void ResolveFeedbackSorting()
    {
        if (ownerRenderer == null)
        {
            feedbackSortingLayerId = 0;
            feedbackBaseSortingOrder = 30;
            return;
        }

        feedbackSortingLayerId = ownerRenderer.sortingLayerID;
        feedbackBaseSortingOrder = ownerRenderer.sortingOrder;
    }

    private void ApplyHealthBarSorting()
    {
        if (healthBarBackground == null || healthBarFill == null)
        {
            return;
        }

        if (ownerRenderer == null)
        {
            healthBarBackground.sortingOrder = 30;
            healthBarFill.sortingOrder = 31;
            return;
        }

        healthBarBackground.sortingLayerID = ownerRenderer.sortingLayerID;
        healthBarFill.sortingLayerID = ownerRenderer.sortingLayerID;
        healthBarBackground.sortingOrder = feedbackBaseSortingOrder + 4;
        healthBarFill.sortingOrder = feedbackBaseSortingOrder + 5;
    }

    private bool IsAlive()
    {
        return characterCore != null && characterCore.currentHp > 0f;
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
        {
            return runtimeSprite;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.SetPixel(0, 1, Color.white);
        texture.SetPixel(1, 0, Color.white);
        texture.SetPixel(1, 1, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        return runtimeSprite;
    }
}

public class EnemyDamageNumberMotion : MonoBehaviour
{
    private Vector3 velocity;
    private float lifetime;
    private TextMeshPro text;
    private Color initialColor;

    public void Initialize(TextMeshPro damageText)
    {
        velocity = new Vector3(Random.Range(-0.08f, 0.08f), 0.34f, 0f);
        lifetime = 0.9f;
        text = damageText;
        initialColor = text != null ? text.color : Color.white;
    }

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;
        lifetime -= Time.deltaTime;

        if (text != null)
        {
            float alphaMultiplier = Mathf.Clamp01(lifetime / 0.9f);
            Color color = initialColor;
            color.a *= alphaMultiplier;
            text.color = color;
        }

        if (lifetime <= 0f)
        {
            CombatObjectPool.ReleaseOrDestroy(gameObject);
        }
    }
}
