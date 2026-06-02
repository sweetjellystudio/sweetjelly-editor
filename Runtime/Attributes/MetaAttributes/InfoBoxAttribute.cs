using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class InfoBoxAttribute : MetaAttribute
    {
        public string Text { get; private set; }
        public InfoBoxType Type { get; private set; }
        public string VisibleIf { get; private set; }

        public InfoBoxAttribute(string text, InfoBoxType type = InfoBoxType.Normal, string visibleIf = null)
        {
            Text = text;
            Type = type;
            VisibleIf = visibleIf;
        }
    }

    public enum InfoBoxType
    {
        Normal,
        Warning,
        Error
    }
}
