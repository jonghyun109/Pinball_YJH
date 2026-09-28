using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Pair Glow (짝 밝기)")]
[DisallowMultipleComponent]
public sealed class PairGlow : MonoBehaviour
{
    [Tooltip("먼저 같이 밝아지는 쪽입니다. 색은 바꾸지 않고 원래 색만 밝힙니다.")]
    [SerializeField] SpriteRenderer[] firstPair;
    [Tooltip("이어서 같이 밝아지는 쪽입니다.")]
    [SerializeField] SpriteRenderer[] secondPair;
    [Tooltip("한 짝이 밝게 있는 시간입니다.")]
    [SerializeField] float hold = 0.18f;
    [Tooltip("공이 들어갈 때마다 0·1 다음 2·3 을 반복하는 횟수입니다.")]
    [SerializeField] int loops = 2;
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
            StopCoroutine(_play);
            RestoreAll();
        }

        _play = StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        float wait = Mathf.Max(0.05f, hold);
        int count = Mathf.Max(1, loops);
        for (int i = 0; i < count; i++)
        {
            RestoreAll();
            Paint(firstPair);
            yield return new WaitForSeconds(wait);
            RestoreAll();
            Paint(secondPair);
            yield return new WaitForSeconds(wait);
        }

        RestoreAll();
        _play = null;
    }

    void Cache()
    {
        int count = Count(firstPair) + Count(secondPair);
        if (count == 0)
        {
            _ready = false;
            return;
        }

        _rests = new Rest[count];
        int index = 0;
        index = Store(firstPair, index);
        Store(secondPair, index);
        _ready = true;
    }

    static int Count(SpriteRenderer[] renderers)
    {
        if (renderers == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    int Store(SpriteRenderer[] renderers, int index)
    {
        if (renderers == null)
        {
            return index;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
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

        return index;
    }

    void Paint(SpriteRenderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        Material material = GlowMaterial();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (material != null)
            {
                material.SetFloat("_Strength", Mathf.Max(0f, strength));
                material.SetFloat("_Width", 8f);
                material.SetFloat("_Phase", 0.5f);
                renderer.sharedMaterial = material;
            }

            renderer.sortingOrder = OrderOf(renderer) + 2;
        }
    }

    int OrderOf(SpriteRenderer renderer)
    {
        for (int i = 0; i < _rests.Length; i++)
        {
            if (_rests[i].Renderer == renderer)
            {
                return _rests[i].Order;
            }
        }

        return renderer.sortingOrder;
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
            name = "PairGlow"
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
