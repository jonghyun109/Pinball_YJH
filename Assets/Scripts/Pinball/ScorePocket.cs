using UnityEngine;

[AddComponentMenu("Pinball/Score Pocket (점수 칸)")]
[DisallowMultipleComponent]
public sealed class ScorePocket : MonoBehaviour
{
    [SerializeField] int points = 100;
    [Tooltip("비우면 점수 숫자를 그대로 표시합니다.")]
    [SerializeField] string label;

    public int Points => points;
    public string Label => string.IsNullOrEmpty(label) ? points.ToString() : label;
    public Vector3 LabelWorldPosition => transform.position;

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
        Register();
    }

    void Start()
    {
        Register();
    }

    void Register()
    {
        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.RegisterPocket(this);
        }
    }

    void OnDisable()
    {
        if (PinballGame.Instance != null)
        {
            PinballGame.Instance.UnregisterPocket(this);
        }
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
            PinballGame.Instance.HandlePocketScore(this, ball);
        }
    }
}
