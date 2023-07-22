using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    /// <summary>
    ///  Represents a type of text style in the form of a html span with dynamic text.
    /// </summary>
    public class SpanWithDynamicTextType : BaseCompassType
    {
        public const string Kind = "dynamictext";

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
