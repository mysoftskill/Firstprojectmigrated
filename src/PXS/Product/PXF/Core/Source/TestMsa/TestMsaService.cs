// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.TestMsa
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    public class TestMsaService : ITestMsaService
    {
        private readonly IAccountDeleteWriter accountDeleteWriter;

        private readonly IXboxAccountsAdapter xboxAccountsAdapter;

        public TestMsaService(IAccountDeleteWriter accountDeleteWriter, IXboxAccountsAdapter xboxAccountsAdapter)
        {
            this.accountDeleteWriter = accountDeleteWriter ?? throw new ArgumentNullException(nameof(accountDeleteWriter));
            this.xboxAccountsAdapter = xboxAccountsAdapter ?? throw new ArgumentNullException(nameof(xboxAccountsAdapter));
        }

        /// <inheritdoc cref="ITestMsaService.PostTestMsaCloseAsync(IRequestContext)"/>.
        public async Task<ServiceResponse<Guid>> PostTestMsaCloseAsync(IRequestContext requestContext)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            AdapterResponse<string> getXuidTask = 
                await this.xboxAccountsAdapter.GetXuidAsync(RequestContextConverter.ToAdapterRequestContext(requestContext)).ConfigureAwait(false);
            if (!getXuidTask.IsSuccess)
                return new ServiceResponse<Guid>
                {
                    Error = new Error(ErrorCode.PartnerError, getXuidTask.Error.Message)
                };

            var accountDeleteInformation = new AccountDeleteInformation
            {
                Reason = AccountCloseReason.Test,
                Cid = requestContext.TargetCid.Value,
                Puid = requestContext.TargetPuid,
                Xuid = getXuidTask.Result
            };

            AdapterResponse<AccountDeleteInformation> response = await this.accountDeleteWriter.WriteDeleteAsync(accountDeleteInformation, nameof(TestMsaService)).ConfigureAwait(false);
            if (response.IsSuccess)
            {
                return new ServiceResponse<Guid>
                {
                    Result = response.Result.CommandId
                };
            }

            return new ServiceResponse<Guid>
            {
                Error = new Error(ErrorCode.PartnerError, response.Error.Message)
            };
        }
    }
}
