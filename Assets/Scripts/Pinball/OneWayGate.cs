using UnityEngine;

[AddComponentMenu("Pinball/One Way Gate (되돌아오기 방지)")]
[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public sealed class OneWayGate : MonoBehaviour
{
    [Tooltip("이 방향으로 나가는 공만 통과합니다. 오브젝트의 위쪽이 필드 쪽을 보게 돌리면 됩니다.")]
    [SerializeField] bool useObjectUp = true;
    [SerializeField] Vector2 allowDirection = Vector2.up;
    [Tooltip("이 값보다 더 반대로 올 때만 막습니다.")]
    [SerializeField] float blockThreshold = 0.05f;

    BoxCollider2D _gate;

    Vector2 AllowDir => useObjectUp ? (Vector2)transform.up : allowDirection.normalized;

    void Reset()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = false;
        box.usedByEffector = false;
#if UNITY_EDITOR
        box.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/PinballWall.physicsMaterial2D");
#endif
    }

    void Awake()
    {
        _gate = GetComponent<BoxCollider2D>();
        _gate.isTrigger = false;
    }

    void FixedUpdate()
    {
        if (_gate == null)
        {
            _gate = GetComponent<BoxCollider2D>();
        }

        var balls = FindObjectsByType<PinballBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            var ball = balls[i];
            if (ball == null || !ball.InPlay || ball.Body == null)
            {
                continue;
            }

            var ballCollider = ball.GetComponent<Collider2D>();
            if (ballCollider == null || !ballCollider.enabled)
            {
                continue;
            }

            Vector2 velocity = ball.Body.linearVelocity;
            bool pass = ball.Guided;
            if (!pass && velocity.sqrMagnitude > 0.0001f)
            {
                pass = Vector2.Dot(velocity.normalized, AllowDir) > -blockThreshold;
            }

            Physics2D.IgnoreCollision(ballCollider, _gate, pass);
        }
    }

    void OnDrawGizmos()
    {
        Vector2 origin = transform.position;
        Vector2 dir = AllowDir;
        Gizmos.color = new Color(0.3f, 1f, 0.45f, 0.95f);
        Gizmos.DrawLine(origin, origin + dir * 0.6f);
        Vector2 tip = origin + dir * 0.6f;
        Vector2 side = new Vector2(-dir.y, dir.x) * 0.12f;
        Gizmos.DrawLine(tip, tip - dir * 0.18f + side);
        Gizmos.DrawLine(tip, tip - dir * 0.18f - side);
    }
}
