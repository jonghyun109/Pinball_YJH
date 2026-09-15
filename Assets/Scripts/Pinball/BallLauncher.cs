using UnityEngine;

[AddComponentMenu("Pinball/Launcher (발사대)")]
[DisallowMultipleComponent]
public sealed class BallLauncher : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공이 나가는 위치. 빈 자식 오브젝트를 두고 여기에 넣으면 됩니다.")]
    [SerializeField] Transform restPoint;
    [Tooltip("넣으면 발사 직후부터 이 길을 따라 마지막 점까지 간 뒤 물리를 켭니다.")]
    [SerializeField] LaunchPath launchPath;

    [Header("발사")]
    [Tooltip("한 번 누를 때 나가는 속도. 벽을 뚫지 않는 선에서 강하게 둡니다.")]
    [SerializeField] float launchSpeed = 22f;
    [SerializeField] float fireCooldown = 0.12f;
    [Tooltip("켜면 이 오브젝트의 위쪽 방향으로 발사합니다. 이미지를 회전해서 각도를 맞출 수 있습니다.")]
    [SerializeField] bool useObjectUp = true;
    [SerializeField] Vector2 launchDirection = Vector2.up;

    float _nextFireTime;

    void Awake()
    {
        if (launchPath == null)
        {
            launchPath = FindFirstObjectByType<LaunchPath>();
        }
    }

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
        if (launchPath == null)
        {
            launchPath = FindFirstObjectByType<LaunchPath>();
        }

        if (launchPath != null && launchPath.HasPath)
        {
            ball.Launch(launchPath.StartPosition, Vector2.zero);
            launchPath.Ride(ball, launchSpeed);
            return;
        }

        Vector2 origin = restPoint != null ? (Vector2)restPoint.position : (Vector2)transform.position;
        Vector2 direction = GetLaunchDirection();
        ball.Launch(origin, direction * launchSpeed);
    }

    Vector2 GetLaunchDirection()
    {
        return useObjectUp ? (Vector2)transform.up : launchDirection.normalized;
    }
}
