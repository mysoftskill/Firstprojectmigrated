

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.Azure.Storage.Blob;

    /// <summary>
    /// this is a helper class for unit tests
    /// </summary>
    public class RecordCreator
    {
        public const string DevStoreUriPrefix = "https://127.0.0.1:10000/devstoreaccount1/";

        public const string Puid = "98517777777";

        public static long Id
        {
            get { return 98517777777; }
        }


        public static long RandomId
        {
            get
            {
                var rand = new Random();
                return 98510000000 + (long)rand.Next(0, 9999999);
            }
        }

        public static string GetRandomAzureBlobName(string prefix=null)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return Guid.NewGuid().ToString("N").ToLowerInvariant();
            }
            return prefix + Guid.NewGuid().ToString("N").ToLowerInvariant();
        }

        public static ExportStatusRecord CreateStatusRecord(string id = null, string requestId = null, string category = null)
        {
            return new ExportStatusRecord(string.IsNullOrWhiteSpace(requestId) ? ExportStorageProvider.GetNewRequestId() : requestId)
            {
                DataTypes = new[] { string.IsNullOrWhiteSpace(category) ? Policies.Current.DataTypes.Ids.PreciseUserLocation.Value : category },
                UserId = string.IsNullOrWhiteSpace(id) ? Puid : id,
            };
        }
    }
}
