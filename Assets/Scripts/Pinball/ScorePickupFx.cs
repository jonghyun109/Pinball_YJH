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
    [SerializeField] float spread = 0.35f;
    [Tooltip("쏘기 전에 아래로 당기는 시간입니다.")]
    [SerializeField] float pullDuration = 0.5f;
    [Tooltip("아래로 얼마나 당길지입니다.")]
    [SerializeField] float pullDistance = 0.35f;
    [SerializeField] float flyDuration = 0.55f;
    [Tooltip("날아갈 때 하나씩 늦게 출발하는 간격입니다.")]
    [SerializeField] float stagger = 0.08f;
    [SerializeField] float arcHeight = 1.1f;

    readonly List<GameObject> _live = new();

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
        origin.z = 0f;
        StartCoroutine(PlayRoutine(origin, chunks, count));
    }

    public void Cancel()
    {
        StopAllCoroutines();
        for (int i = 0; i < _live.Count; i++)
        {
            if (_live[i] != null)
            {
                Destroy(_live[i]);
            }
        }

        _live.Clear();
    }

    IEnumerator PlayRoutine(Vector3 origin, int[] chunks, int count)
    {
        var candies = new GameObject[count];
        var starts = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 start = origin + (Vector3)(Random.insideUnitCircle * spread);
            start.z = 0f;
            starts[i] = start;
            var candy = Instantiate(candyPrefab, start, Quaternion.identity);
            _live.Add(candy);
            candies[i] = candy;
        }

        yield return PullDown(candies, starts);

        for (int i = 0; i < count; i++)
        {
            if (candies[i] != null)
            {
                StartCoroutine(FlyCandy(candies[i], starts[i], chunks[i], i));
            }
        }
    }

    IEnumerator PullDown(GameObject[] candies, Vector3[] starts)
    {
        float duration = Mathf.Max(0.05f, pullDuration);
        var restScales = new Vector3[candies.Length];
        for (int i = 0; i < candies.Length; i++)
        {
            if (candies[i] != null)
            {
                restScales[i] = candies[i].transform.localScale;
            }
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            float stretch = u * u;
            for (int i = 0; i < candies.Length; i++)
            {
                if (candies[i] == null)
                {
                    continue;
                }

                Vector3 pulled = starts[i];
                pulled.y -= pullDistance * stretch;
                candies[i].transform.position = pulled;
                candies[i].transform.localScale = new Vector3(
                    restScales[i].x * Mathf.Lerp(1f, 1.08f, stretch),
                    restScales[i].y * Mathf.Lerp(1f, 0.88f, stretch),
                    restScales[i].z);
            }

            yield return null;
        }

        for (int i = 0; i < candies.Length; i++)
        {
            starts[i].y -= pullDistance;
            if (candies[i] != null)
            {
                candies[i].transform.localScale = restScales[i];
            }
        }
    }

    IEnumerator FlyCandy(GameObject candy, Vector3 start, int chunk, int index)
    {
        if (stagger > 0f && index > 0)
        {
            yield return new WaitForSeconds(stagger * index);
        }

        if (candy == null)
        {
            yield break;
        }

        Vector3 end = GetTargetWorld();
        Vector3 mid = Vector3.Lerp(start, end, 0.45f);
        mid.y += arcHeight + Random.Range(-0.2f, 0.35f);
        mid.x += Random.Range(-0.25f, 0.25f);

        Vector3 restScale = candy.transform.localScale;
        float duration = Mathf.Max(0.2f, flyDuration + Random.Range(-0.06f, 0.06f));
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            float ease = 1f - (1f - u) * (1f - u) * (1f - u);
            candy.transform.position = Bezier(start, mid, GetTargetWorld(), ease);
            candy.transform.localScale = Vector3.Lerp(restScale, restScale * 0.65f, ease);
            yield return null;
        }

        _live.Remove(candy);
        Destroy(candy);
        if (chunk > 0 && PinballGame.Instance != null)
        {
            PinballGame.Instance.AddScore(chunk);
        }
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

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float o = 1f - t;
        return o * o * a + 2f * o * t * b + t * t * c;
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
