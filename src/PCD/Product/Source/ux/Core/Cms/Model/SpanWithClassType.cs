using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    /// <summary>
    ///  Represents a type of text style in the form of a html span with a class attribute.
    /// </summary>
    public class SpanWithClassType : BaseCompassType
    {
        public const string Kind = "spanwithclass";

        private string classname;
        private string text;
        
        public string Key => GetString("key");

        public string CssClassName
        {
            get
            {
                if (classname == null)
                {
                    TryGetString("classname", out classname);
                }
                return classname;
            }
            set
            {
                classname = value;
            }
        }

        public string Text
        {
            get
            {
                if (text == null)
                {
                    TryGetString("text", out text);
                }
                return text;
            }
            set
            {
                text = value;
            }
        }
    }
}
