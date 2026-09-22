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

    bool _tipped;
    bool _boosted;
    float _timer;
    int _count;

    public int Count => _count;

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
}
