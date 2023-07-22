namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    // Adds extra client-specific partial methods to the shared AccoutClose command.
    public partial class AccountCloseCommand : IAccountCloseCommand
    {
        /// <summary>
        /// Initializes a new Account Close command from the given parameters.
        /// </summary>
        public AccountCloseCommand(
            string commandId,
            string assetGroupId,
            string assetGroupQualifier,
            string verifier,
            string correlationVector,
            string leaseReceipt,
            DateTimeOffset approximateLeaseExpiration,
            DateTimeOffset createdTime,
            IPrivacySubject subject,
            string agentState,
            ICommandFeedClient commandFeedClient,
            string cloudInstance = null)
            : base(
                CommandTypeName,
                commandId,
                assetGroupId,
                assetGroupQualifier,
                verifier,
                correlationVector,
                leaseReceipt,
                approximateLeaseExpiration,
                createdTime,
                subject,
                agentState,
                commandFeedClient,
                cloudInstance)
        {
        }
    }
}
