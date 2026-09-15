#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PinballMirrorTool
{
    [MenuItem("Pinball/선택 오브젝트를 활성 기준으로 좌우 대칭 복제")]
    static void DuplicateMirrored()
    {
        var axis = Selection.activeTransform;
        if (axis == null)
        {
            EditorUtility.DisplayDialog("Pinball", "먼저 오브젝트를 선택하세요.\n마지막에 고른 오브젝트의 X를 대칭축으로 씁니다.", "확인");
            return;
        }

        var selected = Selection.transforms;
        if (selected.Length < 2)
        {
            EditorUtility.DisplayDialog(
                "Pinball",
                "대칭축이 될 가운데 오브젝트를 마지막에 클릭한 뒤,\n복사할 왼쪽 범퍼들을 같이 선택하세요.",
                "확인");
            return;
        }

        var axisX = axis.position.x;
        var created = new System.Collections.Generic.List<GameObject>();

        Undo.IncrementCurrentGroup();
        var group = Undo.GetCurrentGroup();

        foreach (var src in selected)
        {
            if (src == axis)
                continue;

            var copy = Object.Instantiate(src.gameObject, src.parent);
            copy.name = src.name + "_R";
            Undo.RegisterCreatedObjectUndo(copy, "Mirror Duplicate");

            var p = src.position;
            p.x = 2f * axisX - p.x;
            copy.transform.position = p;
            copy.transform.rotation = src.rotation;

            var s = src.localScale;
            s.x = -Mathf.Abs(s.x);
            copy.transform.localScale = s;

            created.Add(copy);
        }

        Undo.SetCurrentGroupName("Mirror Duplicate");
        Undo.CollapseUndoOperations(group);

        if (created.Count == 0)
        {
            EditorUtility.DisplayDialog("Pinball", "대칭 복제할 오브젝트가 없습니다.", "확인");
            return;
        }

        Selection.objects = created.ToArray();
    }
}
#endif
