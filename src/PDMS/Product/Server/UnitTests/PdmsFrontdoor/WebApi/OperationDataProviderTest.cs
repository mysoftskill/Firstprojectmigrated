namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using Xunit;

    public class OperationDataProviderTest
    {
        [Theory(DisplayName = "When given a pathAndQuery for V2 APIs, then return a friendly name.")]
        //// Metadata
        [InlineData("GET", "/api/v2/$metadata", "V2.GetMetadata")]
        //// DataOwner
        [InlineData("POST", "/api/v2/dataOwners", "V2.DataOwners.Create")]
        [InlineData("PUT", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataOwners.Update")]
        [InlineData("POST", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.replaceServiceId", "V2.DataOwners.ReplaceServiceId")]
        [InlineData("DELETE", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataOwners.Delete")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.DataOwners.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=dataAgents", "V2.DataOwners.ReadWithTrackingDetailsAndDataAgents")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=assetGroups", "V2.DataOwners.ReadWithTrackingDetailsAndAssetGroups")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=dataAgents,assetGroups", "V2.DataOwners.ReadWithTrackingDetailsDataAgentsAndAssetGroups")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=dataAgents,assetGroups", "V2.DataOwners.ReadWithDataAgentsAndAssetGroups")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=dataAgents", "V2.DataOwners.ReadWithDataAgents")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=assetGroups", "V2.DataOwners.ReadWithAssetGroups")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.DataOwners.Read")]
        [InlineData("GET", "/api/v2/dataOwners('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataOwners.Read")]
        [InlineData("GET", "/api/v2/dataOwners/v2.findByAuthenticatedUser", "V2.DataOwners.FindByAuthenticatedUser")]
        [InlineData("GET", "/api/v2/dataOwners", "V2.DataOwners.ReadAll")]
        [InlineData("GET", "/api/v2/dataOwners?$top=1&$skip=1", "V2.DataOwners.ReadAll")]
        [InlineData("GET", "/api/v2/dataOwners?$skip=1&$top=1", "V2.DataOwners.ReadAll")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=contains(friendlyName,'FriendlyName')", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$filter=friendlyName eq'FriendlyName'", "V2.DataOwners.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataOwners?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.DataOwners.ReadWithFilters")]
        //// AssetGroup
        [InlineData("POST", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.removeVariants", "V2.AssetGroups.RemoveVariants")]
        [InlineData("GET", "/api/v2/assetGroups/v2.findByAssetQualifier(qualifier=@value)/complianceState?@value=stuff", "V2.AssetGroups.ComplianceStateByAssetQualifier")]
        [InlineData("POST", "/api/v2/assetGroups", "V2.AssetGroups.Create")]
        [InlineData("POST", "/api/v2/assetGroups/v2.setAgentRelationships", "V2.AssetGroups.SetAgentRelationships")]
        [InlineData("PUT", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.AssetGroups.Update")]
        [InlineData("DELETE", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.AssetGroups.Delete")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.AssetGroups.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=deleteAgent", "V2.AssetGroups.ReadWithTrackingDetailsAndDeleteAgent")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=dataAssets", "V2.AssetGroups.ReadWithTrackingDetailsAndDataAssets")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails&$expand=deleteAgent,dataAssets", "V2.AssetGroups.ReadWithTrackingDetailsDeleteAgentAndDataAssets")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=deleteAgent,dataAssets", "V2.AssetGroups.ReadWithDeleteAgentAndDataAssets")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=deleteAgent", "V2.AssetGroups.ReadWithDeleteAgent")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id&$expand=dataAssets", "V2.AssetGroups.ReadWithDataAssets")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.AssetGroups.Read")]
        [InlineData("GET", "/api/v2/assetGroups('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.AssetGroups.Read")]
        [InlineData("GET", "/api/v2/assetGroups", "V2.AssetGroups.ReadAll")]
        [InlineData("GET", "/api/v2/assetGroups?$top=1&$skip=1", "V2.AssetGroups.ReadAll")]
        [InlineData("GET", "/api/v2/assetGroups?$skip=1&$top=1", "V2.AssetGroups.ReadAll")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=contains(friendlyName,'FriendlyName')", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$filter=friendlyName eq'FriendlyName'", "V2.AssetGroups.ReadWithFilters")]
        [InlineData("GET", "/api/v2/assetGroups?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.AssetGroups.ReadWithFilters")]
        //// Inventory
        [InlineData("POST", "/api/v2/inventories", "V2.Inventories.Create")]
        [InlineData("PUT", "/api/v2/inventories('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.Inventories.Update")]
        [InlineData("GET", "/api/v2/inventories('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.Inventories.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/inventories('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.Inventories.Read")]
        [InlineData("GET", "/api/v2/inventories('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.Inventories.Read")]
        [InlineData("GET", "/api/v2/inventories", "V2.Inventories.ReadAll")]
        [InlineData("GET", "/api/v2/inventories?$top=1&$skip=1", "V2.Inventories.ReadAll")]
        [InlineData("GET", "/api/v2/inventories?$skip=1&$top=1", "V2.Inventories.ReadAll")]
        [InlineData("GET", "/api/v2/inventories?$filter=contains(friendlyName,'FriendlyName')", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$filter=friendlyName eq'FriendlyName'", "V2.Inventories.ReadWithFilters")]
        [InlineData("GET", "/api/v2/inventories?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.Inventories.ReadWithFilters")]
        //// VariantDefinition
        [InlineData("POST", "/api/v2/variantDefinitions", "V2.VariantDefinitions.Create")]
        [InlineData("PUT", "/api/v2/variantDefinitions('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.VariantDefinitions.Update")]
        [InlineData("GET", "/api/v2/variantDefinitions('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.VariantDefinitions.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/variantDefinitions('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.VariantDefinitions.Read")]
        [InlineData("GET", "/api/v2/variantDefinitions('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.VariantDefinitions.Read")]
        [InlineData("GET", "/api/v2/variantDefinitions", "V2.VariantDefinitions.ReadAll")]
        [InlineData("GET", "/api/v2/variantDefinitions?$top=1&$skip=1", "V2.VariantDefinitions.ReadAll")]
        [InlineData("GET", "/api/v2/variantDefinitions?$skip=1&$top=1", "V2.VariantDefinitions.ReadAll")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=contains(friendlyName,'FriendlyName')", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$filter=friendlyName eq'FriendlyName'", "V2.VariantDefinitions.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantDefinitions?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.VariantDefinitions.ReadWithFilters")]
        //// DataAgent
        [InlineData("POST", "/api/v2/dataAgents", "V2.DataAgents.Create")]
        [InlineData("PUT", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataAgents.Update")]
        [InlineData("DELETE", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataAgents.Delete")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.DataAgents.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.DataAgents.Read")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.DeleteAgent?$select=id,trackingDetails", "V2.DeleteAgents.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.DeleteAgent?$select=id", "V2.DeleteAgents.Read")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.DeleteAgent/v2.calculateRegistrationStatus", "V2.DeleteAgents.CalculateRegistrationStatus")]
        [InlineData("GET", "/api/v2/dataAgents('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.DataAgents.Read")]
        [InlineData("GET", "/api/v2/dataAgents", "V2.DataAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents?$top=1&$skip=1", "V2.DataAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents?$skip=1&$top=1", "V2.DataAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=contains(friendlyName,'FriendlyName')", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$filter=friendlyName eq'FriendlyName'", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.DataAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent", "V2.DeleteAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$top=1&$skip=1", "V2.DeleteAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$skip=1&$top=1", "V2.DeleteAgents.ReadAll")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=contains(friendlyName,'FriendlyName')", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$filter=friendlyName eq'FriendlyName'", "V2.DeleteAgents.ReadWithFilters")]
        [InlineData("GET", "/api/v2/dataAgents/v2.DeleteAgent?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.DeleteAgents.ReadWithFilters")]
        //// User
        [InlineData("GET", "/api/v2/users('me')?$select=id,securityGroups", "V2.Users.Read")]
        //// HistoryItem
        [InlineData("GET", "/api/v2/historyItems", "V2.HistoryItems.ReadAll")]
        [InlineData("GET", "/api/v2/historyItems?$top=1&$skip=1", "V2.HistoryItems.ReadAll")]
        [InlineData("GET", "/api/v2/historyItems?$skip=1&$top=1", "V2.HistoryItems.ReadAll")]
        //// DataAsset
        [InlineData("GET", "/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value=stuff", "V2.DataAssets.FindByQualifier")]
        //// SharingRequest
        [InlineData("POST", "/api/v2/sharingRequests('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.approve", "V2.SharingRequests.Approve")]
        [InlineData("DELETE", "/api/v2/sharingRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.SharingRequests.Delete")]
        [InlineData("GET", "/api/v2/sharingRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.SharingRequests.Read")]
        [InlineData("GET", "/api/v2/sharingRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.SharingRequests.Read")]
        [InlineData("GET", "/api/v2/sharingRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.SharingRequests.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/sharingRequests", "V2.SharingRequests.ReadAll")]
        [InlineData("GET", "/api/v2/sharingRequests?$top=1&$skip=1", "V2.SharingRequests.ReadAll")]
        [InlineData("GET", "/api/v2/sharingRequests?$skip=1&$top=1", "V2.SharingRequests.ReadAll")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=contains(friendlyName,'FriendlyName')", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$filter=friendlyName eq'FriendlyName'", "V2.SharingRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/sharingRequests?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.SharingRequests.ReadWithFilters")]
        //// VariantRequest
        [InlineData("POST", "/api/v2/variantRequests", "V2.VariantRequests.Create")]
        [InlineData("PUT", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.VariantRequests.Update")]
        [InlineData("POST", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.approve", "V2.VariantRequests.Approve")]
        [InlineData("DELETE", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.VariantRequests.Delete")]
        [InlineData("GET", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.VariantRequests.Read")]
        [InlineData("GET", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.VariantRequests.Read")]
        [InlineData("GET", "/api/v2/variantRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.VariantRequests.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/variantRequests", "V2.VariantRequests.ReadAll")]
        [InlineData("GET", "/api/v2/variantRequests?$top=1&$skip=1", "V2.VariantRequests.ReadAll")]
        [InlineData("GET", "/api/v2/variantRequests?$skip=1&$top=1", "V2.VariantRequests.ReadAll")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=contains(friendlyName,'FriendlyName')", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$filter=friendlyName eq'FriendlyName'", "V2.VariantRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/variantRequests?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.VariantRequests.ReadWithFilters")]
        //// Incident
        [InlineData("POST", "/api/v2/incidents", "V2.Incidents.Create")]
        //// TransferRequest
        [InlineData("POST", "/api/v2/transferRequests", "V2.TransferRequests.Create")]
        [InlineData("POST", "/api/v2/transferRequests('e3265b00-23be-4131-abef-fb72a22c3b03')/v2.approve", "V2.TransferRequests.Approve")]
        [InlineData("DELETE", "/api/v2/transferRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.TransferRequests.Delete")]
        [InlineData("GET", "/api/v2/transferRequests('e3265b00-23be-4131-abef-fb72a22c3b03')", "V2.TransferRequests.Read")]
        [InlineData("GET", "/api/v2/transferRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id", "V2.TransferRequests.Read")]
        [InlineData("GET", "/api/v2/transferRequests('e3265b00-23be-4131-abef-fb72a22c3b03')?$select=id,trackingDetails", "V2.TransferRequests.ReadWithTrackingDetails")]
        [InlineData("GET", "/api/v2/transferRequests", "V2.TransferRequests.ReadAll")]
        [InlineData("GET", "/api/v2/transferRequests?$top=1&$skip=1", "V2.TransferRequests.ReadAll")]
        [InlineData("GET", "/api/v2/transferRequests?$skip=1&$top=1", "V2.TransferRequests.ReadAll")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=contains(friendlyName,'FriendlyName')", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=contains(friendlyName,'FriendlyName1234567890')", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=contains(friendlyName,'FriendlyName!@#')", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=contains(friendlyName,'FriendlyName')&$top=1&$skip=1", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=contains(friendlyName,'FriendlyName')&$skip=1&$top=1", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$top=1&$filter=contains(friendlyName,'FriendlyName')", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$filter=friendlyName eq'FriendlyName'", "V2.TransferRequests.ReadWithFilters")]
        [InlineData("GET", "/api/v2/transferRequests?$top=1&$skip=1&$filter=friendlyName eq'FriendlyName'", "V2.TransferRequests.ReadWithFilters")]
        //// Non-matches. This assumes the regex patterns are the same for each entity (which they should be).
        [InlineData("GET", "/probe", null)]
        [InlineData("GET", "/openapi", null)]
        public void VerifyRegexPatterns_V2(string httpMethod, string pathAndQuery, string expectedApiName)
        {
            this.VerifyRegexPatterns(httpMethod, pathAndQuery, expectedApiName, false, OperationDataVersion.V2);
        }
        
        [Theory(DisplayName = "When given a pathAndQuery for the Probe API, then return a friendly name.")]
        [InlineData("GET", "/probe", "Probe")]
        [InlineData("GET", "/prob", null)]
        [InlineData("GET", "/probes", null)]
        [InlineData("POST", "/api/v1/configurations", null)]
        public void VerifyRegexPatterns_Probe(string httpMethod, string pathAndQuery, string expectedApiName)
        {
            this.VerifyRegexPatterns(httpMethod, pathAndQuery, expectedApiName, true, OperationDataVersion.Probe);
        }

        [Theory(DisplayName = "When given a pathAndQuery for the OpenApi document, then return a friendly name.")]
        [InlineData("GET", "/openapi", "OpenApi")]
        [InlineData("GET", "/open", null)]
        [InlineData("GET", "/openapis", null)]
        [InlineData("POST", "/api/v1/configurations", null)]
        public void VerifyRegexPatterns_OpenApi(string httpMethod, string pathAndQuery, string expectedApiName)
        {
            this.VerifyRegexPatterns(httpMethod, pathAndQuery, expectedApiName, true, OperationDataVersion.OpenApi);
        }

        private void VerifyRegexPatterns(string httpMethod, string pathAndQuery, string expectedApiName, bool excludeFromTelemetry, OperationDataVersion version)
        {
            var operationDataProvider = new OperationDataProvider(version);

            var operationData = operationDataProvider.GetFromPathAndQuery(httpMethod, pathAndQuery);

            if (expectedApiName == null)
            {
                Assert.Null(operationData);
            }
            else
            {
                Assert.Equal(expectedApiName, operationData?.Name);
                Assert.Equal(excludeFromTelemetry, operationData.ExcludeFromTelemetry);
            }
        }
    }
}