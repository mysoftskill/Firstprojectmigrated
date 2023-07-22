namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    // Adds extra client-specific partial declarations to the DeleteCommand class.
    public partial class DeleteCommand : IDeleteCommand
    {
        /// <summary>
        /// Initializes a Delete Command from the given parameters.
        /// </summary>
        public DeleteCommand(
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
            IPrivacyPredicate dataTypePredicate,
            DataTypeId dataType,
            TimeRangePredicate timeRangePredicate,
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
            this.DataTypePredicate = dataTypePredicate;
            this.PrivacyDataType = dataType;
            this.TimeRangePredicate = timeRangePredicate;
        }
    }
}
