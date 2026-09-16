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

    [Header("맞을 때 크기")]
    [Tooltip("바깥(또는 작은 범퍼 전체)이 커지는 배율입니다.")]
    [SerializeField] float punchScale = 1.12f;
    [Tooltip("큰 범퍼 안쪽 이미지가 커지는 배율입니다. 바깥보다 크게 두면 됩니다.")]
    [SerializeField] float innerPunchScale = 1.3f;
    [SerializeField] float punchDuration = 0.14f;
    [Tooltip("큰 범퍼에서 더 크게 팝할 안쪽 이미지들. 여기에 넣은 것만 커집니다.")]
    [SerializeField] Transform[] innerVisuals;

    [Header("게이지")]
    [Tooltip("큰 범퍼 Fill 이미지에 Sprite Radial Fill을 붙인 뒤 여기에 넣습니다.")]
    [SerializeField] SpriteRadialFill hitFill;
    [Tooltip("이 횟수만큼 맞으면 Fill이 가득 찹니다.")]
    [SerializeField] int hitsToFill = 8;
    [SerializeField] bool resetWhenFull = true;

    SpriteRenderer _renderer;
    Color _baseColor;
    Vector3 _outerRest;
    Vector3[] _innerRests;
    int _fillHits;
    Coroutine _flash;
    Coroutine _punch;

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

        _outerRest = transform.localScale;
        _innerRests = innerVisuals != null ? new Vector3[innerVisuals.Length] : System.Array.Empty<Vector3>();
        for (int i = 0; i < _innerRests.Length; i++)
        {
            _innerRests[i] = innerVisuals[i] != null ? innerVisuals[i].localScale : Vector3.one;
        }

        if (hitFill != null)
        {
            hitFill.FillAmount = 0f;
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

        Punch();
        AddFill();
        if (flashOnHit)
        {
            Flash();
        }
    }

    void AddFill()
    {
        if (hitFill == null || hitsToFill <= 0)
        {
            return;
        }

        _fillHits++;
        hitFill.FillAmount = Mathf.Clamp01(_fillHits / (float)hitsToFill);
        if (resetWhenFull && _fillHits >= hitsToFill)
        {
            _fillHits = 0;
        }
    }

    void Punch()
    {
        if (_punch != null)
        {
            StopCoroutine(_punch);
        }

        _punch = StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        float duration = Mathf.Max(0.04f, punchDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pop = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));
            transform.localScale = _outerRest * Mathf.Lerp(1f, punchScale, pop);
            ApplyInnerScale(Mathf.Lerp(1f, innerPunchScale, pop));
            yield return null;
        }

        transform.localScale = _outerRest;
        ApplyInnerScale(1f);

        _punch = null;
    }

    void ApplyInnerScale(float multiplier)
    {
        if (innerVisuals == null)
        {
            return;
        }

        int count = Mathf.Min(innerVisuals.Length, _innerRests.Length);
        for (int i = 0; i < count; i++)
        {
            if (innerVisuals[i] != null)
            {
                innerVisuals[i].localScale = _innerRests[i] * multiplier;
            }
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
