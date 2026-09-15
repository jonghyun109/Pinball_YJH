using System.Collections;
using UnityEngine;

[AddComponentMenu("Pinball/Bumper (튕기는 장애물)")]
[DisallowMultipleComponent]
public sealed class Bumper : MonoBehaviour
{
    [Tooltip("공에 추가로 가하는 힘입니다.")]
    [SerializeField] float kickForce = 1.2f;
    [SerializeField] float minImpactSpeed = 0.8f;
    [SerializeField] bool flashOnHit = true;

    SpriteRenderer _renderer;
    Color _baseColor;
    Coroutine _flash;

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }

        collider.isTrigger = false;
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
        {
            _baseColor = _renderer.color;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody == null || collision.collider.GetComponent<PinballBall>() == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
        {
            return;
        }

        Vector2 away = ((Vector2)collision.rigidbody.position - (Vector2)transform.position).normalized;
        if (away.sqrMagnitude < 0.001f && collision.contactCount > 0)
        {
            away = collision.GetContact(0).normal;
        }

        Vector2 tangent = new Vector2(-away.y, away.x);
        Vector2 kick = away * (kickForce * Random.Range(0.9f, 1.1f));
        kick += tangent * Random.Range(-0.12f, 0.12f) * kickForce;
        collision.rigidbody.AddForce(kick, ForceMode2D.Impulse);

        if (flashOnHit)
        {
            Flash();
        }
    }

    void Flash()
    {
        if (_renderer == null)
        {
            return;
        }

        if (_flash != null)
        {
            StopCoroutine(_flash);
        }

        _flash = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        _renderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        _renderer.color = _baseColor;
        _flash = null;
    }
}
