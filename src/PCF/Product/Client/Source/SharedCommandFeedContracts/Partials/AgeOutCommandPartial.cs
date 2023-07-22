namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    // Adds extra client-specific partial methods to the shared AgeOut command.
    public partial class AgeOutCommand : IAgeOutCommand
    {
        /// <summary>
        /// Initializes a new Age-Out command from the given parameters.
        /// </summary>
        public AgeOutCommand(
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
            DateTimeOffset lastActive,
            bool isSuspended,
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
            this.LastActive = lastActive;
            this.IsSuspended = isSuspended;
        }
    }
}
