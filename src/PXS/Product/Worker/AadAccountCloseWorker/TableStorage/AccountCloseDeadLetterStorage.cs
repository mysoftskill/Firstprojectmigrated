// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <inheritdoc />
    public class AccountCloseDeadLetterStorage : DeadLetterStorage<AccountCloseRequest>
    {
    }
}
