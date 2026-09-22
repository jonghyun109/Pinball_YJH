using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Sprite Glow (퍼지는 링)")]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class SpriteGlow : MonoBehaviour
{
    [Header("링")]
    [SerializeField] Color glowColor = new Color(0.72f, 0.2f, 0.95f, 1f);
    [Tooltip("링이 시작되는 크기입니다. 0은 중심입니다.")]
    [Range(0f, 1f)]
    [SerializeField] float startRadius = 0.16f;
    [Tooltip("링이 사라지는 바깥 크기입니다.")]
    [Range(0f, 1f)]
    [SerializeField] float endRadius = 0.94f;
    [SerializeField] float hitDuration = 0.38f;
    [Tooltip("게이지보다 얼마나 더 바깥까지 퍼질지입니다.")]
    [SerializeField] float spreadScale = 2.6f;

    SpriteRenderer _source;
    SpriteRenderer _ring;
    static Sprite _ringSprite;
    Coroutine _pulse;

    void Awake()
    {
        _source = GetComponent<SpriteRenderer>();
        RestoreSource();
        DestroyLeftoverRings();
    }

    void OnEnable()
    {
        if (_source == null)
        {
            _source = GetComponent<SpriteRenderer>();
        }

        RestoreSource();
    }

    void OnDisable()
    {
        StopPulse();
        HideRing();
    }

    void OnDestroy()
    {
        StopPulse();
        if (_ring != null)
        {
            Destroy(_ring.gameObject);
            _ring = null;
        }
    }

    public void Pulse()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureRing();
        StopPulse();
        _pulse = StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine()
    {
        ShowRing();
        float duration = Mathf.Max(0.05f, hitDuration);
        float from = Mathf.Min(startRadius, endRadius);
        float to = Mathf.Max(startRadius, endRadius);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u);
            ApplyRing(Mathf.Lerp(from, to, ease), 1f - u * u);
            yield return null;
        }

        HideRing();
        _pulse = null;
    }

    void EnsureRing()
    {
        if (_source == null)
        {
            _source = GetComponent<SpriteRenderer>();
        }

        if (_ringSprite == null)
        {
            _ringSprite = CreateRingSprite();
        }

        if (_ring == null)
        {
            var child = new GameObject("HitRing");
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.SetActive(false);
            _ring = child.AddComponent<SpriteRenderer>();
            _ring.sprite = _ringSprite;
            if (_source != null && !IsBroken(_source.sharedMaterial))
            {
                _ring.sharedMaterial = _source.sharedMaterial;
            }
        }

        if (_source != null)
        {
            _ring.sortingLayerID = _source.sortingLayerID;
            _ring.sortingOrder = _source.sortingOrder + 8;
        }
    }

    void ApplyRing(float radius, float fade)
    {
        if (_ring == null || _source == null)
        {
            return;
        }

        Vector2 size = _source.sprite != null ? (Vector2)_source.sprite.bounds.size : Vector2.one;
        float maxSize = Mathf.Max(size.x, size.y) * Mathf.Max(0.1f, spreadScale);
        float world = maxSize * Mathf.Clamp01(radius);
        _ring.transform.localScale = new Vector3(world, world, 1f);
        Color color = glowColor;
        color.a = glowColor.a * Mathf.Clamp01(fade);
        _ring.color = color;
    }

    void ShowRing()
    {
        ApplyRing(startRadius, 0f);
        if (_ring != null)
        {
            _ring.gameObject.SetActive(true);
            _ring.enabled = true;
        }
    }

    void HideRing()
    {
        if (_ring != null)
        {
            _ring.enabled = false;
            _ring.gameObject.SetActive(false);
        }
    }

    void StopPulse()
    {
        if (_pulse != null)
        {
            StopCoroutine(_pulse);
            _pulse = null;
        }
    }

    void RestoreSource()
    {
        if (_source == null)
        {
            return;
        }

        _source.SetPropertyBlock(null);
        if (!IsBroken(_source.sharedMaterial))
        {
            return;
        }

        var parentRenderer = _source.transform.parent != null
            ? _source.transform.parent.GetComponent<SpriteRenderer>()
            : null;
        if (parentRenderer != null && !IsBroken(parentRenderer.sharedMaterial))
        {
            _source.sharedMaterial = parentRenderer.sharedMaterial;
        }
    }

    static bool IsBroken(Material material)
    {
        if (material == null || material.shader == null)
        {
            return true;
        }

        string name = material.shader.name;
        return name == "Pinball/Sprite Glow" || name.Contains("InternalError") || name.Contains("Hidden/Internal");
    }

    void DestroyLeftoverRings()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != null && child.name == "HitRing")
            {
                Destroy(child.gameObject);
            }
        }
    }

    static Sprite CreateRingSprite()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        const float ringRadius = 0.46f;
        const float halfThickness = 0.02f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f;
                float v = (y + 0.5f) / size - 0.5f;
                float dist = Mathf.Sqrt(u * u + v * v);
                float alpha = 1f - Mathf.Clamp01(Mathf.Abs(dist - ringRadius) / halfThickness);
                alpha *= alpha;
                byte a = (byte)(alpha * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
