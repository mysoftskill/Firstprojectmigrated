using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
using Microsoft.PrivacyServices.PrivacyOperation.Contracts;
using Microsoft.PrivacyServices.UX.Utilities;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class ManualRequestMockCooker
    {
        private readonly IFixture fixture;
        private readonly CookerUtility cookerUtility;

        private const short ExportRequestIdBase = 1;
        private const short DeleteRequestIdBase = 2;
        private const short AccountCloseRequestIdBase = 3;

        public ManualRequestMockCooker(IFixture iFixture)
        {
            fixture = iFixture;
            cookerUtility = new CookerUtility(fixture);
        }

        public Func<Task<DeleteOperationResponse>> CookDeleteOperationResponse()
        {
            return () => {
                return Task.FromResult(fixture.Build<DeleteOperationResponse>()
                                              .With(m => m.Ids, cookerUtility.CookListFrom(new[] {
                                                  cookerUtility.GenerateFuzzyGuidFromName("ManualDeleteRequest1")
                                              }))
                                              .Create());
            };
        }

        public Func<Task<ExportOperationResponse>> CookExportOperationResponse()
        {
            return () => {
                return Task.FromResult(fixture.Build<ExportOperationResponse>()
                                              .With(m => m.Ids, cookerUtility.CookListFrom(new[] { cookerUtility.GenerateFuzzyGuidFromName("ManualExportRequest1") }))
                                              .Create());
            };
        }

        public Func<Task<IList<PrivacyRequestStatus>>> CookExportRequestStatusResponse()
        {
            IList<string> requestStrings = new List<string>() {
                $"ManualExportRequest{ExportRequestIdBase}1",
                $"ManualExportRequest{ExportRequestIdBase}2"
            };
            return CookRawRequestStatusResponse(requestStrings);
        }

        public Func<Task<IList<PrivacyRequestStatus>>> CookDeleteRequestStatusResponse()
        {
            IList<string> requestStrings = new List<string>() {
                $"ManualDeleteRequest{DeleteRequestIdBase}1",
                $"ManualDeleteRequest{DeleteRequestIdBase}2"
            };
            return CookRawRequestStatusResponse(requestStrings);
        }

        public Func<Task<IList<PrivacyRequestStatus>>> CookAccountCloseRequestStatusResponse()
        {
            IList<string> requestStrings = new List<string>() {
                $"ManualAccountCloseRequest{AccountCloseRequestIdBase}1",
                $"ManualAccountCloseRequest{AccountCloseRequestIdBase}2"
            };
            return CookRawRequestStatusResponse(requestStrings);
        }

        private Func<Task<IList<PrivacyRequestStatus>>> CookRawRequestStatusResponse(IEnumerable<string> requestStrings)
        {
            return () => {
                IList<PrivacyRequestStatus> result = cookerUtility.CookListFrom(requestStrings.ToList()
                                                        .Select((requestStr) => {
                                                            return cookerUtility.CreateObjectWithReadonlyProperties<PrivacyRequestStatus>()
                                                                .WithProperty("Id", cookerUtility.GenerateFuzzyGuidFromName(requestStr))
                                                                .WithProperty("State", PrivacyRequestState.Submitted);
                                                        })
                                                        .ToList());

                return Task.FromResult(result);
            };
        }
    }
}
