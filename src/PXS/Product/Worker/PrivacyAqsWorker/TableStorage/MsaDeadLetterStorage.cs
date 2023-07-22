// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json;

    public enum MsaAccountEventType
    {
        AccountClose,

        AccountCreate
    }

    public class MsaAccountDeadLetterInformation
    {
        [JsonProperty("cid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? Cid { get; set; }

        [JsonProperty("reason", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AccountCloseReason? CloseReason { get; set; }

        [JsonProperty("eventType")]
        [JsonRequired]
        public MsaAccountEventType EventType { get; set; }

        [JsonProperty("preverifier", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PreVerifier { get; set; }

        [JsonProperty("puid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long Puid { get; set; }
    }

    public class MsaDeadLetterStorage : DeadLetterStorage<MsaAccountDeadLetterInformation>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MsaDeadLetterStorage" /> class.
        /// </summary>
        public MsaDeadLetterStorage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsaDeadLetterStorage"/> class.
        /// </summary>
        /// <param name="puid">The row key.</param>
        /// <param name="partitionKey">The partition key.</param>
        public MsaDeadLetterStorage(long puid, string partitionKey = null)
        {
            this.RowKey = puid.ToString();
            this.PartitionKey = partitionKey ?? puid.ToString();
        }
    }
}
