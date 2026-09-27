using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Upward Spray (위쪽 분사)")]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class UpwardSpray : MonoBehaviour
{
    [Tooltip("퍼져 나가는 빛 색입니다.")]
    [SerializeField] Color sprayColor = new Color(1f, 0.86f, 0.15f, 1f);
    [Tooltip("아웃라인보다 얼마나 멀리 위쪽으로 퍼질지입니다. 1보다 커야 자기보다 밖으로 나갑니다.")]
    [Min(1.1f)]
    [SerializeField] float spraySize = 3f;
    [Tooltip("한 번 쇼로록 퍼지는 시간입니다.")]
    [SerializeField] float sprayDuration = 0.4f;
    [Tooltip("공이 한 번 들어갈 때 재생하는 횟수입니다.")]
    [SerializeField] int sprayCount = 2;

    const float BandRadius = 0.34f;

    SpriteRenderer _source;
    SpriteRenderer _spray;
    Sprite _spraySprite;
    Coroutine _play;

    void Awake()
    {
        _source = GetComponent<SpriteRenderer>();
    }

    void OnDisable()
    {
        StopPlay();
        HideSpray();
    }

    void OnDestroy()
    {
        StopPlay();
        if (_spray != null)
        {
            Destroy(_spray.gameObject);
            _spray = null;
        }
    }

    public void SetSize(float size)
    {
        spraySize = Mathf.Max(1.1f, size);
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureSpray();
        StopPlay();
        _play = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        int count = Mathf.Max(1, sprayCount);
        for (int i = 0; i < count; i++)
        {
            yield return SprayOnce();
        }

        HideSpray();
        _play = null;
    }

    IEnumerator SprayOnce()
    {
        ShowSpray();
        float duration = Mathf.Max(0.08f, sprayDuration);
        float start = StartScale();
        float end = start * Mathf.Max(1.1f, spraySize);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u);
            ApplySpray(Mathf.Lerp(start, end, ease), 1f - Mathf.SmoothStep(0.2f, 1f, u));
            yield return null;
        }
    }

    float StartScale()
    {
        float sourceSize = 0.82f;
        if (_source != null && _source.sprite != null)
        {
            Vector2 size = _source.sprite.bounds.size;
            sourceSize = Mathf.Max(size.x, size.y);
        }

        return sourceSize / (BandRadius * 2f);
    }

    void OnDrawGizmosSelected()
    {
        float radius = OutlineRadius() * Mathf.Max(1.1f, spraySize);
        Gizmos.color = new Color(sprayColor.r, sprayColor.g, sprayColor.b, 0.95f);
        const int steps = 28;
        Vector3 previous = transform.TransformPoint(new Vector3(-radius, 0f, 0f));
        for (int i = 1; i <= steps; i++)
        {
            float angle = Mathf.Lerp(Mathf.PI, 0f, i / (float)steps);
            Vector3 point = transform.TransformPoint(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawLine(transform.position, transform.TransformPoint(new Vector3(0f, radius, 0f)));
    }

    float OutlineRadius()
    {
        float sourceSize = 0.82f;
        var renderer = _source != null ? _source : GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            Vector2 size = renderer.sprite.bounds.size;
            sourceSize = Mathf.Max(size.x, size.y);
        }

        return sourceSize * 0.5f;
    }

    void EnsureSpray()
    {
        if (_source == null)
        {
            _source = GetComponent<SpriteRenderer>();
        }

        if (_spraySprite == null)
        {
            _spraySprite = CreateSpraySprite();
        }

        if (_spray == null)
        {
            var child = new GameObject("UpwardSpray");
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.SetActive(false);
            _spray = child.AddComponent<SpriteRenderer>();
            _spray.sprite = _spraySprite;
            if (_source != null && _source.sharedMaterial != null)
            {
                _spray.sharedMaterial = _source.sharedMaterial;
            }
        }

        if (_source != null)
        {
            _spray.sortingLayerID = _source.sortingLayerID;
        }

        _spray.sortingOrder = 40;
    }

    void ApplySpray(float scale, float fade)
    {
        if (_spray == null)
        {
            return;
        }

        _spray.transform.localScale = new Vector3(scale, scale, 1f);
        Color color = sprayColor;
        color.a = sprayColor.a * Mathf.Clamp01(fade);
        _spray.color = color;
    }

    void ShowSpray()
    {
        ApplySpray(StartScale(), 0f);
        if (_spray != null)
        {
            _spray.gameObject.SetActive(true);
            _spray.enabled = true;
        }
    }

    void HideSpray()
    {
        if (_spray != null)
        {
            _spray.enabled = false;
            _spray.gameObject.SetActive(false);
        }
    }

    void StopPlay()
    {
        if (_play != null)
        {
            StopCoroutine(_play);
            _play = null;
        }
    }

    static Sprite CreateSpraySprite()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size - 0.5f;
                float v = (y + 0.5f) / size - 0.5f;
                float dist = Mathf.Sqrt(u * u + v * v);
                float band = Mathf.SmoothStep(0.12f, 0.28f, dist) * (1f - Mathf.SmoothStep(0.4f, 0.5f, dist));
                float upward = Mathf.SmoothStep(-0.12f, 0.02f, v);
                float alpha = Mathf.Clamp01(band * upward);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
