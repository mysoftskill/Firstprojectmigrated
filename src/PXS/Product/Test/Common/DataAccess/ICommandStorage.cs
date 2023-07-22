// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.Common.DataAccess
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     ICommandStorage
    /// </summary>
    public interface ICommandStorage
    {
        Task<PrivacyRequest> ReadPrivacyRequestFirstOrDefaultAsync(Guid objectId, Guid tenantId);

        Task<bool> WritePrivacyRequestAsync(PrivacyRequest request);

        Task<PrivacyRequest> ReadPrivacyRequestFirstOrDefaultAsync(long msaPuid);

        Task<DeadLetterStorage<AccountCloseRequest>> ReadDeadLetterItemAsync(string tenantId, string objectId);
    }
}
