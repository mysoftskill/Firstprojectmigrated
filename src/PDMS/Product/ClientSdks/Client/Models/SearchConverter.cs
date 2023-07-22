
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.PrivacyServices.DataManagement.Client
{
    public class SearchConverter
    {
        public static string ConvertSearchV2ResponseToSearchV1Response(string v2response)
        {
            var searchV2Result = JsonConvert.DeserializeObject<SearchV2Response>(v2response);

            var v1ResponseValue = new List<SearchV1Data>();
            foreach (SearchV2Data v2 in searchV2Result.value)
            {
                SearchV1Data v1 = new SearchV1Data();
                v1.ReservedId = v2.Id;
                if (v2.NodeType == "Service")
                {
                    v1.ServiceName = v2.Name;
                    v1.ServiceDescription = v2.Description;
                    v1.ServiceOid = v2.Id;
                    v1.ServiceShortName = v2.ShortName;
                    v1.ServiceStatus = v2.Status;
                    v1.Discriminator = "Service";
                }
                else
                {
                    v1.ComponentName = v2.Name;
                    v1.ComponentDescription = v2.Description;
                    v1.ComponentOid = v2.Id;
                    v1.ComponentShortName = v2.ShortName;
                    v1.ComponnetStatus = v2.Status;
                    v1.Discriminator = "Component";
                }

                var paths = v2.OrganizationPath.Split('\\');
                v1.DivisionName = paths[0];
                v1.OrganizationName = paths[1];
                v1.ServiceGroupName = paths[2];
                if (paths.Length > 3)
                {
                    v1.TeamGroupName = paths[3];
                }

                v1ResponseValue.Add(v1);
            }
            return JsonConvert.SerializeObject(v1ResponseValue);
        }
    }

    public class SearchV2Response
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odata { get; set; }
        public List<SearchV2Data> value { get; set; }
    }

    public class SearchV2Data
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string NodeType { get; set; }
        public string ParentNodeType { get; set; }
        public string OrganizationPath { get; set; }
        public string ServiceGroupId { get; set; }
        public string TeamGroupId { get; set; }
        public string Tags { get; set; }
        public string Path { get; set; }
        public string Status { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class SearchV1Data
    {
        public string ReservedId { get; set; }
        public string ComponentDescription { get; set; }
        public string ComponentName { get; set; }
        public string ComponentOid { get; set; }
        public string ComponentShortName { get; set; }
        public string ComponnetStatus { get; set; }
        public string Discriminator { get; set; }
        public string ServiceDescription { get; set; }
        public string ServiceGroupName { get; set; }
        public string ServiceName { get; set; }
        public string ServiceOid { get; set; }
        public string ServiceShortName { get; set; }
        public string ServiceStatus { get; set; }
        public string TeamGroupName { get; set; }
        public string OrganizationName { get; set; }
        public string DivisionName { get; set; }
    }
}