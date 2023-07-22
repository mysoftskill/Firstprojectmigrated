using Microsoft.Windows.Services.CompassService.Client.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Component : BaseCompassType
    {
        [JsonProperty]
        public virtual IDictionary<string, string> Strings => GetDictionaryOfStrings("Strings");

        [JsonProperty]
        public IDictionary<string, LinkType> Links => GetDictionaryOfChildType<LinkType>("Links", keySystemName: "Key", caseInsensitiveKeys: true, valuePath: "Value");
    }
}
