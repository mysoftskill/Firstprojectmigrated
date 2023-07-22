namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Http;

    using AutoMapper;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Icm;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;

    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Incident controller.
    /// </summary>
    [ODataRoutePrefix("incidents")]
    public class IncidentV2Controller : ODataController
    {
        private readonly IIcmConnector icmConnector;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentV2Controller" /> class.
        /// </summary>
        /// <param name="icmConnector">The ICM connector.</param>
        /// <param name="mapper">The auto-mapper instance.</param>
        public IncidentV2Controller(
            IIcmConnector icmConnector,
            IMapper mapper)
        {
            this.icmConnector = icmConnector;
            this.mapper = mapper;
        }

        /// <summary>
        /// Creates an incident and sends it to ICM.
        /// </summary>
        /// <group>Incidents V2</group>
        /// <verb>POST</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication. For AAD, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/incidents</url>  
        /// <requestType><see cref="Incident"/>An incident with routing information provided. Must include at minimum one of ownerId, agentId, or assetGroupId in the routing data.</requestType>
        /// <response code="200"><see cref="Incident"/>The created incident with service generated properties filled in.</response>          
        /// <param name="value">The incident.</param>
        /// <returns>The result.</returns>
        [HttpPost]
        [ODataRoute("")]
        public async Task<IHttpActionResult> Create([FromBody] Incident value)
        {
            var response = await EntityModule.CreateAsync<Core.Incident, Incident>(
                    ModelState,
                    value,
                    this.mapper,
                    this.icmConnector.CreateIncidentAsync).ConfigureAwait(false);
            return this.Created(response);
        }
    }
}