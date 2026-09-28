using UnityEngine;

[AddComponentMenu("Pinball/Background Rise (배경 빛)")]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class BackgroundRise : MonoBehaviour
{
    [Tooltip("공이 없을 때 아래에서 위로 지나가는 속도입니다.")]
    [SerializeField] float idleSpeed = 0.18f;
    [Tooltip("공이 없을 때 빛이 얼마나 진한지입니다. 색은 바꾸지 않고 밝기만 올립니다.")]
    [SerializeField] float idleStrength = 0.4f;
    [Tooltip("잭팟 홀에 하나가 더 들어갈 때마다 빨라지는 양입니다.")]
    [SerializeField] float speedPerGift = 0.55f;
    [Tooltip("잭팟 홀에 하나가 더 들어갈 때마다 빛이 진해지는 양입니다.")]
    [SerializeField] float strengthPerGift = 1.15f;
    [Tooltip("빛이 위아래로 퍼지는 두께입니다.")]
    [SerializeField] float bandWidth = 0.28f;
    [Tooltip("배경과 같은 빛이 같이 지나갑니다. 색은 바꾸지 않고, 속도가 바뀌어도 빛 위치는 이어집니다.")]
    [SerializeField] SpriteRenderer[] extraLights;

    SpriteRenderer _renderer;
    Material _runtime;
    Material[] _extraMaterials;
    float _phase;
    static readonly int PhaseId = Shader.PropertyToID("_Phase");
    static readonly int StrengthId = Shader.PropertyToID("_Strength");
    static readonly int WidthId = Shader.PropertyToID("_Width");

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _runtime = _renderer.material;
        if (extraLights == null)
        {
            return;
        }

        _extraMaterials = new Material[extraLights.Length];
        for (int i = 0; i < extraLights.Length; i++)
        {
            SpriteRenderer renderer = extraLights[i];
            if (renderer == null || renderer == _renderer)
            {
                continue;
            }

            _extraMaterials[i] = renderer.sharedMaterial;
            renderer.sharedMaterial = _runtime;
        }
    }

    void OnDestroy()
    {
        RestoreExtras();
        if (_runtime != null)
        {
            Destroy(_runtime);
        }
    }

    void Update()
    {
        if (_runtime == null)
        {
            return;
        }

        int gifts = 0;
        if (PinballGame.Instance != null)
        {
            gifts = Mathf.Max(0, PinballGame.Instance.GiftCount);
        }

        float speed = Mathf.Max(0.02f, idleSpeed + gifts * speedPerGift);
        _phase = Mathf.Repeat(_phase + speed * Time.deltaTime, 1f);
        _runtime.SetFloat(PhaseId, _phase);
        _runtime.SetFloat(StrengthId, Mathf.Max(0f, idleStrength + gifts * strengthPerGift));
        _runtime.SetFloat(WidthId, Mathf.Max(0.04f, bandWidth));
    }

    void RestoreExtras()
    {
        if (extraLights == null || _extraMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(extraLights.Length, _extraMaterials.Length);
        for (int i = 0; i < count; i++)
        {
            if (extraLights[i] == null || _extraMaterials[i] == null)
            {
                continue;
            }

            extraLights[i].sharedMaterial = _extraMaterials[i];
        }
    }
}
