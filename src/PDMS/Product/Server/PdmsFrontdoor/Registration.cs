namespace Microsoft.PrivacyServices.DataManagement.Frontdoor
{
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Web.Http;

    using Microsoft.AspNet.OData.Builder;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;

    /// <summary>
    /// Registers the controllers with the configuration class.
    /// </summary>
    public static class Registration
    {
        /// <summary>
        /// Registers the V2 controllers with the configuration class.
        /// </summary>
        public static readonly ApiRegistration Initialize = (HttpConfiguration config) =>
        {
            var builder = new ODataConventionModelBuilder();
            builder.EnableLowerCamelCase();
            builder.Namespace = "v2";

            builder.EntitySet<DataOwner>("dataOwners");
            builder.EntitySet<DataAgent>("dataAgents");
            builder.EntitySet<DataAsset>("dataAssets");
            builder.EntitySet<Inventory>("inventories");
            builder.EntitySet<AssetGroup>("assetGroups");
            builder.EntitySet<VariantDefinition>("variantDefinitions");
            builder.EntitySet<User>("users");
            builder.EntitySet<HistoryItem>("historyItems");
            builder.EntitySet<SharingRequest>("sharingRequests");
            builder.EntitySet<VariantRequest>("variantRequests");
            builder.EntitySet<Incident>("incidents");
            builder.EntitySet<TransferRequest>("transferRequests");

            var findAssetByQualifier = builder.EntityType<DataAsset>().Collection.Function("findByQualifier");
            findAssetByQualifier.Parameter<string>("qualifier");
            findAssetByQualifier.ReturnsFromEntitySet<DataAsset>("dataAssets");
            findAssetByQualifier.IsComposable = true;
            
            var findAssetGroupByAssetQualifier = builder.EntityType<AssetGroup>().Collection.Function("findByAssetQualifier");
            findAssetGroupByAssetQualifier.Parameter<string>("qualifier");
            findAssetGroupByAssetQualifier.ReturnsFromEntitySet<AssetGroup>("assetGroups");
            findAssetGroupByAssetQualifier.IsComposable = true;

            var setAgentRelationships = builder.EntityType<AssetGroup>().Collection.Action("setAgentRelationships");
            setAgentRelationships.CollectionParameter<SetAgentRelationshipParameters.Relationship>("relationships");
            setAgentRelationships.Returns<SetAgentRelationshipResponse>();

            var removeVariants = builder.EntityType<AssetGroup>().Action("removeVariants");
            removeVariants.CollectionParameter<string>("variantIds");
            removeVariants.ReturnsFromEntitySet<AssetGroup>("assetGroups");

            var findByAuthenticatedUser = builder.EntityType<DataOwner>().Collection.Function("findByAuthenticatedUser");            
            findByAuthenticatedUser.ReturnsFromEntitySet<DataOwner>("dataOwners");
            findByAuthenticatedUser.IsComposable = true;

            var replaceServiceId = builder.EntityType<DataOwner>().Action("replaceServiceId");
            replaceServiceId.EntityParameter<DataOwner>("value");
            replaceServiceId.ReturnsFromEntitySet<DataOwner>("dataOwners");

            var approveSharingRequest = builder.EntityType<SharingRequest>().Action("approve");
            var approveVariantRequest = builder.EntityType<VariantRequest>().Action("approve");
            var approveTransferRequest = builder.EntityType<TransferRequest>().Action("approve");

            var calculateAgentRegistrationStatus = builder.EntityType<DeleteAgent>().Action("calculateRegistrationStatus");
            calculateAgentRegistrationStatus.Returns<AgentRegistrationStatus>();

            var calculateAssetGroupRegistrationStatus = builder.EntityType<AssetGroup>().Action("calculateRegistrationStatus");
            calculateAssetGroupRegistrationStatus.Returns<AssetGroupRegistrationStatus>();

            var deletewithoverridependingcommands = builder.EntityType<DataAgent>().Action("override");
            deletewithoverridependingcommands.Parameter<string>("id");
            deletewithoverridependingcommands.Returns<System.Net.HttpStatusCode>();

            var forceDeleteVariantActionConfig = builder.EntityType<VariantDefinition>().Action("force");
            forceDeleteVariantActionConfig.Parameter<string>("id");
            forceDeleteVariantActionConfig.Returns<System.Net.HttpStatusCode>();

            var checkOwnership = builder.EntityType<DeleteAgent>().Action("checkOwnership");
            checkOwnership.Parameter<string>("agentId");
            checkOwnership.Returns<System.Net.HttpStatusCode>();

            // Register all data types with a specific odata namespace.
            // This is important because some odata queries use the fully qualified name
            // and that can be very verbose if we do not alter the namespace.
            var types =
                from type in typeof(DataOwner).Assembly.ExportedTypes
                where type.Namespace.EndsWith("V2")
                select type;

            foreach (var type in types)
            {
                if (type.IsEnum)
                {
                    var configuration = builder.AddEnumType(type);
                    configuration.Namespace = builder.Namespace;
                }
                else if (type.IsClass)
                {
                    if (type.GetProperties().Any(prop => prop.GetCustomAttribute<KeyAttribute>() != null))
                    {
                        var configuration = builder.AddEntityType(type);
                        configuration.Namespace = builder.Namespace;
                    }
                    else
                    {
                        var configuration = builder.AddComplexType(type);
                        configuration.Namespace = builder.Namespace;
                    }
                }
            }

            var routeName = "odata_V2"; // Must use _V2 in the name to map to V2 controllers.
            var model = builder.GetEdmModel();
            var conventions = ErrorAwareODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, config);

            config.MapODataServiceRoute(routeName, "api/v2", model, new ErrorAwareODataPathHandler(), conventions);
        };
    }
}
