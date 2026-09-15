using UnityEngine;

[AddComponentMenu("Pinball/Sprite Collider (이미지 콜라이더)")]
[RequireComponent(typeof(PolygonCollider2D))]
[DisallowMultipleComponent]
public sealed class SpriteCollider2D : MonoBehaviour
{
    [Tooltip("비우면 PinballWall 머티리얼을 씁니다.")]
    [SerializeField] PhysicsMaterial2D physicsMaterial;
    [Tooltip("값이 작을수록 외곽을 더 잘게 따라갑니다.")]
    [SerializeField, Range(0.05f, 1f)] float outlineDetail = 0.25f;
    [Tooltip("이 알파보다 진한 픽셀만 충돌 모양에 넣습니다.")]
    [SerializeField, Range(1, 250)] int alphaTolerance = 20;
    [Tooltip("아치처럼 가운데가 비어 있으면 켭니다.")]
    [SerializeField] bool detectHoles = true;

    PolygonCollider2D _polygon;
    SpriteRenderer _renderer;

    void Reset()
    {
        ApplyMaterial();
        Rebuild();
    }

    void Awake()
    {
        ApplyMaterial();
    }

    public void Rebuild()
    {
        EnsureRefs();
        ApplyMaterial();

        if (_renderer == null || _renderer.sprite == null)
        {
            Debug.LogWarning("Sprite Collider: Sprite Renderer에 이미지가 있어야 합니다.", this);
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(_polygon, "Rebuild Sprite Collider");
        }
#endif

        bool created = _polygon.CreateFromSprite(
            _renderer.sprite,
            outlineDetail,
            (byte)Mathf.Clamp(alphaTolerance, 0, 254),
            detectHoles,
            false);

        if (!created)
        {
            Debug.LogWarning("Sprite Collider: 이미지에서 외곽을 만들지 못했습니다. 스프라이트 Import 설정에서 Read/Write를 켜 보세요.", this);
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(_polygon);
        }
#endif
    }

    void EnsureRefs()
    {
        if (_polygon == null)
        {
            _polygon = GetComponent<PolygonCollider2D>();
        }

        if (_renderer == null)
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
    }

    void ApplyMaterial()
    {
        EnsureRefs();
        if (_polygon == null)
        {
            return;
        }

        if (physicsMaterial == null)
        {
#if UNITY_EDITOR
            physicsMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Physics/PinballWall.physicsMaterial2D");
#endif
        }

        _polygon.isTrigger = false;
        if (physicsMaterial != null)
        {
            _polygon.sharedMaterial = physicsMaterial;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("이미지 모양으로 다시 만들기")]
    void RebuildFromMenu()
    {
        Rebuild();
    }
#endif
}
