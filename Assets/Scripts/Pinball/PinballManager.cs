using TMPro;
using UnityEngine;

[AddComponentMenu("Pinball/Manager")]
[DisallowMultipleComponent]
public sealed class PinballManager : MonoBehaviour
{
    [Tooltip("남은 공 개수를 보여줄 텍스트. 예: 20/20")]
    [SerializeField] TextMeshProUGUI ballCountText;
    [Tooltip("이번 라운드 점수 텍스트")]
    [SerializeField] TextMeshProUGUI scoreText;
    [Tooltip("최고 점수 텍스트. 게임 시작 시에도 표시됩니다.")]
    [SerializeField] TextMeshProUGUI highScoreText;

    void Start()
    {
        RefreshTexts();
    }

    public void Fire()
    {
        var launcher = PinballGame.Instance != null ? PinballGame.Instance.Launcher : FindFirstObjectByType<BallLauncher>();
        if (launcher != null)
        {
            launcher.Fire();
        }

        RefreshTexts();
    }

    public void Restart()
    {
        if (PinballGame.Instance == null || !PinballGame.Instance.CanRestart)
        {
            return;
        }

        PinballGame.Instance.Restart();
        RefreshTexts();
    }

    void LateUpdate()
    {
        RefreshTexts();
    }

    void RefreshTexts()
    {
        var game = PinballGame.Instance;
        if (game == null)
        {
            return;
        }

        if (ballCountText != null)
        {
            ballCountText.text = $"{game.RemainingBalls}/{game.TotalBalls}";
        }

        if (scoreText != null)
        {
            scoreText.text = game.Score.ToString();
        }

        if (highScoreText != null)
        {
            highScoreText.text = game.HighScore.ToString();
        }
    }
}
