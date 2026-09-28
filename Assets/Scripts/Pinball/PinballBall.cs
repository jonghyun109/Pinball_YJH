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
    TrailRenderer _trail;

    [Header("잔상")]
    [Tooltip("공 뒤에 남는 회색 선의 길이(초)입니다.")]
    [SerializeField] float trailTime = 0.1f;
    [Tooltip("회색 선의 굵기입니다. 공 지름과 같은 0.14가 기본입니다.")]
    [SerializeField] float trailWidth = 0.14f;
    [SerializeField] Color trailColor = new Color(0.55f, 0.55f, 0.55f, 0.9f);

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
        EnsureTrail();
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
        BeginTrail();
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
        EndTrail();
        gameObject.SetActive(false);
    }

    void EnsureTrail()
    {
        if (_trail != null)
        {
            return;
        }

        var child = new GameObject("Trail");
        child.transform.SetParent(transform, false);
        _trail = child.AddComponent<TrailRenderer>();
        _trail.time = Mathf.Max(0.02f, trailTime);
        _trail.minVertexDistance = 0.008f;
        _trail.widthMultiplier = Mathf.Max(0.01f, trailWidth);
        _trail.numCapVertices = 2;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.alignment = LineAlignment.View;
        _trail.textureMode = LineTextureMode.Stretch;
        _trail.sortingOrder = 4;
        _trail.material = TrailMaterial();
        var fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(trailColor.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        _trail.colorGradient = fade;
        _trail.emitting = false;
        _trail.Clear();
    }

    void BeginTrail()
    {
        EnsureTrail();
        _trail.Clear();
        _trail.emitting = true;
    }

    void EndTrail()
    {
        if (_trail == null)
        {
            return;
        }

        _trail.emitting = false;
        _trail.Clear();
    }

    static Material TrailMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        return shader != null ? SharedTrailMaterial(shader) : null;
    }

    static Material _sharedTrail;

    static Material SharedTrailMaterial(Shader shader)
    {
        if (_sharedTrail != null)
        {
            return _sharedTrail;
        }

        _sharedTrail = new Material(shader)
        {
            name = "BallTrail",
            mainTexture = Texture2D.whiteTexture
        };
        return _sharedTrail;
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
