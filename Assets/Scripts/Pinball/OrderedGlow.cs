using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Ordered Glow (순서 밝기)")]
[DisallowMultipleComponent]
public sealed class OrderedGlow : MonoBehaviour
{
    [Tooltip("위에서부터 순서대로 밝아집니다. 색은 바꾸지 않고 원래 색을 더 밝게 비춥니다.")]
    [SerializeField] SpriteRenderer[] lights;
    [Tooltip("한 개가 밝게 있는 시간입니다.")]
    [SerializeField] float hold = 0.18f;
    [Tooltip("원래 색을 얼마나 더 밝게 비출지입니다.")]
    [SerializeField] float strength = 2.2f;

    Rest[] _rests;
    Coroutine _play;
    Material _glow;
    bool _ready;

    void Awake()
    {
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
            Cache();
        }

        if (!_ready)
        {
            return;
        }

        if (_play != null)
        {
            return;
        }

        _play = StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        float wait = Mathf.Max(0.05f, hold);
        while (HasGift())
        {
            for (int i = 0; i < _rests.Length; i++)
            {
                if (!HasGift())
                {
                    break;
                }

                RestoreAll();
                Paint(_rests[i]);
                yield return new WaitForSeconds(wait);
            }
        }

        RestoreAll();
        _play = null;
    }

    static bool HasGift()
    {
        return PinballGame.Instance != null && PinballGame.Instance.GiftCount > 0;
    }

    void Cache()
    {
        int count = 0;
        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    count++;
                }
            }
        }

        if (count == 0)
        {
            _ready = false;
            return;
        }

        _rests = new Rest[count];
        int index = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            SpriteRenderer renderer = lights[i];
            if (renderer == null)
            {
                continue;
            }

            _rests[index++] = new Rest
            {
                Renderer = renderer,
                Material = renderer.sharedMaterial,
                Order = renderer.sortingOrder
            };
        }

        _ready = true;
    }

    void Paint(Rest rest)
    {
        if (rest.Renderer == null)
        {
            return;
        }

        Material material = GlowMaterial();
        if (material != null)
        {
            material.SetFloat("_Strength", Mathf.Max(0f, strength));
            material.SetFloat("_Width", 8f);
            material.SetFloat("_Phase", 0.5f);
            rest.Renderer.sharedMaterial = material;
        }

        rest.Renderer.sortingOrder = rest.Order + 2;
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

            rest.Renderer.sortingOrder = rest.Order;
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
            name = "OrderedGlow"
        };
        return _glow;
    }

    struct Rest
    {
        public SpriteRenderer Renderer;
        public Material Material;
        public int Order;
    }
}
