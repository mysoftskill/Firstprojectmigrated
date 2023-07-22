using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Osgs.ServiceClient.Compass.Exceptions;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Services
{
    public class CmsService : ICmsService
    {
        private readonly ICmsLoader cmsLoader;

        public CmsService(ICmsLoader cmsLoader)
        {
            this.cmsLoader = cmsLoader ?? throw new ArgumentNullException(nameof(cmsLoader));
        }

        public async Task<IBaseCompassType> GetCmsContentItemAsync(CmsKey key, string cultureCode)
        {
            var contentItem = await cmsLoader.LoadAsync(key,cultureCode);
            if (null == contentItem)
            {
                throw new ContentNotFoundException($"CMS item {key.CompositeKey} was not found for culture code {cultureCode}.");
            }

            return contentItem;
        }

        public async Task<IDictionary<string, IBaseCompassType>> GetCmsContentItemsAsync(IEnumerable<CmsKey> keys, string cultureCode)
        {
            EnsureArgument.NotNull(keys, nameof(keys));

            var contentDictionary = keys.Distinct().ToDictionary(key => key, key => GetCmsContentItemAsync(key, cultureCode));

            await Task.WhenAll(contentDictionary.Values);

            return contentDictionary.ToDictionary(content => content.Key.CompositeKey, content => content.Value.Result, StringComparer.InvariantCultureIgnoreCase);
        }

    }
}
