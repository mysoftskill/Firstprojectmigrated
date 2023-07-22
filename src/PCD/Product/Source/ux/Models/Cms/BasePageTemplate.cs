using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Models.Cms.Types
{
    public class BasePageTemplate : BaseCompassType, IBasePageTemplate
    {
        public string PageTitle => GetString("pagetitle");

        public IDictionary<string, string> Strings => GetDictionaryOfStrings("pageinfo/strings");
    }
}
