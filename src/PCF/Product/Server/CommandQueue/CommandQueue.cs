// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public abstract class CommandQueue
    {
        public abstract bool SupportsLeaseReceipt(LeaseReceipt leaseReceipt);

        protected void CheckLeaseReceipt(LeaseReceipt leaseReceipt)
        {
            if (!this.SupportsLeaseReceipt(leaseReceipt))
            {
                throw new CommandFeedException("The given lease receipt was not supported.")
                {
                    ErrorCode = CommandFeedInternalErrorCode.InvalidLeaseReceipt,
                    IsExpected = false,
                };
            }
        }
    }
}
