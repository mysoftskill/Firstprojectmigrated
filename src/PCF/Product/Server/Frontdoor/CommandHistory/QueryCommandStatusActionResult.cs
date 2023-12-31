namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Looks up the cold storage records by the given parameters.
    /// </summary>
    internal class QueryCommandStatusActionResult : GetCommandStatusActionResult
    {
        private readonly IPrivacySubject subject;

        private readonly string requester;

        private readonly IList<PrivacyCommandType> commandTypes;

        private readonly DateTimeOffset oldestRecord;

        public QueryCommandStatusActionResult(
            IPrivacySubject subject,
            string requester,
            IList<PrivacyCommandType> commandTypes,
            DateTimeOffset oldestRecord,
            HttpRequestMessage request,
            ICommandHistoryRepository repository,
            IExportStorageManager exportStorageManager, 
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope) : base(request, repository, exportStorageManager, dataAgentMap, authorizer, authenticationScope)
        {
            this.subject = subject;
            this.requester = requester;
            this.commandTypes = commandTypes;
            this.oldestRecord = oldestRecord;
        }

        protected override bool ReturnAssetGroupStatuses => false;

        protected override bool ReturnMultiple => true;

        protected override Task<IEnumerable<CommandHistoryRecord>> QueryAsync(ICommandHistoryRepository repository, CommandHistoryFragmentTypes fragmentsToRead)
        {
            return repository.QueryAsync(
                this.subject,
                this.requester,
                this.commandTypes,
                this.oldestRecord,
                fragmentsToRead);
        }
    }
}
