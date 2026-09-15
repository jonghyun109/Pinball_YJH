using UnityEngine;

[AddComponentMenu("Pinball/Return Zone (발사 레일 회수)")]
[DisallowMultipleComponent]
public sealed class BallReturnZone : MonoBehaviour
{
    [Tooltip("이 안에 공이 느려진 채로 머무르면 풀로 되돌립니다.")]
    [SerializeField] float restDelay = 0.55f;
    [SerializeField] float maxSpeed = 0.35f;

    readonly System.Collections.Generic.Dictionary<PinballBall, float> _timers = new();

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var ball = other.GetComponent<PinballBall>();
        if (ball == null || ball.Guided || !ball.CanRecycle() || PinballGame.Instance == null)
        {
            return;
        }

        if (ball.Body.linearVelocity.magnitude > maxSpeed)
        {
            _timers[ball] = 0f;
            return;
        }

        _timers.TryGetValue(ball, out float timer);
        timer += Time.deltaTime;
        if (timer >= restDelay)
        {
            _timers.Remove(ball);
            PinballGame.Instance.RecycleBall(ball);
            return;
        }

        _timers[ball] = timer;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var ball = other.GetComponent<PinballBall>();
        if (ball != null)
        {
            _timers.Remove(ball);
        }
    }
}
