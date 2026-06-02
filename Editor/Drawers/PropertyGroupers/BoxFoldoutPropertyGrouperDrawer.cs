namespace SWeetJelly.Editor
{
    using SWeetJelly.Editor.Attributes;

    [CustomPropertyGrouperDrawer(typeof(BoxFoldoutAttribute))]
    public class BoxFoldoutPropertyGrouperDrawer : PropertyGrouperDrawer
    {
        public override void BeginGroup(SweetJellyEditor editor, string groupID, string label)
        {
            EditorFoldoutBool foldoutBool = editor.GetFoldout(groupID);

            foldoutBool.Value = EditorGUILayoutCustom.BeginExpandBoxGroup(!string.IsNullOrEmpty(label) ? label : groupID, foldoutBool.Value);
        }

        public override void EndGroup()
        {
            EditorGUILayoutCustom.EndBoxGroup();
        }

        public override bool DrawRenderers(SweetJellyEditor editor, string groupID) => editor.GetFoldout(groupID).Value;
    }
}
