using UnityEditor;
using SWeetJelly.Editor.Attributes;

namespace SWeetJelly.Editor
{
    public abstract class PropertyMetaDrawer
    {
        public abstract void ApplyPropertyMeta(SerializedProperty property, MetaAttribute metaAttribute);
    }
}
