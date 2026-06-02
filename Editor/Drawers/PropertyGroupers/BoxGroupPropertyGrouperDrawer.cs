namespace SWeetJelly.Editor
{
    using SWeetJelly.Editor.Attributes;

    [CustomPropertyGrouperDrawer(typeof(BoxGroupAttribute))]
    public class BoxGroupPropertyGrouperDrawer : PropertyGrouperDrawer
    {
        public override void BeginGroup(SweetJellyEditor editor, string groupID, string label)
        {
            EditorGUILayoutCustom.BeginBoxGroup(label);
        }

        public override void EndGroup()
        {
            EditorGUILayoutCustom.EndBoxGroup();
        }
    }
}
