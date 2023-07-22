using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class ServiceTreeMockCooker
    {
        private readonly IFixture fixture;
        private readonly CookerUtility cookerUtility;

        public ServiceTreeMockCooker(IFixture iFixture)
        {
            fixture = iFixture;
            cookerUtility = new CookerUtility(fixture);
        }

        public Func<Task<IHttpResult<IEnumerable<Hierarchy>>>> CookListOfServicesFor(string teamName)
        {
            return () => {
                return CookHttpResultEnumerableTaskFor(cookerUtility.CookListFrom(new[] { CookHierarchyFor(teamName) }));
            };
        }

        public Func<Task<IHttpResult<Service>>> CookServiceFor(string teamName)
        {
            return () =>
            {
                return CookHttpResultTaskFor(fixture.Build<Service>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(teamName))
                            .With(m => m.Name, $"I9n_{teamName}_Name")
                            .With(m => m.Description, $"I9n_{teamName}_Desc")
                            .With(m => m.AdminUserNames, cookerUtility.CookListFrom(new[] { $"I9n_{teamName}_Admin" }))
                            .With(m => m.Level, ServiceTreeLevel.Service)
                            .Create());
            };
        }

        private Hierarchy CookHierarchyFor(string teamName)
        {
            return fixture.Build<Hierarchy>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(teamName))
                            .With(m => m.Name, $"I9n_{teamName}_Name")
                            .With(m => m.Level, ServiceTreeLevel.Service)
                            .Create();
        }

        private Task<IHttpResult<IEnumerable<Hierarchy>>> CookHttpResultEnumerableTaskFor(IEnumerable<Hierarchy> hierarchy)
        {
            IHttpResult<IEnumerable<Hierarchy>> result = new HttpResult<IEnumerable<Hierarchy>>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), hierarchy);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<Service>> CookHttpResultTaskFor(Service service)
        {
            IHttpResult<Service> result = new HttpResult<Service> (
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), service);
            return Task.FromResult(result);
        }
    }
}
