using UnityEngine;

[AddComponentMenu("Pinball/Slot Machine Blink (슬롯 라이트 교차)")]
[DisallowMultipleComponent]
public sealed class SlotMachineBlink : MonoBehaviour
{
    [Tooltip("먼저 켜지는 슬롯 라이트입니다. 비우면 SlotMachineLight 를 찾습니다.")]
    [SerializeField] SpriteRenderer first;
    [Tooltip("이어서 켜지는 슬롯 라이트입니다. 비우면 SlotMachineLight1 을 찾습니다.")]
    [SerializeField] SpriteRenderer second;
    [Tooltip("공이 없을 때 한 쪽이 켜져 있는 시간입니다.")]
    [SerializeField] float interval = 0.42f;
    [Tooltip("잭팟 홀에 공이 하나 들어갈 때마다 줄어드는 시간입니다. 켜진 쪽은 바꾸지 않고 속도만 올립니다.")]
    [SerializeField] float fasterPerGift = 0.08f;
    [Tooltip("더 이상 줄어들지 않는 최소 시간입니다.")]
    [SerializeField] float minInterval = 0.06f;
    [Tooltip("꺼진 쪽의 투명도입니다. 0이면 사라지고, 1이면 켜진 것과 같습니다.")]
    [Range(0.05f, 1f)]
    [SerializeField] float dimAlpha = 0.25f;

    Color _firstColor = Color.white;
    Color _secondColor = Color.white;
    float _timer;
    bool _firstOn = true;
    bool _ready;

    void Awake()
    {
        Resolve();
        if (first != null)
        {
            _firstColor = first.color;
        }

        if (second != null)
        {
            _secondColor = second.color;
        }

        _ready = first != null || second != null;
        Apply();
    }

    void OnDisable()
    {
        if (!Application.isPlaying || !_ready)
        {
            return;
        }

        Set(first, _firstColor, true);
        Set(second, _secondColor, true);
    }

    void Update()
    {
        if (!_ready)
        {
            return;
        }

        float wait = Interval();
        _timer += Time.deltaTime;
        if (_timer < wait)
        {
            return;
        }

        _timer -= wait;
        if (_timer > wait)
        {
            _timer = 0f;
        }

        _firstOn = !_firstOn;
        Apply();
    }

    float Interval()
    {
        int gifts = 0;
        if (PinballGame.Instance != null)
        {
            gifts = Mathf.Max(0, PinballGame.Instance.GiftCount);
        }

        return Mathf.Max(minInterval, interval - gifts * fasterPerGift);
    }

    void Resolve()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.name == "SlotMachineLight")
            {
                first = renderer;
            }
            else if (renderer.name == "SlotMachineLight1")
            {
                second = renderer;
            }
        }
    }

    void Apply()
    {
        Set(first, _firstColor, _firstOn);
        Set(second, _secondColor, !_firstOn);
    }

    void Set(SpriteRenderer renderer, Color color, bool on)
    {
        if (renderer == null)
        {
            return;
        }

        color.a = on ? color.a : color.a * dimAlpha;
        renderer.color = color;
    }
}
