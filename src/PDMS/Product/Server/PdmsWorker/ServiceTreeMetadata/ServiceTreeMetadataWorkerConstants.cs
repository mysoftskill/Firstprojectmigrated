using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.DataManagement.Worker.ServiceTreeMetadata
{
    public static class ServiceTreeMetadataWorkerConstants
    {
        public static string ServiceTreeServicesWithMetadataQuery = "let Services = ServiceTree_ServiceHierarchy_Snapshot \r\n| where Level == \"Service\"; \r\nlet Metadatas = ServiceTree_ServiceMetadata_Snapshot \r\n| where MetadataDefinitionInternalId == 1337; \r\nServices  \r\n| join Metadatas on $left.InternalId == $right.ServiceInternalId\r\n| project Id, Name, Value;";
        public static string ServiceTreeServicesUnderDivisionQuery = "let DivisionInternalIds = ServiceTree_OrganizationHierarchy_Snapshot\r\n| where Status == 1 and Id in (<DivisionIds>)\r\n| project InternalId;\r\nlet OrgIds = ServiceTree_OrganizationHierarchy_Snapshot\r\n| where Status == 1 and (ParentDivisionInternalId in (DivisionInternalIds) or InternalId in (DivisionInternalIds))\r\n| project InternalId;\r\nlet SGIds = ServiceTree_OrganizationHierarchy_Snapshot\r\n| where Status == 1 and (ParentOrganizationInternalId in (OrgIds) or InternalId in (OrgIds))\r\n| project InternalId;\r\nlet TGIds = ServiceTree_OrganizationHierarchy_Snapshot\r\n| where Status == 1 and (ParentOrganizationInternalId in (OrgIds) or ParentServiceGroupInternalId in (SGIds) or InternalId in (SGIds))\r\n| project InternalId;\r\nServiceTree_ServiceHierarchy\r\n| where Level == \"Service\" and Status == 1 and (ParentTeamGroupInternalId in (TGIds) or ParentServiceGroupInternalId in (SGIds))\r\n| project Id;\r\n";
        public static string NGPPowerBIUrlTemplate = "https://msit.powerbi.com/groups/me/apps/15b4d804-6ae2-4bca-8019-f45f82d8ed79/reports/1cad80c7-f7ab-4f99-9f5d-7693ef03481d/ReportSection284b807dd91f43a4f94f?ctid=72f988bf-86f1-41af-91ab-2d7cd011db47&experience=power-bi&filter=DataOwnerAssetCountsV3%2FServiceId%20eq%20%27<ServiceId>%27";
        public static string PrivacyComplianceDashboardTemplate = "https://manage.privacy.microsoft-ppe.com/data-owners/edit/";
    }
}
