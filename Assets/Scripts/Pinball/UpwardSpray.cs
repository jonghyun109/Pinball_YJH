using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Upward Spray (위쪽 분사)")]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class UpwardSpray : MonoBehaviour
{
    [Tooltip("퍼져 나가는 빛 색입니다.")]
    [SerializeField] Color sprayColor = new Color(1f, 0.92f, 0.2f, 1f);
    [Tooltip("아웃라인보다 얼마나 멀리 위쪽으로 퍼질지입니다. 1보다 커야 자기보다 밖으로 나갑니다.")]
    [Min(1.1f)]
    [SerializeField] float spraySize = 3f;
    [Tooltip("한 번 쇼로록 퍼지는 시간입니다.")]
    [SerializeField] float sprayDuration = 0.45f;
    [Tooltip("공이 한 번 들어갈 때 재생하는 횟수입니다.")]
    [SerializeField] int sprayCount = 2;
    [Tooltip("원 아래쪽을 자르는 높이입니다. 0은 원 가운데입니다. 올려서 판넬 선에 맞춥니다.")]
    [SerializeField] float panelCut;
    [Tooltip("분사가 시작되는 위치입니다. Y를 내리면 더 아래에서 시작합니다.")]
    [SerializeField] Vector2 startOffset;

    SpriteRenderer _source;
    SpriteRenderer _spray;
    Material _sprayMaterial;
    Coroutine _play;
    static Material _lightMaterial;

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

        if (_sprayMaterial != null)
        {
            Destroy(_sprayMaterial);
            _sprayMaterial = null;
        }
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        EnsureSpray();
        if (_spray == null || _spray.sprite == null)
        {
            return;
        }

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
        float duration = Mathf.Max(0.08f, sprayDuration);
        float end = Mathf.Max(1.1f, spraySize);
        float t = 0f;
        ShowSpray();
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u);
            ApplySpray(Mathf.Lerp(1f, end, ease), 1f - Mathf.SmoothStep(0.25f, 1f, u));
            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        SpriteRenderer renderer = _source != null ? _source : GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Bounds bounds = renderer.sprite.bounds;
        float scale = Mathf.Max(1.1f, spraySize);
        float lift = bounds.min.y * (1f - scale);
        Vector3 center = transform.TransformPoint(new Vector3(bounds.center.x + startOffset.x, bounds.center.y + lift + startOffset.y, 0f));
        Vector3 size = new Vector3(bounds.size.x * scale, bounds.size.y * scale, 0f);
        Gizmos.color = new Color(sprayColor.r, sprayColor.g, sprayColor.b, 0.95f);
        Gizmos.DrawWireCube(center, size);
        float cutY = CutLocalY(renderer);
        Vector3 left = transform.TransformPoint(new Vector3(bounds.min.x, cutY, 0f));
        Vector3 right = transform.TransformPoint(new Vector3(bounds.max.x * scale, cutY, 0f));
        Gizmos.DrawLine(left, right);
    }

    float CutLocalY(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return panelCut;
        }

        return renderer.sprite.bounds.center.y + panelCut;
    }

    void EnsureSpray()
    {
        if (_source == null)
        {
            _source = GetComponent<SpriteRenderer>();
        }

        if (_source == null || _source.sprite == null)
        {
            return;
        }

        if (_spray == null)
        {
            var child = new GameObject("UpwardSpray");
            child.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            child.transform.localRotation = Quaternion.identity;
            child.SetActive(false);
            _spray = child.AddComponent<SpriteRenderer>();
        }

        _spray.sprite = _source.sprite;
        _spray.flipX = _source.flipX;
        _spray.flipY = _source.flipY;
        _spray.sortingLayerID = _source.sortingLayerID;
        _spray.sortingOrder = 80;
        if (_sprayMaterial == null && LightMaterial != null)
        {
            _sprayMaterial = new Material(LightMaterial);
        }

        if (_sprayMaterial != null)
        {
            _spray.sharedMaterial = _sprayMaterial;
        }

        ApplyClip();
    }

    void ApplyClip()
    {
        if (_sprayMaterial == null)
        {
            return;
        }

        SpriteRenderer renderer = _source != null ? _source : GetComponent<SpriteRenderer>();
        float worldY = transform.TransformPoint(new Vector3(0f, CutLocalY(renderer), 0f)).y;
        _sprayMaterial.SetFloat("_ClipEnabled", 1f);
        _sprayMaterial.SetFloat("_ClipY", worldY);
    }

    void ApplySpray(float scale, float fade)
    {
        if (_spray == null || _source == null || _source.sprite == null)
        {
            return;
        }

        _spray.transform.localScale = new Vector3(scale, scale, 1f);
        float lift = _source.sprite.bounds.min.y * (1f - scale);
        _spray.transform.localPosition = new Vector3(startOffset.x, lift + startOffset.y, 0f);
        Color color = sprayColor;
        color.a = sprayColor.a * Mathf.Clamp01(fade);
        _spray.color = color;
    }

    void ShowSpray()
    {
        ApplySpray(1f, 1f);
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

    static Material LightMaterial
    {
        get
        {
            if (_lightMaterial != null)
            {
                return _lightMaterial;
            }

#if UNITY_EDITOR
            _lightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/SpriteLight.mat");
#endif
            if (_lightMaterial == null)
            {
                Shader shader = Shader.Find("Pinball/Sprite Light");
                if (shader != null)
                {
                    _lightMaterial = new Material(shader)
                    {
                        name = "SpriteLight"
                    };
                }
            }

            return _lightMaterial;
        }
    }
}
