using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client;

namespace Microsoft.PrivacyServices.UX.Core.Cms
{
    public class CmsClientProvider : ICmsClientProvider
    {
        public CmsClientProvider(ICmsClient instance)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        #region ICmsClientProvider Members
        public ICmsClient Instance
        {
            get;
        }
        #endregion
    }
}
