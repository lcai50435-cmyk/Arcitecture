using UnityEngine;

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

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        ownerRenderer = GetComponent<SpriteRenderer>();
        EnsureVisuals();
        RefreshHealthBar();
        SetHealthBarVisible(false);
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
        UpdateHealthBarPosition();

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
        barVisibleTimer = 1.2f;
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
        healthBarFill.sortingOrder = 31;
        healthBarFill.color = new Color(0.88f, 0.23f, 0.18f, 1f);
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

    private void SpawnDamageNumber(float damage)
    {
        GameObject numberObject = new GameObject("DamageNumber");
        numberObject.transform.SetParent(transform, false);
        numberObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);

        TextMesh textMesh = numberObject.AddComponent<TextMesh>();
        textMesh.text = Mathf.Max(0f, damage).ToString("0");
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.08f;
        textMesh.color = new Color(0.98f, 0.86f, 0.28f, 1f);

        MeshRenderer meshRenderer = numberObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 32;
        }

        EnemyDamageNumberMotion motion = numberObject.AddComponent<EnemyDamageNumberMotion>();
        motion.Initialize();
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

    public void Initialize()
    {
        velocity = new Vector3(Random.Range(-0.12f, 0.12f), 0.55f, 0f);
        lifetime = 0.65f;
    }

    private void Update()
    {
        transform.localPosition += velocity * Time.deltaTime;
        lifetime -= Time.deltaTime;

        TextMesh textMesh = GetComponent<TextMesh>();
        if (textMesh != null)
        {
            Color color = textMesh.color;
            color.a = Mathf.Clamp01(lifetime / 0.65f);
            textMesh.color = color;
        }

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
