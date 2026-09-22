using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Pinball/Score Pickup FX (캔디 점수 연출)")]
[DisallowMultipleComponent]
public sealed class ScorePickupFx : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] GameObject candyPrefab;
    [Tooltip("캔디가 날아갈 마지막 위치. Score 텍스트나 그 옆 빈 오브젝트를 넣으면 됩니다.")]
    [SerializeField] Transform target;

    [Header("연출")]
    [SerializeField] int candyCount = 5;
    [SerializeField] Vector3 spawnOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] float spread = 0.35f;
    [Tooltip("처음 나올 때 0에서 원래 크기로 커지는 시간입니다.")]
    [SerializeField] float popDuration = 0.18f;
    [Tooltip("쏘기 전에 아래로 당기는 시간입니다.")]
    [SerializeField] float pullDuration = 0.5f;
    [Tooltip("아래로 얼마나 당길지입니다.")]
    [SerializeField] float pullDistance = 0.35f;
    [SerializeField] float flyDuration = 0.55f;
    [Tooltip("날아갈 때 하나씩 늦게 출발하는 간격입니다.")]
    [SerializeField] float stagger = 0.08f;
    [SerializeField] float arcHeight = 1.1f;

    readonly Queue<GameObject> _available = new();
    readonly List<GameObject> _live = new();
    Transform _root;
    Vector3 _restScale = Vector3.one;

    void Awake()
    {
        WarmPool();
    }

    public void Play(Vector3 origin, int points)
    {
        if (candyPrefab == null || points <= 0)
        {
            PinballGame.Instance?.AddScore(points);
            return;
        }

        if (target == null)
        {
            var manager = FindFirstObjectByType<PinballManager>();
            if (manager != null)
            {
                target = manager.ScoreText != null ? manager.ScoreText.transform : null;
            }
        }

        int count = Mathf.Max(1, candyCount);
        int[] chunks = SplitPoints(points, count);
        origin += spawnOffset;
        origin.z = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 start = origin + (Vector3)(Random.insideUnitCircle * spread);
            start.z = 0f;
            var candy = Rent(start);
            if (candy == null)
            {
                continue;
            }

            StartCoroutine(SlingCandy(candy, start, chunks[i], i));
        }
    }

    public void Cancel()
    {
        StopAllCoroutines();
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            Return(_live[i]);
        }
    }

    IEnumerator SlingCandy(GameObject candy, Vector3 start, int chunk, int index)
    {
        if (stagger > 0f && index > 0)
        {
            yield return new WaitForSeconds(stagger * index);
        }

        if (candy == null || !candy.activeInHierarchy)
        {
            yield break;
        }

        candy.transform.localScale = Vector3.zero;
        float popTime = Mathf.Max(0.05f, popDuration);
        float t = 0f;
        while (t < 1f)
        {
            if (candy == null || !candy.activeInHierarchy)
            {
                yield break;
            }

            t += Time.deltaTime / popTime;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u);
            candy.transform.localScale = _restScale * ease;
            yield return null;
        }

        if (candy == null || !candy.activeInHierarchy)
        {
            yield break;
        }

        candy.transform.localScale = _restScale;
        Vector3 pulled = start;
        pulled.y -= pullDistance;
        Vector3 squashed = new Vector3(_restScale.x * 1.08f, _restScale.y * 0.88f, _restScale.z);
        float pullTime = Mathf.Max(0.05f, pullDuration);
        float flyTime = Mathf.Max(0.15f, flyDuration + Random.Range(-0.04f, 0.04f));

        t = 0f;
        while (t < 1f)
        {
            if (candy == null || !candy.activeInHierarchy)
            {
                yield break;
            }

            t += Time.deltaTime / pullTime;
            float u = Mathf.Clamp01(t);
            float stretch = u * u;
            candy.transform.position = Vector3.Lerp(start, pulled, stretch);
            candy.transform.localScale = Vector3.Lerp(_restScale, squashed, stretch);
            yield return null;
        }

        if (candy == null || !candy.activeInHierarchy)
        {
            yield break;
        }

        Vector3 launch = candy.transform.position;
        Vector3 end = GetTargetWorld();
        Vector3 kick = launch + (launch - start);
        Vector3 mid = Vector3.Lerp(launch, end, 0.45f);
        mid.y += arcHeight + Random.Range(-0.15f, 0.3f);
        mid.x += Random.Range(-0.2f, 0.2f);

        t = 0f;
        while (t < 1f)
        {
            if (candy == null || !candy.activeInHierarchy)
            {
                yield break;
            }

            t += Time.deltaTime / flyTime;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u) * (1f - u);
            candy.transform.position = Cubic(launch, kick, mid, end, ease);
            candy.transform.localScale = Vector3.Lerp(squashed, _restScale * 0.65f, ease);
            yield return null;
        }

        Return(candy);
        if (chunk > 0 && PinballGame.Instance != null)
        {
            PinballGame.Instance.AddScore(chunk);
        }
    }

    void WarmPool()
    {
        if (candyPrefab == null || _root != null)
        {
            return;
        }

        _restScale = candyPrefab.transform.localScale;
        _root = new GameObject("CandyPool").transform;
        _root.SetParent(transform, false);

        int size = Mathf.Max(8, candyCount * 3);
        for (int i = 0; i < size; i++)
        {
            _available.Enqueue(CreateCandy());
        }
    }

    GameObject CreateCandy()
    {
        var candy = Instantiate(candyPrefab, _root);
        candy.name = candyPrefab.name;
        candy.SetActive(false);
        return candy;
    }

    GameObject Rent(Vector3 position)
    {
        WarmPool();
        if (candyPrefab == null)
        {
            return null;
        }

        var candy = _available.Count > 0 ? _available.Dequeue() : CreateCandy();
        candy.transform.SetParent(null, false);
        candy.transform.SetPositionAndRotation(position, Quaternion.identity);
        candy.transform.localScale = Vector3.zero;
        candy.SetActive(true);
        _live.Add(candy);
        return candy;
    }

    void Return(GameObject candy)
    {
        if (candy == null)
        {
            return;
        }

        _live.Remove(candy);
        candy.SetActive(false);
        candy.transform.localScale = _restScale;
        if (_root != null)
        {
            candy.transform.SetParent(_root, false);
        }

        _available.Enqueue(candy);
    }

    Vector3 GetTargetWorld()
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        if (target is RectTransform)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return target.position;
            }

            Vector3 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            float depth = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            world.z = 0f;
            return world;
        }

        Vector3 position = target.position;
        position.z = 0f;
        return position;
    }

    static Vector3 Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float o = 1f - t;
        return o * o * o * a + 3f * o * o * t * b + 3f * o * t * t * c + t * t * t * d;
    }

    static int[] SplitPoints(int points, int count)
    {
        var chunks = new int[count];
        int share = points / count;
        int remain = points % count;
        for (int i = 0; i < count; i++)
        {
            chunks[i] = share + (i < remain ? 1 : 0);
        }

        return chunks;
    }
}
