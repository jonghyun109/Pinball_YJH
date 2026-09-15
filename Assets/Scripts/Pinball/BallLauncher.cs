using UnityEngine;

[AddComponentMenu("Pinball/Launcher (발사대)")]
[DisallowMultipleComponent]
public sealed class BallLauncher : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공이 나가는 위치. 빈 자식 오브젝트를 두고 여기에 넣으면 됩니다.")]
    [SerializeField] Transform restPoint;

    [Header("발사")]
    [Tooltip("한 번 누를 때 나가는 속도. 벽을 뚫지 않는 선에서 강하게 둡니다.")]
    [SerializeField] float launchSpeed = 22f;
    [SerializeField] float fireCooldown = 0.12f;
    [Tooltip("켜면 이 오브젝트의 위쪽 방향으로 발사합니다. 이미지를 회전해서 각도를 맞출 수 있습니다.")]
    [SerializeField] bool useObjectUp = true;
    [SerializeField] Vector2 launchDirection = Vector2.up;

    [Header("흔들림")]
    [Tooltip("발사 각도가 이 범위(도) 안에서 조금씩 달라집니다.")]
    [SerializeField] float angleJitter = 7f;
    [Tooltip("속도가 이 비율만큼 왔다 갔다 합니다. 0.15면 ±15%")]
    [SerializeField] float speedJitter = 0.12f;
    [Tooltip("발사 위치가 좌우로 살짝 어긋납니다.")]
    [SerializeField] float sideJitter = 0.05f;
    [Tooltip("공 회전도 조금씩 달라지게 합니다.")]
    [SerializeField] float spinJitter = 220f;

    float _nextFireTime;

    public void Fire()
    {
        if (Time.time < _nextFireTime)
        {
            return;
        }

        var pool = PinballGame.Instance != null ? PinballGame.Instance.Pool : null;
        if (pool == null)
        {
            return;
        }

        var ball = pool.Get();
        if (ball == null)
        {
            return;
        }

        _nextFireTime = Time.time + fireCooldown;
        Vector2 origin = restPoint != null ? (Vector2)restPoint.position : (Vector2)transform.position;
        Vector2 direction = GetLaunchDirection();
        Vector2 side = new Vector2(-direction.y, direction.x);
        origin += side * Random.Range(-sideJitter, sideJitter);

        float angle = Random.Range(-angleJitter, angleJitter);
        Vector2 launchDir = Quaternion.Euler(0f, 0f, angle) * (Vector3)direction;
        float speed = launchSpeed * (1f + Random.Range(-speedJitter, speedJitter));
        float spin = Random.Range(-spinJitter, spinJitter);
        ball.Launch(origin, launchDir * speed, spin);
    }

    Vector2 GetLaunchDirection()
    {
        return useObjectUp ? (Vector2)transform.up : launchDirection.normalized;
    }
}
