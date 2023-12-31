namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A collection of methods exposed for PXS.
    /// </summary>
    [RoutePrefix("pxs")]
    public class PxsController : ApiController
    {
        /// <summary>
        /// Receives an array of Commands from PXS and inserts them into queues.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pxs/commands</url>
        /// <verb>post</verb>
        /// <group>PXS</group>
        /// <response code="200"></response>
        [HttpPost]
        [Route("commands")]
        [IncomingRequestActionFilter("PXS", "PostCommands", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> PostCommands()
        {
            string content = await this.Request.Content.ReadAsStringAsync();
            JObject[] pxsCommands = JsonConvert.DeserializeObject<JObject[]>(content);

            return new PxsInsertCommandActionResult(
                this.Request,
                CommandFeedGlobals.ExpandCommandBatchWorkItemPublisher,
                CommandFeedGlobals.CommandValidationService,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.PxsService,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap().Version,
                pxsCommands);
        }

        /// <summary>
        /// Receives export Id from PXS and delete that entry from PCF storage.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pxs/deleteexport/</url>
        /// <verb>delete</verb>
        /// <group>PXS</group>
        /// <response code="200"></response>
        [HttpDelete]
        [Route("deleteexport")]
        [IncomingRequestActionFilter("PXS", "DeleteExport", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> DeleteExport()
        {
            string requestBody = await this.Request.Content.ReadAsStringAsync();
            var requestMessage = JsonConvert.DeserializeObject<DeleteExportArchiveParameters>(requestBody);
            if (!CommandId.TryParse(requestMessage.CommandId, out CommandId id))
            {
               return this.BadRequest("Invalid command ID.");
            }

            return new PxsDeleteExportArchiveActionResult(
               this.Request,
               CommandFeedGlobals.DeleteFullExportArchivePublisher,
               CommandFeedGlobals.ServiceAuthorizer,
               AuthenticationScope.PxsService,
               requestMessage);
        }

    }
}
