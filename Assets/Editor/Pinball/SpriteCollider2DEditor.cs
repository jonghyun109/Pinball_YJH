#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteCollider2D))]
public sealed class SpriteCollider2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("이미지 모양으로 다시 만들기", GUILayout.Height(28)))
        {
            var collider = (SpriteCollider2D)target;
            collider.Rebuild();
        }
    }
}
#endif
