using System;

namespace SWeetJelly.Editor
{
    public abstract class PropertyGrouperDrawer
    {
        public abstract void BeginGroup(SweetJellyEditor editor, string groupID, string label);

        public abstract void EndGroup();

        public virtual bool DrawRenderers(SweetJellyEditor editor, string groupID)
        {
            return true;
        }
    }
}
