using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.V2;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class DataOwnerMockCooker
    {
        private readonly IFixture fixture;
        private readonly CookerUtility cookerUtility;

        public DataOwnerMockCooker(IFixture iFixture)
        {
            fixture = iFixture;
            cookerUtility = new CookerUtility(fixture);
        }

        public Func<Task<IHttpResult<IEnumerable<DataOwner>>>> CookListOfDataOwners()
        {
            return () => {
                return CookHttpResultEnumerableTaskFor(
                    cookerUtility.CookListFrom(new[] {
                        CookRawOwnerFor("Team1"),
                        CookRawOwnerFor("Team2")
                    }));
            };
        }

        public Func<Task<IHttpResult<DataOwner>>> CookDataOwnerFor(string teamName)
        {
            return () => {
                return CookHttpResultTaskFor(CookRawOwnerFor(teamName));
            };
        }

        public Func<Task<IHttpResult>> CookEmptyHttpResult()
        {
            return cookerUtility.CookEmptyHttpResult();
        }

        private DataOwner CookRawOwnerFor(string teamName)
        {
            var writeSecurityGroups = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_WriteSG@microsoft.com" });
            var taggingSecurityGroups = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_TaggingSG@microsoft.com" });
            var alertContacts = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_AlertContact@microsoft.com" });
            var announcementContacts = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_AnnouncementContact@microsoft.com" });
            var sharingRequestContacts = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_SharingRequestContact@microsoft.com" });
            var serviceAdmins = cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_ServiceAdmin@microsoft.com" });

            var serviceTree = fixture.Build<ServiceTree>()
                                .With(m => m.ServiceId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                                .With(m => m.DivisionId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                                .With(m => m.OrganizationId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                                .With(m => m.ServiceAdmins, serviceAdmins)
                                .Create();

            return fixture.Build<DataOwner>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                            .With(m => m.Name, $"I9n_{teamName}_Name")
                            .With(m => m.Description, $"I9n_{teamName}_Description")
                            .With(m => m.AlertContacts, alertContacts)
                            .With(m => m.AnnouncementContacts, announcementContacts)
                            .With(m => m.SharingRequestContacts, sharingRequestContacts)
                            .With(m => m.WriteSecurityGroups, writeSecurityGroups)
                            .With(m => m.TagSecurityGroups, taggingSecurityGroups)
                            .With(m => m.ServiceTree, serviceTree)
                            .Create();
        }

        private Task<IHttpResult<IEnumerable<DataOwner>>> CookHttpResultEnumerableTaskFor(IEnumerable<DataOwner> dataOwners)
        {
            IHttpResult<IEnumerable<DataOwner>> result = new HttpResult<IEnumerable<DataOwner>>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), dataOwners);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<DataOwner>> CookHttpResultTaskFor(DataOwner dataOwner)
        {
            IHttpResult<DataOwner> result = new HttpResult<DataOwner>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), dataOwner);
            return Task.FromResult(result);
        }
    }
}
