namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    /// <summary>
    /// The model for service groups. There are more properties in service tree than what is listed below.
    /// </summary>
    public class ServiceGroup : ServiceTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceGroup" /> class.
        /// </summary>
        public ServiceGroup() : base(ServiceTreeLevel.ServiceGroup)
        {
        }        
    }
}