using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    /// <summary>
    ///  Represents a type of text style in the form of a html span with an icon.
    /// </summary>
    public class SpanWithIconType : BaseCompassType
    {
        public const string  Kind = "spanwithicon";

        private string classname;
        
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
    }
}
