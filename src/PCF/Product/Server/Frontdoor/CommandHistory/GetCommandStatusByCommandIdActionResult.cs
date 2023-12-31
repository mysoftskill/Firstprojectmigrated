namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Looks up the cold storage record for the given command ID.
    /// </summary>
    internal class GetCommandStatusByCommandIdActionResult : GetCommandStatusActionResult
    {
        private readonly CommandId commandId;

        public GetCommandStatusByCommandIdActionResult(
            HttpRequestMessage request,
            CommandId commandId, 
            ICommandHistoryRepository repository,
            IExportStorageManager exportStorageManager,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope) : base(request, repository, exportStorageManager, dataAgentMap, authorizer, authenticationScope)
        {
            this.commandId = commandId;
        }

        protected override bool ReturnAssetGroupStatuses => true;

        protected override bool ReturnMultiple => false;

        protected override async Task<IEnumerable<CommandHistoryRecord>> QueryAsync(ICommandHistoryRepository repository, CommandHistoryFragmentTypes fragmentsToRead)
        {
            var record = await repository.QueryAsync(this.commandId, fragmentsToRead);

            if (record == null)
            {
                return null;
            }

            return new[] { record };
        }
    }
}
