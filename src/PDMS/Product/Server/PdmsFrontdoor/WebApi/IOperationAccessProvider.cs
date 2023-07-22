namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi
{
    /// <summary>
    /// Defines methods to get permissions for a give application ID.
    /// </summary>
    public interface IOperationAccessProvider
    {
        /// <summary>
        /// Finds the corresponding operation access permission or null if not found.
        /// </summary>
        /// <param name="applicationId">The application ID of the request.</param>
        /// <returns>The operation access permission.</returns>
        OperationAccessPermission GetAccessPermissions(string applicationId);
    }

    /// <summary>
    /// Contains information about an application's access permissions.
    /// </summary>
    public class OperationAccessPermission
    {
        /// <summary>
        /// Gets or sets a application ID for the operation.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets a friendly name for the operation.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the allowed operations for an application.
        /// </summary>
        public string[] AllowedOperations { get; set; }
    }

    /// <summary>
    /// A default implementation that does not restrict the operation access.
    /// </summary>
    public class DefaultOperationAccessProvider : IOperationAccessProvider
    {
        /// <summary>
        /// Simply returns a default permission which allows any operation.
        /// </summary>
        /// <param name="applicationId">The application ID of the request.</param>
        /// <returns>The default OperationAccessPermission.</returns>
        public OperationAccessPermission GetAccessPermissions(string applicationId)
        {
            return new OperationAccessPermission { ApplicationId = applicationId, FriendlyName = "Default", AllowedOperations = new[] { "*" } };
        }
    }
}