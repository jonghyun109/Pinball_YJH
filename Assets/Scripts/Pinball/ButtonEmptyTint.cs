using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Pinball/Button Empty Tint (공 없을 때 버튼 색)")]
[DisallowMultipleComponent]
public sealed class ButtonEmptyTint : MonoBehaviour
{
    [Tooltip("공이 0이 되면 잠깐 이 색이 됩니다.")]
    [SerializeField] Color red = new Color(1f, 0.22f, 0.18f, 1f);
    [Tooltip("빨간 뒤 유지되는 색입니다.")]
    [SerializeField] Color gray = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("빨강으로 있다가 회색으로 바뀌기까지 시간입니다.")]
    [SerializeField] float redHold = 0.4f;

    Button _button;
    ColorBlock _rest;
    bool _empty;
    Coroutine _routine;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _rest = _button.colors;
        }
    }

    void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        Apply(_rest);
        _empty = false;
    }

    void Update()
    {
        if (_button == null || PinballGame.Instance == null)
        {
            return;
        }

        bool empty = PinballGame.Instance.RemainingBalls <= 0;
        if (empty == _empty)
        {
            return;
        }

        _empty = empty;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (!empty)
        {
            Apply(_rest);
            return;
        }

        _routine = StartCoroutine(RedThenGray());
    }

    IEnumerator RedThenGray()
    {
        Apply(Tint(red));
        yield return new WaitForSeconds(Mathf.Max(0.05f, redHold));
        if (_empty)
        {
            Apply(Tint(gray));
        }

        _routine = null;
    }

    ColorBlock Tint(Color color)
    {
        ColorBlock block = _rest;
        Color pressed = color * 0.82f;
        pressed.a = color.a;
        block.normalColor = color;
        block.highlightedColor = color;
        block.pressedColor = pressed;
        block.selectedColor = color;
        block.disabledColor = color;
        return block;
    }

    void Apply(ColorBlock colors)
    {
        if (_button != null)
        {
            _button.colors = colors;
        }
    }
}
