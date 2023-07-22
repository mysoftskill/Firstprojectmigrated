namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataOwner controller.
    /// </summary>
    public interface IDataOwnerClient
    {
        /// <summary>
        /// Issues a create call for the given data owner
        /// and returns the newly created data owner.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="BadArgumentError.InvalidArgument">
        /// Thrown when a property on the data owner is not valid.
        /// </exception>
        /// <exception cref="BadArgumentError.NullArgument">
        /// Thrown when a required property on the data owner is missing.
        /// </exception>
        /// <exception cref="ConflictError.AlreadyExists">
        /// [Target:friendlyName] Thrown when the friendly name already exists on another data owner.
        /// </exception>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data owner.</returns>
        Task<IHttpResult<DataOwner>> CreateAsync(DataOwner dataOwner, RequestContext requestContext);

        /// <summary>
        /// Issues a read call for the given data owner id.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="NotFoundError.Entity">
        /// Thrown when the given id cannot be found.
        /// </exception>
        /// <param name="id">The id of the data owner to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding data owner.</returns>
        Task<IHttpResult<DataOwner>> ReadAsync(string id, RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None);

        /// <summary>
        /// Issues a read call that retrieves all known data owners.
        /// If the number of existing data owners is larger than the configured server-side max page size,
        /// then only the first page of data owners are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data owner filter criteria.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <returns>A collection result with all the returned data owners, total number of existing data owners and possible next page link.</returns>
        Task<IHttpResult<Collection<DataOwner>>> ReadByFiltersAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None, DataOwnerFilterCriteria filterCriteria = null);

        /// <summary>
        /// Issues a read call that retrieves all known data owners. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data owner filter criteria.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <returns>All available data owners.</returns>
        Task<IHttpResult<IEnumerable<DataOwner>>> ReadAllByFiltersAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None, DataOwnerFilterCriteria filterCriteria = null);
        
        /// <summary>
        /// Returns all data owners for which the authenticated user has a matching security group.
        /// Returns an empty collection if no matching data owners are found.
        /// Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>All available data owners for the authenticated user.</returns>
        Task<IHttpResult<IEnumerable<DataOwner>>> FindAllByAuthenticatedUserAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None);

        /// <summary>
        /// Issues an update call for the given data owner
        /// and returns the updated data owner.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="NotFoundError.Entity">
        /// Thrown when the given data owner does not exist.
        /// </exception>
        /// <exception cref="BadArgumentError.InvalidArgument">
        /// Thrown when a property on the data owner is not valid.
        /// </exception>
        /// <exception cref="BadArgumentError.NullArgument">
        /// Thrown when a required property on the data owner is missing.
        /// </exception>
        /// <exception cref="ExpiredError.ETagMismatch">
        /// Thrown when the given data owner ETag does not match with persisted data owner.
        /// </exception>
        /// <exception cref="ConflictError.AlreadyExists">
        /// [Target:friendlyName] Thrown when the friendly name already exists on another data owner.
        /// </exception>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data owner.</returns>
        Task<IHttpResult<DataOwner>> UpdateAsync(DataOwner dataOwner, RequestContext requestContext);

        /// <summary>
        /// Deletes the data owner that has the corresponding
        /// serviceTree.serviceId and merges that owner's properties
        /// with the provided data owner.
        /// </summary>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="NotFoundError.Entity">
        /// Thrown when the given data owner does not exist.
        /// </exception>
        /// <exception cref="BadArgumentError.InvalidArgument">
        /// Thrown when a property on the data owner is not valid.
        /// </exception>
        /// <exception cref="BadArgumentError.NullArgument">
        /// Thrown when a required property on the data owner is missing.
        /// </exception>
        /// <exception cref="ExpiredError.ETagMismatch">
        /// Thrown when the given data owner ETag does not match with persisted data owner.
        /// </exception>
        /// <exception cref="ConflictError.AlreadyExists">
        /// [Target:name] Thrown when the name already exists on another data owner.
        /// </exception>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The data owner with properties updated by the service.</returns>
        Task<IHttpResult<DataOwner>> ReplaceServiceIdAsync(DataOwner dataOwner, RequestContext requestContext);

        /// <summary>
        /// Issues a delete call with the given data owner id.
        /// </summary>
        /// <param name="id">The id of the data owner to delete.</param>
        /// <param name="etag">The ETag of the data owner.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext);
    }
}