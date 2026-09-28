using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Pinball/Button Press (버튼 눌림)")]
[DisallowMultipleComponent]
public sealed class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("눌릴 때 Scale Y에서 빼는 값입니다.")]
    [SerializeField] float pressDrop = 0.3f;
    [SerializeField] float duration = 0.06f;

    Vector3 _rest;
    bool _ready;
    bool _pressed;
    Coroutine _tween;

    void Awake()
    {
        _rest = transform.localScale;
        _ready = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        Vector3 target = _rest;
        target.y = Mathf.Max(0.05f, _rest.y - Mathf.Max(0f, pressDrop));
        TweenTo(target);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    void OnDisable()
    {
        if (_tween != null)
        {
            StopCoroutine(_tween);
            _tween = null;
        }

        _pressed = false;
        if (_ready)
        {
            transform.localScale = _rest;
        }
    }

    void Release()
    {
        if (!_pressed)
        {
            return;
        }

        _pressed = false;
        TweenTo(_rest);
    }

    void TweenTo(Vector3 target)
    {
        if (_tween != null)
        {
            StopCoroutine(_tween);
        }

        _tween = StartCoroutine(Tween(target));
    }

    IEnumerator Tween(Vector3 target)
    {
        Vector3 from = transform.localScale;
        float t = 0f;
        float time = Mathf.Max(0.01f, duration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time;
            transform.localScale = Vector3.Lerp(from, target, Mathf.Clamp01(t));
            yield return null;
        }

        transform.localScale = target;
        _tween = null;
    }
}
