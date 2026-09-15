using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Pinball/Launch Path (발사 경로)")]
[DisallowMultipleComponent]
public sealed class LaunchPath : MonoBehaviour
{
    [Header("경로")]
    [Tooltip("비우면 이 오브젝트의 자식들을 순서대로 경로 점으로 씁니다.")]
    [SerializeField] Transform[] points;
    [SerializeField, Range(4, 24)] int samplesPerSegment = 10;

    [Header("표시")]
    [SerializeField] Color pathColor = new Color(0.35f, 0.9f, 1f, 0.95f);

    [Header("도착 후")]
    [Tooltip("길을 다 따라간 뒤, 풀리는 각도가 이 범위(도) 안에서 달라집니다.")]
    [SerializeField] float releaseAngleJitter = 10f;
    [Tooltip("풀리는 속도가 이 비율만큼 달라집니다. 0.15면 ±15%")]
    [SerializeField] float releaseSpeedJitter = 0.15f;
    [SerializeField] float releaseSpinJitter = 180f;

    readonly List<Rider> _riders = new();
    readonly List<Transform> _waypoints = new();
    readonly List<Vector2> _samples = new();
    readonly List<float> _cum = new();
    float _length;

    public bool HasPath
    {
        get
        {
            CollectWaypoints(_waypoints);
            return _waypoints.Count >= 2;
        }
    }

    public Vector2 StartPosition
    {
        get
        {
            CollectWaypoints(_waypoints);
            return _waypoints.Count > 0 ? (Vector2)_waypoints[0].position : (Vector2)transform.position;
        }
    }

    struct Rider
    {
        public PinballBall Ball;
        public float Distance;
        public float Speed;
    }

    public void Ride(PinballBall ball, float speed)
    {
        if (ball == null || !RebuildSamples() || _length < 0.01f)
        {
            return;
        }

        Sample(0f, out Vector2 start, out _);
        if (!ball.InPlay)
        {
            ball.Launch(start, Vector2.zero);
        }

        ball.BeginGuide();
        ball.FollowGuide(start, 0f);
        _riders.Add(new Rider
        {
            Ball = ball,
            Distance = 0f,
            Speed = Mathf.Max(0.01f, speed)
        });
    }

    void OnDisable()
    {
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            if (_riders[i].Ball != null && _riders[i].Ball.Guided)
            {
                _riders[i].Ball.EndGuide(Vector2.zero);
            }
        }

        _riders.Clear();
    }

    Vector2 ReleaseVelocity(Vector2 tangent, float speed)
    {
        float angle = Random.Range(-releaseAngleJitter, releaseAngleJitter);
        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * (Vector3)tangent;
        float scaled = speed * (1f + Random.Range(-releaseSpeedJitter, releaseSpeedJitter));
        return dir * Mathf.Max(0.01f, scaled);
    }

    void FixedUpdate()
    {
        if (_riders.Count == 0)
        {
            return;
        }

        if (!RebuildSamples())
        {
            return;
        }

        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            var rider = _riders[i];
            if (rider.Ball == null || !rider.Ball.InPlay)
            {
                _riders.RemoveAt(i);
                continue;
            }

            rider.Distance += rider.Speed * Time.fixedDeltaTime;
            if (rider.Distance >= _length)
            {
                Sample(_length, out Vector2 end, out Vector2 tangent);
                rider.Ball.FollowGuide(end, 0f);
                rider.Ball.EndGuide(ReleaseVelocity(tangent, rider.Speed));
                if (rider.Ball.Body != null)
                {
                    rider.Ball.Body.angularVelocity = Random.Range(-releaseSpinJitter, releaseSpinJitter);
                }
                _riders.RemoveAt(i);
                continue;
            }

            Sample(rider.Distance, out Vector2 pos, out _);
            rider.Ball.FollowGuide(pos, 0f);
            _riders[i] = rider;
        }
    }

    bool RebuildSamples()
    {
        CollectWaypoints(_waypoints);
        if (_waypoints.Count < 2)
        {
            return false;
        }

        _samples.Clear();
        _cum.Clear();
        int steps = Mathf.Max(4, samplesPerSegment);
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            Vector2 p0 = Point(_waypoints, i - 1);
            Vector2 p1 = Point(_waypoints, i);
            Vector2 p2 = Point(_waypoints, i + 1);
            Vector2 p3 = Point(_waypoints, i + 2);
            int start = i == 0 ? 0 : 1;
            for (int s = start; s <= steps; s++)
            {
                float t = s / (float)steps;
                _samples.Add(Catmull(p0, p1, p2, p3, t));
            }
        }

        _cum.Add(0f);
        for (int i = 1; i < _samples.Count; i++)
        {
            _cum.Add(_cum[i - 1] + Vector2.Distance(_samples[i - 1], _samples[i]));
        }

        _length = _cum[_cum.Count - 1];
        return _samples.Count >= 2 && _length > 0.01f;
    }

    void Sample(float distance, out Vector2 position, out Vector2 tangent)
    {
        distance = Mathf.Clamp(distance, 0f, _length);
        int i = 0;
        while (i < _cum.Count - 2 && _cum[i + 1] < distance)
        {
            i++;
        }

        float span = _cum[i + 1] - _cum[i];
        float t = span > 0.0001f ? (distance - _cum[i]) / span : 0f;
        Vector2 a = _samples[i];
        Vector2 b = _samples[i + 1];
        position = Vector2.Lerp(a, b, t);
        tangent = b - a;
        if (tangent.sqrMagnitude < 0.0001f)
        {
            tangent = Vector2.right;
        }
        else
        {
            tangent.Normalize();
        }
    }

    void CollectWaypoints(List<Transform> into)
    {
        into.Clear();
        if (points != null && points.Length > 0)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null)
                {
                    into.Add(points[i]);
                }
            }

            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            into.Add(transform.GetChild(i));
        }
    }

    static Vector2 Point(List<Transform> waypoints, int index)
    {
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].position;
    }

    static Vector2 Catmull(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    void OnDrawGizmos()
    {
        if (!RebuildSamples())
        {
            return;
        }

        Gizmos.color = pathColor;
        for (int i = 1; i < _samples.Count; i++)
        {
            Gizmos.DrawLine(_samples[i - 1], _samples[i]);
        }

        CollectWaypoints(_waypoints);
        for (int i = 0; i < _waypoints.Count; i++)
        {
            Gizmos.DrawSphere(_waypoints[i].position, i == 0 || i == _waypoints.Count - 1 ? 0.1f : 0.07f);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("경로 점 추가")]
    void AddWaypoint()
    {
        var go = new GameObject($"Point_{transform.childCount + 1}");
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Add Launch Path Point");
        go.transform.SetParent(transform, false);
        if (transform.childCount > 1)
        {
            go.transform.position = transform.GetChild(transform.childCount - 2).position + Vector3.up * 0.7f;
        }
        else
        {
            go.transform.position = transform.position;
        }

        UnityEditor.Selection.activeGameObject = go;
    }
#endif
}
