using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    [CustomPropertyGrouperDrawer(typeof(FoldoutAttribute))]
    public class FoldoutPropertyGrouperDrawer : PropertyGrouperDrawer
    {
        public override void BeginGroup(SweetJellyEditor editor, string groupID, string label)
        {
            EditorFoldoutBool foldoutBool = editor.GetFoldout(groupID);

            foldoutBool.Value = EditorGUILayout.Foldout(foldoutBool.Value, !string.IsNullOrEmpty(label) ? label : groupID, true);

            EditorGUI.indentLevel++;

        }

        public override void EndGroup()
        {
            EditorGUI.indentLevel--;
        }

        public override bool DrawRenderers(SweetJellyEditor editor, string groupID) => editor.GetFoldout(groupID).Value;
    }
}
