using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    public class RichParagraph : BaseCompassType
    {
        private string classname;

        public string ParagraphText => GetString("text");

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

        public IList<TextStyleMultiType> ParagraphStyle
        {
            get
            {
                return GetChildTypeList<TextStyleMultiType>("styleitems");
            }
        }
    }
}
