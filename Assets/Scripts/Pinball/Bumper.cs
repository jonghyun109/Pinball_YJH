using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Bumper (튕기는 장애물)")]
[DisallowMultipleComponent]
public sealed class Bumper : MonoBehaviour
{
    [Tooltip("공에 추가로 가하는 힘입니다.")]
    [SerializeField] float kickForce = 1.2f;
    [SerializeField] float minImpactSpeed = 0.8f;
    [SerializeField] bool flashOnHit = true;

    [Header("맞을 때 크기")]
    [Tooltip("바깥(또는 작은 범퍼 전체)이 커지는 배율입니다.")]
    [SerializeField] float punchScale = 1.12f;
    [Tooltip("큰 범퍼 안쪽 이미지가 커지는 배율입니다. 바깥보다 크게 두면 됩니다.")]
    [SerializeField] float innerPunchScale = 1.3f;
    [SerializeField] float punchDuration = 0.14f;
    [Tooltip("큰 범퍼에서 더 크게 팝할 안쪽 이미지들. 여기에 넣은 것만 커집니다.")]
    [SerializeField] Transform[] innerVisuals;

    [Header("게이지")]
    [Tooltip("큰 범퍼 Fill 이미지에 Sprite Radial Fill을 붙인 뒤 여기에 넣습니다.")]
    [SerializeField] SpriteRadialFill hitFill;
    [Tooltip("이 횟수만큼 맞으면 Fill이 가득 찹니다.")]
    [SerializeField] int hitsToFill = 8;
    [SerializeField] bool resetWhenFull = true;

    [Header("글로우")]
    [Tooltip("맞을 때 형광으로 깜빡일 배경입니다. SlotGuage_BG에 Sprite Glow를 붙인 뒤 넣습니다.")]
    [SerializeField] SpriteGlow[] hitGlows;

    [Header("가득 찼을 때 라이트")]
    [Tooltip("비우면 같은 게이지 안에서 Light001 이름을 찾습니다.")]
    [SerializeField] SpriteRenderer light001;
    [Tooltip("비우면 같은 게이지 안에서 Light002 이름을 찾습니다.")]
    [SerializeField] SpriteRenderer light002;
    [Tooltip("어두운 전구 이미지를 이 색으로 밝힙니다.")]
    [SerializeField] Color fullLightRed = new Color(1f, 0.08f, 0.2f, 1f);
    [Tooltip("어두운 전구 이미지를 이 색으로 밝힙니다.")]
    [SerializeField] Color fullLightBlue = new Color(0.12f, 0.45f, 1f, 1f);
    [Tooltip("한 색을 유지하는 시간입니다.")]
    [SerializeField] float fullLightHold = 0.14f;
    [Tooltip("001이 바뀐 뒤 002가 따라가는 간격입니다.")]
    [SerializeField] float fullLightStagger = 0.07f;
    [SerializeField] int fullLightLoops = 2;

    SpriteRenderer _renderer;
    Color _baseColor;
    Color _light001Rest;
    Color _light002Rest;
    Material _light001Material;
    Material _light002Material;
    int _light001Order;
    int _light002Order;
    int _lightFrontOrder;
    bool _lightRestCached;
    static Material _lightMaterial;
    Vector3 _outerRest;
    Vector3[] _innerRests;
    int _fillHits;
    bool _fullLightPlaying;
    Coroutine _flash;
    Coroutine _punch;
    Coroutine _fullLights;

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }

        collider.isTrigger = false;
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
        {
            _baseColor = _renderer.color;
        }

        _outerRest = transform.localScale;
        _innerRests = innerVisuals != null ? new Vector3[innerVisuals.Length] : System.Array.Empty<Vector3>();
        for (int i = 0; i < _innerRests.Length; i++)
        {
            _innerRests[i] = innerVisuals[i] != null ? innerVisuals[i].localScale : Vector3.one;
        }

        if (hitFill != null)
        {
            hitFill.FillAmount = 0f;
        }

        AutoFindLights();
        CacheLightColors();
    }

    void OnDisable()
    {
        if (_fullLights != null)
        {
            StopCoroutine(_fullLights);
            _fullLights = null;
        }

        _fullLightPlaying = false;
        if (Application.isPlaying && _lightRestCached)
        {
            RestoreLights();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody == null || collision.collider.GetComponent<PinballBall>() == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
        {
            return;
        }

        Vector2 away = ((Vector2)collision.rigidbody.position - (Vector2)transform.position).normalized;
        if (away.sqrMagnitude < 0.001f && collision.contactCount > 0)
        {
            away = collision.GetContact(0).normal;
        }

        Vector2 tangent = new Vector2(-away.y, away.x);
        Vector2 kick = away * (kickForce * Random.Range(0.9f, 1.1f));
        kick += tangent * Random.Range(-0.12f, 0.12f) * kickForce;
        collision.rigidbody.AddForce(kick, ForceMode2D.Impulse);

        Punch();
        if (!_fullLightPlaying)
        {
            AddFill();
        }

        PulseGlows();
        if (flashOnHit)
        {
            Flash();
        }
    }

    void AddFill()
    {
        if (hitFill == null || hitsToFill <= 0)
        {
            return;
        }

        _fillHits++;
        hitFill.FillAmount = Mathf.Clamp01(_fillHits / (float)hitsToFill);
        if (resetWhenFull && _fillHits >= hitsToFill)
        {
            _fillHits = 0;
            PlayFullLights();
        }
    }

    void AutoFindLights()
    {
        var root = hitFill != null ? hitFill.transform.parent : transform.parent;
        if (root == null)
        {
            root = transform;
        }

        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (light001 == null && renderer.name.IndexOf("Light001", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                light001 = renderer;
            }
            else if (light002 == null && renderer.name.IndexOf("Light002", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                light002 = renderer;
            }
        }
    }

    void CacheLightColors()
    {
        if (light001 != null)
        {
            _light001Rest = light001.color;
            _light001Material = light001.sharedMaterial;
            _light001Order = light001.sortingOrder;
        }

        if (light002 != null)
        {
            _light002Rest = light002.color;
            _light002Material = light002.sharedMaterial;
            _light002Order = light002.sortingOrder;
        }

        _lightFrontOrder = Mathf.Max(_light001Order, _light002Order) + 2;
        _lightRestCached = light001 != null || light002 != null;
    }

    void PlayFullLights()
    {
        if (light001 == null && light002 == null)
        {
            if (hitFill != null)
            {
                hitFill.FillAmount = 0f;
            }

            return;
        }

        if (_fullLights != null)
        {
            StopCoroutine(_fullLights);
        }

        _fullLights = StartCoroutine(FullLightRoutine());
    }

    IEnumerator FullLightRoutine()
    {
        _fullLightPlaying = true;
        int loops = Mathf.Max(1, fullLightLoops);
        float hold = Mathf.Max(0.04f, fullLightHold);
        float stagger = Mathf.Max(0f, fullLightStagger);

        for (int i = 0; i < loops; i++)
        {
            PaintLight(light001, fullLightRed);
            if (stagger > 0f)
            {
                yield return new WaitForSeconds(stagger);
            }

            PaintLight(light002, fullLightRed);
            yield return new WaitForSeconds(hold);

            PaintLight(light001, fullLightBlue);
            if (stagger > 0f)
            {
                yield return new WaitForSeconds(stagger);
            }

            PaintLight(light002, fullLightBlue);
            yield return new WaitForSeconds(hold);
        }

        RestoreLights();
        if (hitFill != null)
        {
            hitFill.FillAmount = 0f;
        }

        _fullLightPlaying = false;
        _fullLights = null;
    }

    void PaintLight(SpriteRenderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = LightMaterial;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        renderer.color = color;
        if (light001 != null)
        {
            light001.sortingOrder = light001 == renderer ? _lightFrontOrder : _light001Order;
        }

        if (light002 != null)
        {
            light002.sortingOrder = light002 == renderer ? _lightFrontOrder : _light002Order;
        }
    }

    void RestoreLights()
    {
        RestoreLight(light001, _light001Rest, _light001Material, _light001Order);
        RestoreLight(light002, _light002Rest, _light002Material, _light002Order);
    }

    static void RestoreLight(SpriteRenderer renderer, Color color, Material material, int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
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

    void Punch()
    {
        if (_punch != null)
        {
            StopCoroutine(_punch);
        }

        _punch = StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        float duration = Mathf.Max(0.04f, punchDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pop = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
            transform.localScale = _outerRest * Mathf.Lerp(1f, punchScale, pop);
            ApplyInnerScale(Mathf.Lerp(1f, innerPunchScale, pop));
            yield return null;
        }

        transform.localScale = _outerRest;
        ApplyInnerScale(1f);

        _punch = null;
    }

    void ApplyInnerScale(float multiplier)
    {
        if (innerVisuals == null)
        {
            return;
        }

        int count = Mathf.Min(innerVisuals.Length, _innerRests.Length);
        for (int i = 0; i < count; i++)
        {
            if (innerVisuals[i] != null)
            {
                innerVisuals[i].localScale = _innerRests[i] * multiplier;
            }
        }
    }

    void PulseGlows()
    {
        if (hitGlows == null)
        {
            return;
        }

        for (int i = 0; i < hitGlows.Length; i++)
        {
            if (hitGlows[i] != null)
            {
                hitGlows[i].Pulse();
            }
        }
    }

    void Flash()
    {
        if (_renderer == null)
        {
            return;
        }

        if (_flash != null)
        {
            StopCoroutine(_flash);
        }

        _flash = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        _renderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        _renderer.color = _baseColor;
        _flash = null;
    }
}
