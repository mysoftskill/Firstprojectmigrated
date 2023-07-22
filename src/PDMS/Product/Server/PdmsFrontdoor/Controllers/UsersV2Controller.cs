[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "Swagger documentation.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:ElementReturnValueMustBeDocumented", Justification = "Swagger documentation.")]
namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using AspNet.OData;
    using AspNet.OData.Routing;

    using Common.Authentication;

    using DataAccess.ActiveDirectory;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;

    /// <summary>
    /// Users controller.
    /// </summary>
    [ODataRoutePrefix("users")]
    public class UsersV2Controller : ODataController
    {
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly ICachedActiveDirectory cachedActiveDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersV2Controller" /> class.
        /// </summary>
        /// <param name="authenticatedPrincipal">The authenticated user.</param>
        /// <param name="cachedActiveDirectory">The active directory decorator which implements caching.</param>
        public UsersV2Controller(
            AuthenticatedPrincipal authenticatedPrincipal,
            ICachedActiveDirectory cachedActiveDirectory)
        {
            this.authenticatedPrincipal = authenticatedPrincipal;
            this.cachedActiveDirectory = cachedActiveDirectory;
        }

        /// <summary>
        /// Reads a user by id. Pass ‘me’ to use the id of the authenticated user.
        /// </summary>
        /// <group>Users V2</group>
        /// <verb>GET</verb>
        /// <header name="Authorization" required="true" type="string">Authentication token for the request. The service supports AAD user/app authentication, the header value must be in the format: Bearer {token}.</header>
        /// <header name="MS-CV" required="false" type="string">Correlation Vector for the request. If not provided, then the service generates a new one.</header>
        /// <url>https://management.privacy.microsoft.com/api/v2/users('{id}')</url>  
        /// <pathParam name="id" required="true" type="string">id of the user to retrieve.</pathParam>
        /// <response code="200"><see cref="User"/>The user if found.</response>
        [HttpGet]
        [ODataRoute("('{id}')")]
        public async Task<IHttpActionResult> ReadById([FromODataUri] string id)
        {
            // TODO: This API needs to support reading any user (not just the authenticated user) based on id eventually.
            var securityGroupIds = await this.cachedActiveDirectory.GetSecurityGroupIdsAsync(this.authenticatedPrincipal).ConfigureAwait(false);
            var user = new User()
            {
                Id = this.authenticatedPrincipal.UserId,
                SecurityGroups = securityGroupIds?.Distinct() ?? Enumerable.Empty<Guid>()
            };

            return this.Ok(user);
        }
    }
}
