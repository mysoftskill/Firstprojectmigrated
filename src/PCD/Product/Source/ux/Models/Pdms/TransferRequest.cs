using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    public class TransferRequest
    {
        /// <summary>
        /// Gets or sets transfer request ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the original owner ID.
        /// </summary>
        public string SourceOwnerId { get; set; }

        /// <summary>
        /// Gets or sets the original owner name.
        /// </summary>
        public string SourceOwnerName { get; set; }

        /// <summary>
        /// Gets or sets the new owner ID.
        /// </summary>
        public string TargetOwnerId { get; set; }

        /// <summary>
        /// Gets or sets the request state of the transfer.
        /// </summary>
        public TransferRequestStates RequestState { get; set; }

        /// <summary>
        /// Gets or sets the asset groups being transfered.
        /// </summary>
        public IEnumerable<AssetGroup> AssetGroups { get; set; }
    }
}
