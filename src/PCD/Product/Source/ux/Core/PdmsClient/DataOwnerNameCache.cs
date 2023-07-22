using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    public class DataOwnerNameCache : IDataOwnerNameCache
    {
        // TODO: Remove this when all IPdmsClientProvider methods are mocked and exposed via IClientProviderAccessor
        private readonly IPdmsClientProvider pdmsClient;

        private readonly IMemoryCache memoryCache;

        public DataOwnerNameCache(
            IPdmsClientProvider pdmsClient,
            IMemoryCache memoryCache)
        {
            this.pdmsClient = pdmsClient ?? throw new ArgumentNullException(nameof(pdmsClient));
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<string> GetDataOwnerName(string dataOwnerId)
        {
            if (string.IsNullOrEmpty(dataOwnerId) || dataOwnerId == Guid.Empty.ToString())
            {
                return "";
            }

            var dataOwner = await memoryCache.GetOrCreateAsync(dataOwnerId, async entry =>
            {
                double cacheExpirationHours = 6;

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(cacheExpirationHours);
                return (await pdmsClient.Instance.DataOwners.ReadAsync(dataOwnerId, await pdmsClient.CreateNewRequestContext())).Response;
            });
            
            return dataOwner.Name;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetDataOwnerNamesAsync(IEnumerable<string> dataOwnerIds)
        {
            var dataOwnerNames = dataOwnerIds.Where(id => id != null).Distinct().ToDictionary(id => id, id => GetDataOwnerName(id));
            await Task.WhenAll(dataOwnerNames.Values);

            return dataOwnerNames.ToDictionary(owner => owner.Key, owner => owner.Value.Result);
        }
    }
}
