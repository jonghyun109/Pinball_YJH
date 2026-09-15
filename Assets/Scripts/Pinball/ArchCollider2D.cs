using UnityEngine;

[AddComponentMenu("Pinball/Arch Collider (아치 콜라이더)")]
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class ArchCollider2D : MonoBehaviour
{
    const string HostName = "_ArchShape";

    [Header("모양")]
    [SerializeField] Vector2 center = Vector2.zero;
    [SerializeField] float radius = 2.2f;
    [Tooltip("가로/세로 비율. (1, 1)이면 정원입니다. X를 키우면 옆으로 넓은 타원, Y를 키우면 위로 긴 타원이 됩니다.")]
    [SerializeField] Vector2 ellipseScale = new Vector2(1.2f, 1f);
    [SerializeField] float startAngle = 0f;
    [SerializeField] float endAngle = 180f;
    [SerializeField, Range(4, 64)] int segments = 24;
    [Tooltip("레일의 둥근 두께. 공이 모서리에 덜 걸립니다.")]
    [SerializeField] float edgeRadius = 0.03f;

    [Header("미리보기")]
    [Tooltip("아치 이미지가 없을 때 게임 화면에서 곡선을 보여줍니다. 그림을 넣으면 끄면 됩니다.")]
    [SerializeField] bool showPreview = true;
    [SerializeField] Color previewColor = new Color(0.86f, 0.78f, 0.45f, 1f);
    [SerializeField] float previewWidth = 0.12f;

    [Header("물리")]
    [SerializeField] PhysicsMaterial2D physicsMaterial;

    Transform _host;
    EdgeCollider2D _edge;
    LineRenderer _line;
    static Material _previewMaterial;

    void Reset()
    {
        Rebuild();
    }

    void Awake()
    {
        Rebuild();
    }

    void OnValidate()
    {
        radius = Mathf.Max(0.05f, radius);
        ellipseScale.x = Mathf.Max(0.05f, ellipseScale.x);
        ellipseScale.y = Mathf.Max(0.05f, ellipseScale.y);
        segments = Mathf.Clamp(segments, 4, 64);
        edgeRadius = Mathf.Max(0f, edgeRadius);
        previewWidth = Mathf.Max(0.01f, previewWidth);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildDelayed;
        UnityEditor.EditorApplication.delayCall += RebuildDelayed;
#endif
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildDelayed;
#endif
    }

    void RebuildDelayed()
    {
        if (this != null)
        {
            Rebuild();
        }
    }

    [ContextMenu("Rebuild Arch")]
    public void Rebuild()
    {
        EnsureHost();
        EnsureCollider();

        var points = BuildArcPoints();
        _edge.enabled = true;
        _edge.edgeRadius = edgeRadius;
        _edge.sharedMaterial = physicsMaterial;
        _edge.points = points;
        ApplyPreview(points);
    }

    Vector2[] BuildArcPoints()
    {
        int count = segments + 1;
        var points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float rad = Mathf.Deg2Rad * Mathf.Lerp(startAngle, endAngle, t);
            points[i] = center + new Vector2(
                Mathf.Cos(rad) * radius * ellipseScale.x,
                Mathf.Sin(rad) * radius * ellipseScale.y);
        }

        return points;
    }

    void EnsureHost()
    {
        _host = transform.Find(HostName);
        if (_host == null)
        {
            var go = new GameObject(HostName);
            _host = go.transform;
            _host.SetParent(transform, false);
        }

        _host.localPosition = Vector3.zero;
        _host.localRotation = Quaternion.identity;
        Vector3 parentScale = transform.lossyScale;
        _host.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, parentScale.x),
            1f / Mathf.Max(0.0001f, parentScale.y),
            1f);
        _host.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
    }

    void ApplyPreview(Vector2[] points)
    {
        if (_line == null)
        {
            _line = _host.GetComponent<LineRenderer>();
            if (_line == null)
            {
                _line = _host.gameObject.AddComponent<LineRenderer>();
            }
        }

        _line.enabled = showPreview;
        if (!showPreview)
        {
            return;
        }

        _line.useWorldSpace = false;
        _line.loop = false;
        _line.textureMode = LineTextureMode.Stretch;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.positionCount = points.Length;
        _line.startWidth = previewWidth;
        _line.endWidth = previewWidth;
        _line.startColor = previewColor;
        _line.endColor = previewColor;
        _line.sortingOrder = 8;
        _line.sharedMaterial = GetPreviewMaterial();
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;

        for (int i = 0; i < points.Length; i++)
        {
            _line.SetPosition(i, points[i]);
        }
    }

    static Material GetPreviewMaterial()
    {
        if (_previewMaterial != null)
        {
            return _previewMaterial;
        }

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        }

        _previewMaterial = new Material(shader)
        {
            name = "ArchPreview",
            hideFlags = HideFlags.HideAndDontSave
        };
        return _previewMaterial;
    }

    void EnsureCollider()
    {
        var extras = _host.GetComponents<PolygonCollider2D>();
        for (int i = 0; i < extras.Length; i++)
        {
            DestroyCollider(extras[i]);
        }

        var edges = _host.GetComponents<EdgeCollider2D>();
        if (edges.Length == 0)
        {
            _edge = _host.gameObject.AddComponent<EdgeCollider2D>();
            return;
        }

        _edge = edges[0];
        for (int i = 1; i < edges.Length; i++)
        {
            DestroyCollider(edges[i]);
        }
    }

    static void DestroyCollider(Object collider)
    {
        if (collider == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(collider);
        }
        else
        {
            DestroyImmediate(collider);
        }
    }

    void OnDrawGizmosSelected()
    {
        var points = BuildArcPoints();
        Gizmos.color = new Color(0.98f, 0.72f, 0.28f, 0.95f);
        Vector3 origin = transform.position;
        Quaternion rotation = transform.rotation;
        for (int i = 1; i < points.Length; i++)
        {
            Gizmos.DrawLine(origin + rotation * (Vector3)points[i - 1], origin + rotation * (Vector3)points[i]);
        }
    }
}
