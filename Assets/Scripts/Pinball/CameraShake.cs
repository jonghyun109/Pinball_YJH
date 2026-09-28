using UnityEngine;

[AddComponentMenu("Pinball/Camera Shake (카메라 흔들림)")]
[DisallowMultipleComponent]
public sealed class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Tooltip("한 번 흔들릴 때 움직이는 크기입니다.")]
    [SerializeField] float strength = 0.15f;
    [Tooltip("흔들림이 사라지기까지 걸리는 시간입니다.")]
    [SerializeField] float duration = 0.16f;

    Vector3 _rest;
    float _timeLeft;
    float _duration = 0.16f;
    float _strength = 0.15f;

    void Awake()
    {
        Instance = this;
        _rest = transform.localPosition;
        _duration = duration;
        _strength = strength;
    }

    void OnDisable()
    {
        transform.localPosition = _rest;
        _timeLeft = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void Play()
    {
        if (Instance == null)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Instance = camera.GetComponent<CameraShake>();
            if (Instance == null)
            {
                Instance = camera.gameObject.AddComponent<CameraShake>();
            }
        }

        Instance.Shake();
    }

    public void Shake()
    {
        _rest = _timeLeft > 0f ? _rest : transform.localPosition;
        _strength = strength;
        _duration = Mathf.Max(0.02f, duration);
        _timeLeft = _duration;
    }

    void LateUpdate()
    {
        if (_timeLeft <= 0f)
        {
            transform.localPosition = _rest;
            return;
        }

        _timeLeft -= Time.deltaTime;
        float fade = Mathf.Clamp01(_timeLeft / _duration);
        Vector2 offset = Random.insideUnitCircle * (_strength * fade);
        transform.localPosition = _rest + new Vector3(offset.x, offset.y, 0f);
    }
}
