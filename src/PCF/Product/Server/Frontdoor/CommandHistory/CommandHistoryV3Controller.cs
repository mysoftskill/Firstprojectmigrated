namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Web.Http;
    
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    /// Cold Storage Controller - For interacting with cold storage.
    /// </summary>
    [RoutePrefix("coldstorage/v3")]
    [ExcludeFromCodeCoverage]
    public partial class CommandHistoryV3Controller : ApiController
    {
        /// <summary>
        /// Gets the cold storage record for the given command ID.
        /// </summary>
        /// <url>https://pcf.privacy.microsoft.com/coldstorage/v3/status/commandid/{commandId}</url>
        /// <verb>get</verb>
        /// <group>Command History V3</group>
        /// <param in="path" name="commandId" cref="string">The command ID.</param>
        /// <response code="200"><see cref="CommandStatusResponse"/></response>
        [HttpGet]
        [Route("status/commandid/{commandId}")]
        [IncomingRequestActionFilter("ColdStorage", "GetCommandStatusById", "3.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetCommandStatus(string commandId)
        {
            if (!CommandId.TryParse(commandId, out CommandId id))
            {
                return this.BadRequest("Invalid command ID.");
            }

            var actionResult = new GetCommandStatusByCommandIdActionResult(
                this.Request,
                id,
                CommandFeedGlobals.CommandHistory,
                ExportStorageManager.Instance,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.GetFullCommandStatus);

            actionResult.RedactConfidentialFields = false;

            return actionResult;
        }

        /// <summary>
        /// Gets the cold storage records issued by the given requester.
        /// </summary>
        /// <url>https://pcf.privacy.microsoft.com/coldstorage/v3/status/query</url>
        /// <verb>get</verb>
        /// <group>Command History V3</group>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="CommandStatusResponse"/>
        /// </response>
        [HttpGet]
        [Route("status/query")]
        [IncomingRequestActionFilter("ColdStorage", "QueryCommandStatus", "3.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult QueryCommandStatusByRequester()
        {
            var queryParameters = this.Request.GetQueryNameValuePairs().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Get the subject if there is one
            IPrivacySubject subject = null;
            if (queryParameters.TryGetValue("msaPuid", out var base10Puid))
            {
                if (!long.TryParse(base10Puid, out long puid))
                {
                    throw new BadRequestException("Invalid MSA PUID.");
                }

                subject = new MsaSubject { Puid = puid };
            }
            else if (queryParameters.TryGetValue("aadObjectId", out var objectIdStr))
            {
                if (!Guid.TryParse(objectIdStr, out Guid objectId))
                {
                    throw new BadRequestException("Invalid AAD ObjectId.");
                }

                subject = new AadSubject { ObjectId = objectId };
            }

            // Get the requester if there is one
            queryParameters.TryGetValue("requester", out string requester);

            // Get the command types if present
            List<PrivacyCommandType> commandTypes = null;
            if (queryParameters.TryGetValue("commandTypes", out string commandTypesStr))
            {
                commandTypes = commandTypesStr
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(
                        s =>
                        {
                            if (!Enum.TryParse<PrivacyCommandType>(s, out var commandType))
                            {
                                throw new BadRequestException("Invalid command type");
                            }

                            return commandType;
                        })
                    .ToList();
            }

            // Get the oldest time if there is one
            DateTimeOffset oldest = DateTimeOffset.MinValue;
            if (queryParameters.TryGetValue("oldest", out string oldestStr))
            {
                if (!DateTimeOffset.TryParseExact(oldestStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out oldest))
                {
                    throw new BadRequestException("Invalid oldest timestamp");
                }
            }

            var actionResult = new QueryCommandStatusActionResult(
                subject,
                requester,
                commandTypes,
                oldest, 
                this.Request,
                CommandFeedGlobals.CommandHistory,
                ExportStorageManager.Instance,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.GetFullCommandStatus);

            actionResult.RedactConfidentialFields = false;

            return actionResult;
        }

        /// <summary>
        /// Gets the cold storage records issued by the given requester.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/coldstorage/v3/commandquery/{agent}/{assetGroup}/{command}</url>
        /// <verb>get</verb>
        /// <group>Command History V3</group>
        /// <param in="path" name="agent" cref="string">The agent ID.</param>
        /// <param in="path" name="assetGroup" cref="string">The asset group ID.</param>
        /// <param in="path" name="command" cref="string">The command ID.</param>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="CommandStatusResponse"/>
        /// </response>
        [HttpGet]
        [Route("commandquery/{agent}/{assetGroup}/{command}")]
        [IncomingRequestActionFilter("CommandHistory", "QueryCommandByCommandId", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult QueryCommandByCommandId(string agent, string assetGroup, string command)
        {
            if (!AgentId.TryParse(agent, out AgentId agentId))
            {
                return this.BadRequest("Bad agent ID");
            }

            if (!AssetGroupId.TryParse(assetGroup, out AssetGroupId assetGroupId))
            {
                return this.BadRequest("Bad asset group ID");
            }

            if (!CommandId.TryParse(command, out CommandId commandId))
            {
                return this.BadRequest("Bad command ID");
            }

            var actionResult = new QueryCommandByIdActionResult(
                this.Request,
                commandId,
                agentId,
                assetGroupId,
                CommandFeedGlobals.ServiceAuthorizer,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                AuthenticationScope.GetFullCommandStatus,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.CommandQueueFactory);

            return actionResult;
        }
    }
}
