namespace Microsoft.PrivacyServices.DataManagement.UnitTests.ClientSdks.Client.Models
{
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Xunit;
    public class SearchConverterTest
    {
		[Fact(DisplayName = "Test conversion v2 response to v1 response with Service and Component node types")]
		public void Test_ConvertSearchV2ResponseToSearchV1Response()
		{
			var v2Response = "{\"@odata.context\":\"https://servicetreeprodwest.azurewebsites.net/api/$metadata#ServiceHierarchy\",\"value\":[{\"Id\":\"048d6ea8-54ba-4e9c-ad70-1e1b99987148\",\"Name\":\"Test\",\"ShortName\":\"Test\",\"Description\":\"Test\",\"NodeType\":\"Component\",\"ParentNodeType\":\"Service\",\"OrganizationPath\":\"Experiences&Devices\\\\ModernWorkplaceTransformation\\\\Verticals\",\"ServiceGroupId\":\"eb74e7a0-60cc-4296-8a6a-bdb856106bef\",\"TeamGroupId\":null,\"Tags\":\"\",\"Path\":\"TeamsShiftsService\\\\Default\\\\Test\",\"Status\":\"Active\",\"Created\":\"2020-06-29T19:37:45.2809353Z\",\"CreatedBy\":\"madhurta@microsoft.com\",\"Modified\":\"2020-06-29T19:37:45.2809353Z\",\"ModifiedBy\":\"madhurta@microsoft.com\",\"RankingPriority\":0},{\"Id\":\"dd711ad7-42d6-4df4-b753-5bef9a39117a\",\"Name\":\"TestCloud\",\"ShortName\":\"TestCloud\",\"Description\":\"TestCloud\",\"NodeType\":\"Component\",\"ParentNodeType\":\"Service\",\"OrganizationPath\":\"Cloud+AIPlatform\\\\DeveloperServices\\\\MobileDeveloperServices\",\"ServiceGroupId\":\"64daa0a8-1751-46d9-9998-79bdbb9a1e73\",\"TeamGroupId\":null,\"Tags\":\"\",\"Path\":\"AppCenter\\\\Default\\\\TestCloud\",\"Status\":\"Active\",\"Created\":\"2016-11-07T23:08:13.8395205Z\",\"CreatedBy\":\"mattgi@microsoft.com\",\"Modified\":\"2016-11-07T23:08:13.8395205Z\",\"ModifiedBy\":\"mattgi@microsoft.com\",\"RankingPriority\":2}]}";
			var v1ResponseExpected = "[{\"ReservedId\":\"048d6ea8-54ba-4e9c-ad70-1e1b99987148\",\"ComponentDescription\":\"Test\",\"ComponentName\":\"Test\",\"ComponentOid\":\"048d6ea8-54ba-4e9c-ad70-1e1b99987148\",\"ComponentShortName\":\"Test\",\"ComponnetStatus\":\"Active\",\"Discriminator\":\"Component\",\"ServiceDescription\":null,\"ServiceGroupName\":\"Verticals\",\"ServiceName\":null,\"ServiceOid\":null,\"ServiceShortName\":null,\"ServiceStatus\":null,\"TeamGroupName\":null,\"OrganizationName\":\"ModernWorkplaceTransformation\",\"DivisionName\":\"Experiences&Devices\"},{\"ReservedId\":\"dd711ad7-42d6-4df4-b753-5bef9a39117a\",\"ComponentDescription\":\"TestCloud\",\"ComponentName\":\"TestCloud\",\"ComponentOid\":\"dd711ad7-42d6-4df4-b753-5bef9a39117a\",\"ComponentShortName\":\"TestCloud\",\"ComponnetStatus\":\"Active\",\"Discriminator\":\"Component\",\"ServiceDescription\":null,\"ServiceGroupName\":\"MobileDeveloperServices\",\"ServiceName\":null,\"ServiceOid\":null,\"ServiceShortName\":null,\"ServiceStatus\":null,\"TeamGroupName\":null,\"OrganizationName\":\"DeveloperServices\",\"DivisionName\":\"Cloud+AIPlatform\"}]";

			var v1ResponseActual = SearchConverter.ConvertSearchV2ResponseToSearchV1Response(v2Response);

			Assert.Equal(v1ResponseExpected, v1ResponseActual);
		}
    }
}
