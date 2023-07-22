using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client;

namespace Microsoft.PrivacyServices.UX.Core.Cms
{
    public interface ICmsClientProvider
    {
        /// <summary>
        /// Gets instance of CMS client.
        /// </summary>
        ICmsClient Instance { 
            get;
        }
    }
}
