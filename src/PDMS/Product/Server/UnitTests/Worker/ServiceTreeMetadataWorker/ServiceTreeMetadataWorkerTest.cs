using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.Models;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
using Microsoft.PrivacyServices.DataManagement.Models.Filters;
using Microsoft.PrivacyServices.DataManagement.Models.V2;
using Microsoft.PrivacyServices.DataManagement.Worker.DataOwner;
using Microsoft.PrivacyServices.DataManagement.Worker.ServiceTreeMetadata;
using Microsoft.PrivacyServices.Testing;
using Moq;
using Newtonsoft.Json;
using Ploeh.AutoFixture.Xunit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

using ST = Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

namespace Microsoft.PrivacyServices.DataManagement.UnitTests.Worker.ServiceTreeMetadataWorker
{
    public class ServiceTreeMetadataWorkerTest
    {
        [Theory(DisplayName = "Verify RunUpdates"), ValidData]
        public async Task VerifyRunUpdates(
            Lock<ServiceTreeMetadataWorkerLockState> lockStatus,
            [Frozen] Mock<IPrivacyDataStorageReader> storageReader,
            [Frozen] Mock<IServiceTreeKustoClient> serviceTreeKustoClient,
            [Frozen] Mock<IKustoClient> ngpKustoClient,
            [Frozen] Mock<IAppConfiguration> appConfiguration,
            [Frozen] Mock<ST.IServiceTreeClient> serviceTreeClient,
            DataManagement.Worker.ServiceTreeMetadata.ServiceTreeMetadataWorker worker)
        {
            ServiceTreeMetadataWorkerConstants.ServiceTreeServicesUnderDivisionQuery = "GetServicesUnderDivisionQuery<DivisionIds>";
            ServiceTreeMetadataWorkerConstants.ServiceTreeServicesWithMetadataQuery = "GetServicesWithMetadataQuery";
            ServiceTreeMetadataWorkerConstants.NGPPowerBIUrlTemplate = "https://msit.powerbi.com/groups/me/apps/15b4d804-6ae2-4bca-8019-f45f82d8ed79/reports/1cad80c7-f7ab-4f99-9f5d-7693ef03481d/ReportSection284b807dd91f43a4f94f?ctid=72f988bf-86f1-41af-91ab-2d7cd011db47&experience=power-bi&filter=DataOwnerAssetCountsV3%2FServiceId%20eq%20%27<ServiceId>%27";
            ServiceTreeMetadataWorkerConstants.PrivacyComplianceDashboardTemplate = "https://manage.privacy.microsoft-ppe.com/data-owners/edit/";
        
            appConfiguration.Setup(c => c.GetConfigValue<string>(ConfigNames.PDMS.ServiceTreeMetadataWorker_WhiteListedServices_Divisions, It.IsAny<string>())).Returns("WhiteListDivisions");
            appConfiguration.Setup(c => c.GetConfigValue<string>(ConfigNames.PDMS.ServiceTreeMetadataWorker_WhiteListedServices_Services, It.IsAny<string>())).Returns("WhiteListServices");
            appConfiguration.Setup(c => c.GetConfigValue<string>(ConfigNames.PDMS.ServiceTreeMetadataWorker_BlackListedServices_Services, It.IsAny<string>())).Returns(this.BuildBlackListedServices());
            appConfiguration.Setup(c => c.GetConfigValue<string>(ConfigNames.PDMS.ServiceTreeMetadataWorker_GetServicesWithMetadataQuery, It.IsAny<string>())).Returns("GetServicesWithMetadataQuery");
            appConfiguration.Setup(c => c.GetConfigValue<string>(ConfigNames.PDMS.ServiceTreeMetadataWorker_GetServicesUnderDivisionQuery, It.IsAny<string>())).Returns("GetServicesUnderDivisionQuery<DivisionIds>");
            appConfiguration.Setup(c => c.GetConfigValue<bool>(ConfigNames.PDMS.ServiceTreeMetadataWorker_Enabled, It.IsAny<bool>())).Returns(true);

            KustoResponse whiteListedServices = this.BuildWhitelistedServices();
            HttpResult res = new HttpResult(System.Net.HttpStatusCode.OK, "", null, HttpMethod.Post, "", "", 1, "" );
            HttpResult<KustoResponse> whiteListedServicesHttpResult = new HttpResult<KustoResponse>(res, whiteListedServices);
            serviceTreeKustoClient.Setup(c => c.QueryAsync("GetServicesUnderDivisionQueryWhiteListDivisions")).ReturnsAsync(whiteListedServicesHttpResult);

            KustoResponse servicesUnderDivision = this.BuildServicesUnderDivision();
            HttpResult<KustoResponse> servicesUnderDivisionHttpResult = new HttpResult<KustoResponse>(res, servicesUnderDivision);
            serviceTreeKustoClient.Setup(c => c.QueryAsync("GetServicesWithMetadataQuery")).ReturnsAsync(servicesUnderDivisionHttpResult);

            KustoResponse ngpServicesFromKusto = new KustoResponse();
            ngpServicesFromKusto.Rows = new List<List<object>> { };
            HttpResult<KustoResponse> ngpServicesFromKustoHttpResult = new HttpResult<KustoResponse>(res, ngpServicesFromKusto);
            ngpKustoClient.Setup(c => c.QueryAsync(It.IsAny<string>())).ReturnsAsync(ngpServicesFromKustoHttpResult);

            storageReader.Setup(m => m.GetDataOwnersAsync(It.IsAny<DataOwnerFilterCriteria>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(this.CreateDataOwnersFilterResult());

            serviceTreeClient
                .Setup(m => m.CreateMetadata(It.IsAny<Guid>(),It.IsAny<ServiceTreeMetadata>(),It.IsAny<RequestContext>()))
                .ReturnsAsync(res);
            serviceTreeClient
                .Setup(m => m.UpdateMetadata(It.IsAny<Guid>(), It.IsAny<ServiceTreeMetadata>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(res);
            serviceTreeClient
                .Setup(m => m.DeleteMetadata(It.IsAny<Guid>(), It.IsAny<RequestContext>()))
                .ReturnsAsync(res);

            await worker.DoLockWorkAsync(lockStatus, System.Threading.CancellationToken.None);

            Assert.Equal(5, serviceTreeClient.Invocations.Count);
            
            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000004"),serviceTreeClient.Invocations[0].Arguments[0]);
            Assert.Equal("CreateMetadata", serviceTreeClient.Invocations[0].Method.Name);


            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000002"), serviceTreeClient.Invocations[1].Arguments[0]);
            Assert.Equal("UpdateMetadata", serviceTreeClient.Invocations[1].Method.Name);

            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000001"), serviceTreeClient.Invocations[2].Arguments[0]);
            Assert.Equal("DeleteMetadata", serviceTreeClient.Invocations[2].Method.Name);
            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000005"), serviceTreeClient.Invocations[3].Arguments[0]);
            Assert.Equal("DeleteMetadata", serviceTreeClient.Invocations[3].Method.Name);
            Assert.Equal(new Guid("00000000-0000-0000-0000-000000000006"), serviceTreeClient.Invocations[4].Arguments[0]);
            Assert.Equal("DeleteMetadata", serviceTreeClient.Invocations[4].Method.Name);
        }

        private string BuildBlackListedServices()
        {
            string result = "";
            for (var i=0;i<7;i+=2)
            {

                result += "00000000-0000-0000-0000-00000000000" + (i + 1) + ",";
            }
            return result;
        }

        private FilterResult<DataOwner> CreateDataOwnersFilterResult()
        {
            List<DataOwner> result = new List<DataOwner> ();
            for(var i=0; i<=3; i++)
            {
                var item = new DataOwner();
                item.Id = Guid.NewGuid();
                item.ServiceTree = new ServiceTree();
                item.ServiceTree.ServiceId = "00000000-0000-0000-0000-00000000000" + (i + 1);
                result.Add(item);
            }
            return new FilterResult<DataOwner>
            {
                Values = result,
                Index = 0,
                Count = result.Count,
                Total = result.Count
            };
        }

        private KustoResponse BuildServicesUnderDivision()
        {
            KustoResponse result = new KustoResponse();
            result.Rows = new List<List<object>> { };
            for (var i = 1; i <= 7; i++)
            {
                if(i!=3 && i!=4 && i!=7)
                {
                    string serviceId = "00000000-0000-0000-0000-00000000000" + i;
                    string value = "{ \"NGP_PowerbI_URL\":\"https://msit.powerbi.com/groups/1350553e-3f5d-4ad6-9365-0e67e8fd525e/reports/1cad80c7-f7ab-4f99-9f5d-7693ef03481d/ReportSection284b807dd91f43a4f94f?filter=DataOwnerAssetCountsV3/ServiceId eq '<serviceId>'\",\"Privacy_Compliance_Dashboard\":\"https://manage.privacy.microsoft.com/data-owners/edit/<serviceId>\"}".Replace("<serviceId>", serviceId);
                    result.Rows.Add(new List<object>() { serviceId, "Service" + i, value });
                }
            }
            return result;
        }

        private KustoResponse BuildWhitelistedServices()
        {
            KustoResponse result = new KustoResponse();
            result.Rows = new List<List<object>> { };
            for(var i = 1; i <= 7; i++)
            {
                result.Rows.Add(new List<object>() { "00000000-0000-0000-0000-00000000000" + i });
            }
            return result;
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customizations.Add(new IdSpecimenBuilder());

                this.Fixture.Customize<ServiceTreeMetadataWorkerLockState>(x => x.With(y => y.InProgress, true));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }

    }
}
