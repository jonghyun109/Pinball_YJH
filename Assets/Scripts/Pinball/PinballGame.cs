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

    [Header("잭팟 팝업")]
    [Tooltip("잭팟 홀에 공이 하나라도 들어가고, 발사한 공이 필드에서 전부 사라지면 켜집니다.")]
    [SerializeField] GameObject jackpotPopup;
    [Tooltip("모든 공이 사라진 뒤 팝업을 띄우기까지 기다릴 시간입니다.")]
    [SerializeField] float jackpotPopupDelay = 2f;

    const string HighScoreKey = "PinballHighScore";

    readonly List<ScorePocket> _pockets = new();
    readonly List<GiftHole> _giftHoles = new();
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
    public int GiftCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < _giftHoles.Count; i++)
            {
                if (_giftHoles[i] != null)
                {
                    total += _giftHoles[i].Count;
                }
            }

            return total;
        }
    }
    public event System.Action<int, int> ScoreAdded;
    bool _jackpotPopupShown;
    float _jackpotPopupReadyTime = -1f;

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

        HideJackpotPopup();
    }

    void LateUpdate()
    {
        TryShowJackpotPopup();
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

    public void RegisterGiftHole(GiftHole hole)
    {
        if (hole != null && !_giftHoles.Contains(hole))
        {
            _giftHoles.Add(hole);
        }
    }

    public void UnregisterGiftHole(GiftHole hole)
    {
        _giftHoles.Remove(hole);
    }

    public void HandleGift(GiftHole hole, PinballBall ball)
    {
        if (hole == null || ball == null || !ball.InPlay)
        {
            return;
        }

        RecycleBall(ball);
        hole.Collect();
        TryShowJackpotPopup();
    }

    public void HandlePocketScore(ScorePocket pocket, PinballBall ball)
    {
        if (pocket == null || ball == null || !ball.InPlay)
        {
            return;
        }

        RecycleBall(ball);
        LastScoreMessage = $"{pocket.Points}점";

        var fx = FindFirstObjectByType<ScorePickupFx>();
        if (fx != null)
        {
            fx.Play(pocket.transform.position, pocket.Points);
            return;
        }

        AddScore(pocket.Points);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Score += amount;
        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        ScoreAdded?.Invoke(amount, Score);
    }

    public void RecycleBall(PinballBall ball)
    {
        if (_pool != null)
        {
            _pool.Retire(ball);
        }

        TryShowJackpotPopup();
    }

    public void Restart()
    {
        if (_pool == null || !_pool.CanRestart)
        {
            return;
        }

        Score = 0;
        LastScoreMessage = "-";
        HideJackpotPopup();
        var fx = FindFirstObjectByType<ScorePickupFx>();
        if (fx != null)
        {
            fx.Cancel();
        }

        for (int i = 0; i < _giftHoles.Count; i++)
        {
            if (_giftHoles[i] != null)
            {
                _giftHoles[i].ResetRound();
            }
        }

        _pool.RestartRound();
    }

    void TryShowJackpotPopup()
    {
        if (_jackpotPopupShown || jackpotPopup == null || GiftCount <= 0)
        {
            return;
        }

        if (_pool == null || !_pool.AllCleared)
        {
            _jackpotPopupReadyTime = -1f;
            return;
        }

        if (_jackpotPopupReadyTime < 0f)
        {
            _jackpotPopupReadyTime = Time.time + Mathf.Max(0f, jackpotPopupDelay);
        }

        if (Time.time < _jackpotPopupReadyTime)
        {
            return;
        }

        _jackpotPopupShown = true;
        jackpotPopup.SetActive(true);
    }

    void HideJackpotPopup()
    {
        _jackpotPopupShown = false;
        _jackpotPopupReadyTime = -1f;
        if (jackpotPopup != null)
        {
            jackpotPopup.SetActive(false);
        }
    }
}
