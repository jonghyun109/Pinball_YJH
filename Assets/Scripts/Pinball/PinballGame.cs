using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Pinball/Game (게임 매니저)")]
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class PinballGame : MonoBehaviour
{
    public static PinballGame Instance { get; private set; }

    [Header("참조")]
    [Tooltip("비워 두면 씬에서 발사대를 자동으로 찾습니다.")]
    [SerializeField] BallLauncher launcher;

    [Header("물리")]
    [SerializeField] Vector2 gravity = new Vector2(0f, -16f);

    const string HighScoreKey = "PinballHighScore";

    readonly List<ScorePocket> _pockets = new();
    BallPool _pool;

    public BallPool Pool => _pool;
    public BallLauncher Launcher => launcher;
    public IReadOnlyList<ScorePocket> Pockets => _pockets;
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public string LastScoreMessage { get; private set; } = "-";
    public int RemainingBalls => _pool != null ? _pool.Remaining : 0;
    public int TotalBalls => _pool != null ? _pool.Total : 20;
    public bool CanRestart => _pool != null && _pool.CanRestart;

    void Awake()
    {
        Instance = this;
        Physics2D.gravity = gravity;
        Physics2D.velocityIterations = 14;
        Physics2D.positionIterations = 10;

        _pool = GetComponent<BallPool>();
        if (_pool == null)
        {
            _pool = gameObject.AddComponent<BallPool>();
        }

        if (launcher == null)
        {
            launcher = FindFirstObjectByType<BallLauncher>();
        }

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    void Start()
    {
        if (_pool != null)
        {
            _pool.Build();
        }

        if (launcher == null)
        {
            Debug.LogWarning("PinballGame: 발사대(BallLauncher)를 씬에 배치해 주세요.", this);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterPocket(ScorePocket pocket)
    {
        if (pocket != null && !_pockets.Contains(pocket))
        {
            _pockets.Add(pocket);
        }
    }

    public void UnregisterPocket(ScorePocket pocket)
    {
        _pockets.Remove(pocket);
    }

    public void HandlePocketScore(ScorePocket pocket, PinballBall ball)
    {
        if (pocket == null || ball == null || !ball.InPlay)
        {
            return;
        }

        Score += pocket.Points;
        LastScoreMessage = $"{pocket.Points}점";
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        RecycleBall(ball);
    }

    public void RecycleBall(PinballBall ball)
    {
        if (_pool != null)
        {
            _pool.Retire(ball);
        }
    }

    public void Restart()
    {
        if (_pool == null || !_pool.CanRestart)
        {
            return;
        }

        Score = 0;
        LastScoreMessage = "-";
        _pool.RestartRound();
    }
}
