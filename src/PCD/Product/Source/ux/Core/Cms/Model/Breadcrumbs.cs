using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;
using Newtonsoft.Json;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Breadcrumbs : BaseCompassType
    {
        [JsonProperty]
        public IList<BreadcrumbLink> Links => GetChildTypeList<BreadcrumbLink>("Links");
    }
}
