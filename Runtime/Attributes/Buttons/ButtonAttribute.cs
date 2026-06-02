using System;

namespace SWeetJelly.Editor.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        private string title;
        public string Title => title;

        private object[] methodParams;
        public object[] Params => methodParams;

        private string visibilityConditionName;
        public string VisabilityConditionName => visibilityConditionName;

        private ButtonVisibility visibilityOption;
        public ButtonVisibility VisibilityOption => visibilityOption;
        
        public ButtonAttribute()
        {
            title = null;
            methodParams = null;

            visibilityConditionName = null;
        }

        public ButtonAttribute(string title = null, params object[] methodParams)
        {
            this.title = title;
            this.methodParams = methodParams;

            visibilityConditionName = null;
        }

        public ButtonAttribute(string title = null, string visibilityConditionName= "", ButtonVisibility visibilityOption = ButtonVisibility.HideIf, params object[] methodParams)
        {
            this.title = title;
            this.visibilityConditionName = visibilityConditionName;
            this.visibilityOption = visibilityOption;
            this.methodParams = methodParams;
        }
    }
}
