using UnityEngine;

[AddComponentMenu("Pinball/Sprite Radial Fill (시계방향 채우기)")]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class SpriteRadialFill : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] float fillAmount;
    [SerializeField] bool clockwise = true;

    SpriteRenderer _renderer;
    MaterialPropertyBlock _block;
    static Material _material;

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            fillAmount = Mathf.Clamp01(value);
            Apply();
        }
    }

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        fillAmount = Mathf.Clamp01(fillAmount);
        Apply();
    }

    public void Apply()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        if (_renderer == null)
        {
            return;
        }

        if (_material == null)
        {
            var shader = Shader.Find("Pinball/Sprite Radial Fill");
            if (shader == null)
            {
                return;
            }

            _material = new Material(shader)
            {
                name = "SpriteRadialFill",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        if (_renderer.sharedMaterial != _material)
        {
            _renderer.sharedMaterial = _material;
        }

        _block ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat("_FillAmount", fillAmount);
        _block.SetFloat("_Clockwise", clockwise ? 1f : 0f);
        _renderer.SetPropertyBlock(_block);
    }
}
