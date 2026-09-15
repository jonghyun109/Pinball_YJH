using UnityEngine;

[AddComponentMenu("Pinball/Ball (공)")]
[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public sealed class PinballBall : MonoBehaviour
{
    Rigidbody2D _body;
    Collider2D _collider;
    float _launchedAt = -10f;
    float _baseGravityScale = 1.15f;

    public Rigidbody2D Body => _body;
    public bool InPlay { get; private set; }
    public bool Guided { get; private set; }

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
        _collider = GetComponent<Collider2D>();
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        _baseGravityScale = _body.gravityScale;
    }

    public void Launch(Vector2 position, Vector2 velocity, float spin = 0f)
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        StopGuide();
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
        return InPlay && !Guided && Time.time - _launchedAt > 0.35f;
    }

    public void BeginGuide()
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        Guided = true;
        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        if (_collider != null)
        {
            _collider.enabled = false;
        }

        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
    }

    public void FollowGuide(Vector2 position, float angularVelocity)
    {
        if (_body == null)
        {
            return;
        }

        _body.MovePosition(position);
        _body.angularVelocity = angularVelocity;
    }

    public void EndGuide(Vector2 velocity)
    {
        if (!Guided)
        {
            return;
        }

        StopGuide();
        _body.linearVelocity = velocity;
        _body.WakeUp();
    }

    public void SleepInPool()
    {
        InPlay = false;
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        StopGuide();
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.simulated = false;
        gameObject.SetActive(false);
    }

    void StopGuide()
    {
        Guided = false;
        if (_body == null)
        {
            return;
        }

        _body.bodyType = RigidbodyType2D.Dynamic;
        _body.gravityScale = _baseGravityScale;
        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }
}
