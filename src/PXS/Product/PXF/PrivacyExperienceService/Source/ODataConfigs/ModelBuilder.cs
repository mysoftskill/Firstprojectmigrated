// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.ODataConfigs
{
    using Microsoft.AspNet.OData.Builder;
    using Microsoft.OData.Edm;
    using Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1;

    /// <summary>
    ///     Public static class for ODataModelBuilder.
    /// </summary>
    public static class ModelBuilder
    {
        /// <summary>
        ///     The OData Namespace for the ModelBuilder
        /// </summary>
        public const string ODataNamespace = "Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1";
        
        /// <summary>
        ///     Build EDM Model.
        /// </summary>
        /// <returns></returns>
        public static IEdmModel GetEdmModel()
        {
            var modelBuilder = new ODataConventionModelBuilder();

            modelBuilder.Namespace = ODataNamespace;
            modelBuilder.EnableLowerCamelCase();

            modelBuilder.EntitySet<DataPolicyOperation>("dataPolicyOperations");

            modelBuilder.EntitySet<User>("users");
            ActionConfiguration userExportPersonalDataAction = modelBuilder.EntityType<User>().Action("exportPersonalData");
            userExportPersonalDataAction.Parameter<string>("storageLocation");

            modelBuilder.Singleton<Directory>("directory");

            ActionConfiguration softDeletedExportPersonalDataAction = modelBuilder.EntityType<DeletedItem>().Action("exportPersonalData");
            softDeletedExportPersonalDataAction.Parameter<string>("storageLocation");

            modelBuilder.EntityType<TenantReference>().Action("removePersonalData");

            modelBuilder.EntityType<InboundSharedUserProfile>().Action("removePersonalData");
            ActionConfiguration inboundExportPersonalDataAction = modelBuilder.EntityType<InboundSharedUserProfile>().Action("exportPersonalData");
            inboundExportPersonalDataAction.Parameter<string>("storageLocation");

            return modelBuilder.GetEdmModel();
        }
    }
}
