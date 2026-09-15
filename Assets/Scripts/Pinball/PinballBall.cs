using UnityEngine;

[AddComponentMenu("Pinball/Ball (공)")]
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class PinballBall : MonoBehaviour
{
    Rigidbody2D _body;
    float _launchedAt = -10f;

    public Rigidbody2D Body => _body;
    public bool InPlay { get; private set; }

    void Reset()
    {
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.simulated = false;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.gravityScale = 1.15f;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.08f;
        body.mass = 1f;

        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
        }
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Launch(Vector2 position, Vector2 velocity, float spin = 0f)
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        InPlay = true;
        _launchedAt = Time.time;
        transform.position = position;
        _body.simulated = true;
        _body.bodyType = RigidbodyType2D.Dynamic;
        _body.position = position;
        _body.rotation = 0f;
        _body.linearVelocity = velocity;
        _body.angularVelocity = spin;
        _body.WakeUp();
    }

    public bool CanRecycle()
    {
        return InPlay && Time.time - _launchedAt > 0.35f;
    }

    public void SleepInPool()
    {
        InPlay = false;
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.simulated = false;
        gameObject.SetActive(false);
    }
}
