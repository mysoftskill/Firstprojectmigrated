namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks
{
    using System;
    using System.Web.Http;

    public class ServiceController : ApiController
    {
        private readonly IServiceTreeStub serviceTreeStub;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceController" /> class.
        /// </summary>
        /// <param name="serviceTreeStub">The service tree stub instance.</param>
        public ServiceController(IServiceTreeStub serviceTreeStub)
        {
            this.serviceTreeStub = serviceTreeStub;
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="config">The http configuration object.</param>
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
        }
        
        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/Services({id})")]
        public IHttpActionResult ServiceReadByIdAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("Service.ReadByIdAsync", new object[] { id }));
        }

        /// <summary>
        /// Mock API for FindByAuthenticatedUserAsync.
        /// </summary>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/PeopleHierarchy/ServiceTree.GetServicesForCurrentUser")]
        public IHttpActionResult ServiceFindByAuthenticatedUserAsync()
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("Service.FindByAuthenticatedUserAsync", null));
        }

        /// <summary>
        /// Mock API for FindByNameAsync.
        /// </summary>
        /// <param name="name">The service name.</param>
        /// <returns>A set of services.</returns>
        [HttpGet]
        [Route("api/ServiceHierarchy/ServiceTree.SearchServiceHierarchyByKeyword(Keyword={name})")]
        public IHttpActionResult ServiceFindByNameAsync([FromUri] string name)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("Service.FindByNameAsync", new object[] { name }));
        }
        
        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/ServiceGroups({id})")]
        public IHttpActionResult ServiceGroupReadByIdAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("ServiceGroup.ReadByIdAsync", new object[] { id }));
        }

        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/OrganizationHierarchy({id})/ServiceTree.GetCurrentAuthorizations")]
        public IHttpActionResult ServiceGetAuthorizationsAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("ServiceGroup.GetAuthorizationsAsync", new object[] { id }));
        }

        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/OrganizationHierarchy/ServiceTree.GetByServiceGroupId(ServiceGroupId={id})")]
        public IHttpActionResult ServiceGroupGetHierarchyAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("ServiceGroup.GetHierarchyAsync", new object[] { id }));
        }

        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/OrganizationHierarchy/ServiceTree.GetByTeamGroupId(TeamGroupId={id})")]
        public IHttpActionResult TeamGroupGetHierarchyAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("TeamGroup.GetHierarchyAsync", new object[] { id }));
        }

        /// <summary>
        /// Mock API for FindByNameAsync.
        /// </summary>
        /// <param name="name">The search term.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/OrganizationHierarchy/ServiceTree.SearchServiceGroupOrTeamGroupByKeyword(Keyword='{name}')")]
        public IHttpActionResult ServiceGroupOrTeamGroupFindByNameAsync([FromUri] string name)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("ServiceGroupOrTeamGroup.FindByNameAsync", new object[] { name }));
        }

        /// <summary>
        /// Mock API for ReadByIdAsync.
        /// </summary>
        /// <param name="id">The service id.</param>
        /// <returns>A service.</returns>
        [HttpGet]
        [Route("api/TeamGroups({id})")]
        public IHttpActionResult TeamGroupReadByIdAsync([FromUri] Guid id)
        {
            return this.ResponseMessage(this.serviceTreeStub.Execute("TeamGroup.ReadByIdAsync", new object[] { id }));
        }
    }
}