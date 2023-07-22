using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdmsModels = Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    public class VariantNameCache : IVariantNameCache
    {
        // TODO: Remove this when all IPdmsClientProvider methods are mocked and exposed via IClientProviderAccessor
        private readonly IPdmsClientProvider pdmsClient;

        private readonly IMemoryCache memoryCache;

        public VariantNameCache(
            IPdmsClientProvider pdmsClient,
            IMemoryCache memoryCache)
        {
            this.pdmsClient = pdmsClient ?? throw new ArgumentNullException(nameof(pdmsClient));
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<string> GetVariantName(string variantId)
        {
            var variant = await memoryCache.GetOrCreateAsync(variantId, async entry =>
            {
                double cacheExpirationHours = 12;

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(cacheExpirationHours);
                return (await pdmsClient.Instance.VariantDefinitions.ReadAsync(variantId, await pdmsClient.CreateNewRequestContext())).Response;
            });
            return variant.Name;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetVariantNamesAsync(IEnumerable<string> variantIds)
        {
            var variantNames = variantIds.Distinct().ToDictionary(id => id, id => GetVariantName(id));
            await Task.WhenAll(variantNames.Values);

            return variantNames.ToDictionary(v => v.Key, v => v.Value.Result);
        }
    }
}
