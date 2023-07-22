using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.PrivacyServices.UX.Core.ClientProviderAccessor;
using Microsoft.Windows.Services.CompassService.Client;
using Microsoft.Windows.Services.CompassService.Client.Model;
using CmsModels = Microsoft.PrivacyServices.UX.Core.Cms.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    public class CmsLoader : ICmsLoader
    {
        private readonly ICmsKeyParser cmsKeyParser;
        private readonly IClientProviderAccessor<ICmsClientProvider> cmsClientProviderAccessor;

        public CmsLoader(
            IClientProviderAccessor<ICmsClientProvider> cmsClientProviderAccessor,
            ICmsKeyParser cmsKeyParser)
        {
            this.cmsClientProviderAccessor = cmsClientProviderAccessor ?? throw new ArgumentNullException(nameof(cmsClientProviderAccessor));
            this.cmsKeyParser = cmsKeyParser ?? throw new ArgumentNullException(nameof(cmsKeyParser));
        }

        public async Task<IBaseCompassType> LoadAsync(CmsKey key, string cultureCode)
        {
            var parsedCmsKey = cmsKeyParser.Parse(key);

            switch (parsedCmsKey.CmsTypeName)
            {
                case "component":
                    return await cmsClientProviderAccessor.ProviderInstance.Instance.GetContentItemAsync<CmsModels.Component>(parsedCmsKey.CmsLocation, cultureCode);

                case "page":
                    return await cmsClientProviderAccessor.ProviderInstance.Instance.GetContentItemAsync<CmsModels.Page>(parsedCmsKey.CmsLocation, cultureCode);

                default:
                    throw new NotSupportedException($"CMS type {parsedCmsKey.CmsTypeName} is not supported.");
            }

        }
    }
}
