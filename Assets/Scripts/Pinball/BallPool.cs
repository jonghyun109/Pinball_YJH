using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Pinball/Ball Pool (공 풀)")]
[DefaultExecutionOrder(-60)]
[DisallowMultipleComponent]
public sealed class BallPool : MonoBehaviour
{
    [Tooltip("비워 두면 씬에 있는 공을 원본으로 복제합니다.")]
    [SerializeField] PinballBall prefab;
    [SerializeField] int size = 20;

    readonly Queue<PinballBall> _available = new();
    readonly List<PinballBall> _all = new();
    Transform _root;
    bool _built;

    public int Remaining => _available.Count;
    public int Total => _all.Count > 0 ? _all.Count : size;
    public bool CanFire => _available.Count > 0;
    public bool CanRestart => _built && _available.Count == 0;

    void Awake()
    {
        Build();
    }

    public void Build()
    {
        if (_built)
        {
            return;
        }

        if (prefab == null)
        {
            prefab = FindFirstObjectByType<PinballBall>(FindObjectsInactive.Include);
        }

        if (prefab == null)
        {
            Debug.LogWarning("BallPool: 원본 공(PinballBall)이 없습니다. 공 이미지를 하나 배치해 주세요.", this);
            return;
        }

        _root = new GameObject("Balls").transform;
        _root.SetParent(transform, false);

        RegisterInstance(prefab, true);
        int copies = Mathf.Max(0, size - 1);
        for (int i = 0; i < copies; i++)
        {
            var clone = Instantiate(prefab, _root);
            clone.name = $"{prefab.name}_{i + 1}";
            RegisterInstance(clone, false);
        }

        _built = true;
    }

    public PinballBall Get()
    {
        if (!_built)
        {
            Build();
        }

        if (_available.Count == 0)
        {
            return null;
        }

        var ball = _available.Dequeue();
        ball.gameObject.SetActive(true);
        return ball;
    }

    public void Retire(PinballBall ball)
    {
        if (ball == null)
        {
            return;
        }

        ball.SleepInPool();
    }

    public void RestartRound()
    {
        if (!_built)
        {
            Build();
        }

        _available.Clear();
        for (int i = 0; i < _all.Count; i++)
        {
            _all[i].SleepInPool();
            _available.Enqueue(_all[i]);
        }
    }

    void RegisterInstance(PinballBall ball, bool reparent)
    {
        if (reparent)
        {
            ball.transform.SetParent(_root, true);
        }

        _all.Add(ball);
        ball.SleepInPool();
        _available.Enqueue(ball);
    }
}
