using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Medium Star Sequence (중간 별 순서 빛)")]
[DisallowMultipleComponent]
public sealed class MediumStarSequence : MonoBehaviour
{
    [Tooltip("비우면 자식 이름에 Fill 이 들어간 스프라이트를 찾습니다.")]
    [SerializeField] SpriteRenderer fill;
    [Tooltip("비우면 자식 이름에 Outline 이 들어간 스프라이트를 찾습니다.")]
    [SerializeField] SpriteRenderer outline;
    [Tooltip("비우면 이 오브젝트의 스프라이트를 배경으로 씁니다.")]
    [SerializeField] SpriteRenderer background;
    [Tooltip("Fill만 이 색으로 밝힙니다. Outline과 배경은 색을 바꾸지 않습니다.")]
    [SerializeField] Color lightColor = new Color(1f, 0.86f, 0.28f, 1f);
    [Tooltip("Outline과 배경이 원래 색으로 얼마나 밝아지는지입니다.")]
    [SerializeField] float glowStrength = 2.2f;
    [Tooltip("한 겹이 켜져 있는 시간입니다. Fill, Outline, BG 순서로 켜집니다.")]
    [SerializeField] float hold = 0.18f;

    Rest[] _rests;
    bool _ready;
    Coroutine _play;
    int _frontOrder;
    Material _glow;
    static Material _lightMaterial;

    void Awake()
    {
        Bind();
        Cache();
    }

    void OnDisable()
    {
        if (!Application.isPlaying || !_ready)
        {
            return;
        }

        if (_play != null)
        {
            StopCoroutine(_play);
            _play = null;
        }

        RestoreAll();
    }

    void OnDestroy()
    {
        if (_glow != null)
        {
            Destroy(_glow);
        }
    }

    public void Play()
    {
        if (!_ready)
        {
            Bind();
            Cache();
        }

        if (!_ready)
        {
            return;
        }

        if (_play != null)
        {
            StopCoroutine(_play);
            RestoreAll();
        }

        _play = StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        float wait = Mathf.Max(0.05f, hold);
        SpriteRenderer[] order = { fill, outline, background };
        for (int i = 0; i < order.Length; i++)
        {
            RestoreAll();
            Paint(order[i]);
            yield return new WaitForSeconds(wait);
        }

        RestoreAll();
        _play = null;
    }

    void Bind()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string name = renderer.name;
            if (fill == null && name.Contains("Fill"))
            {
                fill = renderer;
            }
            else if (outline == null && name.Contains("Outline"))
            {
                outline = renderer;
            }
            else if (background == null && name.Contains("_BG"))
            {
                background = renderer;
            }
        }

        if (background == null)
        {
            background = GetComponent<SpriteRenderer>();
        }
    }

    void Cache()
    {
        SpriteRenderer[] order = { fill, outline, background };
        int count = 0;
        int front = 0;
        for (int i = 0; i < order.Length; i++)
        {
            if (order[i] == null)
            {
                continue;
            }

            count++;
            front = Mathf.Max(front, order[i].sortingOrder);
        }

        if (count == 0)
        {
            _ready = false;
            return;
        }

        _frontOrder = front + 2;
        _rests = new Rest[count];
        int index = 0;
        for (int i = 0; i < order.Length; i++)
        {
            SpriteRenderer renderer = order[i];
            if (renderer == null)
            {
                continue;
            }

            _rests[index++] = new Rest
            {
                Renderer = renderer,
                Color = renderer.color,
                Material = renderer.sharedMaterial,
                Order = renderer.sortingOrder
            };
        }

        _ready = true;
    }

    void Paint(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer == fill ? LightMaterial : GlowMaterial();
        if (material != null)
        {
            if (renderer != fill)
            {
                material.SetFloat("_Strength", Mathf.Max(0f, glowStrength));
                material.SetFloat("_Width", 8f);
                material.SetFloat("_Phase", 0.5f);
            }

            renderer.sharedMaterial = material;
        }

        if (renderer == fill)
        {
            renderer.color = lightColor;
        }

        renderer.sortingOrder = _frontOrder;
    }

    void RestoreAll()
    {
        if (_rests == null)
        {
            return;
        }

        for (int i = 0; i < _rests.Length; i++)
        {
            Rest rest = _rests[i];
            if (rest.Renderer == null)
            {
                continue;
            }

            if (rest.Material != null)
            {
                rest.Renderer.sharedMaterial = rest.Material;
            }

            rest.Renderer.color = rest.Color;
            rest.Renderer.sortingOrder = rest.Order;
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

    Material GlowMaterial()
    {
        if (_glow != null)
        {
            return _glow;
        }

        Shader shader = Shader.Find("Pinball/Background Rise");
#if UNITY_EDITOR
        if (shader == null)
        {
            Material source = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/BackgroundRise.mat");
            if (source != null)
            {
                shader = source.shader;
            }
        }
#endif
        if (shader == null)
        {
            return null;
        }

        _glow = new Material(shader)
        {
            name = "MediumStarGlow"
        };
        return _glow;
    }

    struct Rest
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public Material Material;
        public int Order;
    }
}
