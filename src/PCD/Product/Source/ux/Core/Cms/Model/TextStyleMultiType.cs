using System;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    public class TextStyleMultiType : BaseCompassType
    {
        public object Style
        {
            get
            {
                string typeName = BaseContent["style/#Type"].UnencodedValue;

                if (string.IsNullOrWhiteSpace(typeName))
                {
                    throw new InvalidOperationException("Could not find typeName on compass object");
                }

                switch (typeName)
                {
                    case SpanWithClassType.Kind:
                        return GetChildType<SpanWithClassType>("style");
                    case SpanWithIconType.Kind:
                        return GetChildType<SpanWithIconType>("style");
                    case SpanWithDynamicTextType.Kind:
                        return GetChildType<SpanWithDynamicTextType>("style");
                    case LinkType.Kind:
                        return GetChildType<LinkType>("style");
                    default:
                        throw new InvalidOperationException($"Unable to map {typeName} to a specific type.");
                }
            }
        }
    }
}
