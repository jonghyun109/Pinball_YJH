using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[AddComponentMenu("Pinball/Gift Hole (잭팟 홀)")]
[DisallowMultipleComponent]
public sealed class GiftHole : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공이 들어갈 때마다 1, 2, 3으로 올라가는 텍스트입니다.")]
    [SerializeField] TMP_Text giftText;
    [Tooltip("비우면 이 오브젝트가 돌아갑니다.")]
    [SerializeField] Transform rotateTarget;

    [Header("회전")]
    [Tooltip("게임이 시작된 뒤 한 번 도는 간격입니다.")]
    [SerializeField] float idleInterval = 0.5f;
    [Tooltip("공이 들어간 뒤 도는 간격입니다. 작을수록 빨라집니다.")]
    [SerializeField] float hitInterval = 0.12f;
    [SerializeField] float stepAngle = 45f;

    [Header("가운데 별 라이트")]
    [Tooltip("비우면 같은 세트의 StarLight_InRow001_1, _3 을 찾습니다.")]
    [SerializeField] SpriteRenderer[] starLights13;
    [Tooltip("비우면 같은 세트의 StarLight_InRow001_0, _4 를 찾습니다.")]
    [SerializeField] SpriteRenderer[] starLights04;
    [Tooltip("어두운 별 이미지를 이 색으로 밝힙니다.")]
    [SerializeField] Color starLightColor = new Color(1f, 0.86f, 0.28f, 1f);
    [Tooltip("한 쌍이 켜져 있는 시간입니다. 1·3이 이 시간 동안 켜진 뒤 0·4가 같은 시간 켜집니다.")]
    [SerializeField] float starLightHold = 0.22f;
    [Tooltip("1·3 다음 0·4를 한 세트로, 이 횟수만큼 반복합니다.")]
    [SerializeField] int starLightLoops = 4;
    [Tooltip("별이 켜질 때 위로 퍼지는 거리입니다. 1보다 커야 아웃라인 밖으로 나갑니다.")]
    [Min(1.1f)]
    [SerializeField] float spraySize = 3f;

    bool _tipped;
    bool _boosted;
    float _timer;
    int _count;
    StarRest[] _starRests;
    bool _starRestReady;
    Coroutine _starGlow;
    static Material _starLightMaterial;

    public int Count => _count;

    void Awake()
    {
        BindStarLights();
        CacheStarLights();
    }

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
    }

    void OnEnable()
    {
        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.RegisterGiftHole(this);
        }
    }

    void Start()
    {
        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.RegisterGiftHole(this);
        }

        ResetRound();
    }

    void OnDisable()
    {
        if (_starGlow != null)
        {
            StopCoroutine(_starGlow);
            _starGlow = null;
        }

        if (Application.isPlaying && _starRestReady)
        {
            RestoreStarLights();
        }

        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.UnregisterGiftHole(this);
        }
    }

    void Update()
    {
        float interval = _boosted ? hitInterval : idleInterval;
        if (interval <= 0f)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < interval)
        {
            return;
        }

        _timer = 0f;
        StepRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<PinballBall>();
        if (ball == null || !ball.CanRecycle())
        {
            return;
        }

        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.HandleGift(this, ball);
        }
    }

    public void Collect()
    {
        _count++;
        _boosted = true;
        _timer = 0f;
        StepRotation();
        RefreshText();
        PlayStarLights();
    }

    public void ResetRound()
    {
        _count = 0;
        _boosted = false;
        _tipped = false;
        _timer = 0f;
        ApplyRotation();
        RefreshText();
    }

    void StepRotation()
    {
        _tipped = !_tipped;
        ApplyRotation();
    }

    void ApplyRotation()
    {
        var target = rotateTarget != null ? rotateTarget : transform;
        Vector3 angles = target.localEulerAngles;
        angles.z = _tipped ? stepAngle : 0f;
        target.localEulerAngles = angles;
    }

    void RefreshText()
    {
        if (giftText != null)
        {
            giftText.text = _count > 0 ? _count.ToString() : string.Empty;
        }
    }

    void BindStarLights()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (starLights13 == null || starLights13.Length == 0)
        {
            starLights13 = FindStarLights(renderers, "StarLight_InRow001_1", "StarLight_InRow001_3");
        }

        if (starLights04 == null || starLights04.Length == 0)
        {
            starLights04 = FindStarLights(renderers, "StarLight_InRow001_0", "StarLight_InRow001_4");
        }
    }

    static SpriteRenderer[] FindStarLights(SpriteRenderer[] renderers, params string[] names)
    {
        var found = new List<SpriteRenderer>(names.Length);
        for (int n = 0; n < names.Length; n++)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer != null && IsStarName(renderer.name, names[n]))
                {
                    found.Add(renderer);
                    break;
                }
            }
        }

        return found.ToArray();
    }

    static bool IsStarName(string objectName, string spriteName)
    {
        if (objectName == spriteName)
        {
            return true;
        }

        return objectName.StartsWith(spriteName + " ", System.StringComparison.Ordinal);
    }

    void CacheStarLights()
    {
        var unique = new List<SpriteRenderer>(4);
        AddUnique(unique, starLights13);
        AddUnique(unique, starLights04);
        _starRests = new StarRest[unique.Count];
        for (int i = 0; i < unique.Count; i++)
        {
            SpriteRenderer renderer = unique[i];
            _starRests[i] = new StarRest
            {
                Renderer = renderer,
                Color = renderer.color,
                Material = renderer.sharedMaterial
            };
        }

        _starRestReady = _starRests.Length > 0;
    }

    static void AddUnique(List<SpriteRenderer> list, SpriteRenderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null && !list.Contains(renderer))
            {
                list.Add(renderer);
            }
        }
    }

    void PlayOutlineSpray()
    {
        UpwardSpray spray = FindOutlineSpray();
        if (spray == null)
        {
            return;
        }

        spray.SetSize(spraySize);
        spray.Play();
    }

    UpwardSpray FindOutlineSpray()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        UpwardSpray spray = root.GetComponentInChildren<UpwardSpray>(true);
        if (spray != null)
        {
            return spray;
        }

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer != null && renderer.name.StartsWith("JackPotHoll_Outline_Fill", System.StringComparison.Ordinal))
            {
                return renderer.gameObject.AddComponent<UpwardSpray>();
            }
        }

        return gameObject.AddComponent<UpwardSpray>();
    }

    void PlayStarLights()
    {
        PlayOutlineSpray();
        if (!_starRestReady)
        {
            return;
        }

        if (_starGlow != null)
        {
            StopCoroutine(_starGlow);
            RestoreStarLights();
        }

        _starGlow = StartCoroutine(StarLightRoutine());
    }

    IEnumerator StarLightRoutine()
    {
        float hold = Mathf.Max(0.05f, starLightHold);
        int loops = Mathf.Max(1, starLightLoops);
        for (int i = 0; i < loops; i++)
        {
            PaintStarLights(starLights13);
            yield return new WaitForSeconds(hold);
            RestoreStarLights(starLights13);
            PaintStarLights(starLights04);
            yield return new WaitForSeconds(hold);
            RestoreStarLights(starLights04);
        }

        RestoreStarLights();
        _starGlow = null;
    }

    void PaintStarLights(SpriteRenderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        Material material = StarLightMaterial;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.color = starLightColor;
        }
    }

    void RestoreStarLights()
    {
        if (_starRests == null)
        {
            return;
        }

        for (int i = 0; i < _starRests.Length; i++)
        {
            RestoreOne(_starRests[i]);
        }
    }

    void RestoreStarLights(SpriteRenderer[] renderers)
    {
        if (renderers == null || _starRests == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            for (int r = 0; r < _starRests.Length; r++)
            {
                if (_starRests[r].Renderer == renderer)
                {
                    RestoreOne(_starRests[r]);
                    break;
                }
            }
        }
    }

    static void RestoreOne(StarRest rest)
    {
        if (rest.Renderer == null)
        {
            return;
        }

        if (rest.Material != null)
        {
            rest.Renderer.sharedMaterial = rest.Material;
        }

        rest.Renderer.color = rest.Color;
    }

    static Material StarLightMaterial
    {
        get
        {
            if (_starLightMaterial != null)
            {
                return _starLightMaterial;
            }

#if UNITY_EDITOR
            _starLightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/SpriteLight.mat");
#endif
            if (_starLightMaterial == null)
            {
                Shader shader = Shader.Find("Pinball/Sprite Light");
                if (shader != null)
                {
                    _starLightMaterial = new Material(shader)
                    {
                        name = "SpriteLight"
                    };
                }
            }

            return _starLightMaterial;
        }
    }

    struct StarRest
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public Material Material;
    }
}
