using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.V2;
using Microsoft.PrivacyServices.Policy;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class DataAgentMockCooker
    {
        private readonly IFixture fixture;
        private readonly CookerUtility cookerUtility;

        public DataAgentMockCooker(IFixture iFixture)
        {
            fixture = iFixture;
            cookerUtility = new CookerUtility(fixture);
        }

        public Func<bool[]> CookOperationalReadinessFor(string agentName)
        {
            return () => {
                bool[] pdmsOpReadiness = new bool[128];
                Array.Clear(pdmsOpReadiness, 0, pdmsOpReadiness.Length);

                return pdmsOpReadiness;
            };
        }

        public Func<Task<IHttpResult<IEnumerable<DeleteAgent>>>> CookListOfDeleteAgentsFor(string teamName)
        {
            return () => {
                return CookHttpResultEnumerableTaskFor(cookerUtility.CookListFrom(new[] {
                    CookRawDeleteAgentFor("Agent1", teamName),
                    CookRawDeleteAgentFor("Agent2", teamName)
                }));
            };
        }

        public Func<Task<IHttpResult<IEnumerable<DeleteAgent>>>> CookListOfDeleteAgentsWithoutProdConnectionFor(string teamName)
        {
            return () => {
                return CookHttpResultEnumerableTaskFor(cookerUtility.CookListFrom(new[] {
                    CookRawDeleteAgentWithoutProdFor("Agent1", teamName),
                    CookRawDeleteAgentWithoutProdFor("Agent2", teamName)
                }));
            };
        }

        public Func<Task<IHttpResult<DeleteAgent>>> CookDataAgentFor(string teamName)
        {
            return () => {
                return CookHttpResultTaskFor(CookRawDeleteAgentFor("Agent1", teamName));
            };
        }

        public Func<Task<IHttpResult<Collection<DeleteAgent>>>> CookListOfDataAgentsWithCountFor(string teamName)
        {
            return () => {
                return CookHttpResultCollectionTaskFor(fixture.Build<Collection<DeleteAgent>>()
                                                                .With(m => m.Total, cookerUtility.GetNumberFromName(teamName))
                                                                .Create());
            };
        }

        public Func<Task<IHttpResult>> CookEmptyHttpResult()
        {
            return cookerUtility.CookEmptyHttpResult();
        }

        private DeleteAgent CookRawDeleteAgentFor(string agentName, string teamName)
        {
            var cloudInstanceId = Policies.Current.CloudInstances.Ids.Public;
            var supportedClouds = cookerUtility.CookListFrom(new[] { cloudInstanceId });

            return fixture.Build<DeleteAgent>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(agentName).ToString())
                            .With(m => m.OwnerId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                            .With(m => m.Name, $"I9n_{agentName}_{teamName}_Name")
                            .With(m => m.ConnectionDetails, CookConnectionDetailsFor(agentName, teamName))
                            .With(m => m.DeploymentLocation, cloudInstanceId)
                            .With(m => m.SupportedClouds, supportedClouds)
                            .Create();
        }

        private DeleteAgent CookRawDeleteAgentWithoutProdFor(string agentName, string teamName)
        {
            var cloudInstanceId = Policies.Current.CloudInstances.Ids.Public;
            var supportedClouds = cookerUtility.CookListFrom(new[] { cloudInstanceId });

            return fixture.Build<DeleteAgent>()
                            .With(m => m.Id, cookerUtility.GenerateFuzzyGuidFromName(agentName).ToString())
                            .With(m => m.OwnerId, cookerUtility.GenerateFuzzyGuidFromName(teamName).ToString())
                            .With(m => m.Name, $"I9n_{agentName}_{teamName}_Name")
                            .With(m => m.ConnectionDetails, CookConnectionDetailsWithoutProdFor(agentName, teamName))
                            .With(m => m.DeploymentLocation, cloudInstanceId)
                            .With(m => m.SupportedClouds, supportedClouds)
                            .Create();
        }

        private Dictionary<ReleaseState, ConnectionDetail> CookConnectionDetailsFor(string agentName, string teamName)
        {
            return fixture.Build<Dictionary<ReleaseState, ConnectionDetail>>()
                .Do(entity => {
                    entity[ReleaseState.Prod] = fixture.Build<ConnectionDetail>()
                                                        .With(m => m.Protocol, Policies.Current.Protocols.Ids.CosmosDeleteSignalV2)
                                                        .With(m => m.AuthenticationType, AuthenticationType.AadAppBasedAuth)
                                                        .With(m => m.AadAppId, Guid.Empty)
                                                        .With(m => m.MsaSiteId, 0L)
                                                        .With(m => m.ReleaseState, ReleaseState.Prod)
                                                        .Create();
                })
                .Create();
        }

        private Dictionary<ReleaseState, ConnectionDetail> CookConnectionDetailsWithoutProdFor(string agentName, string teamName)
        {
            return fixture.Build<Dictionary<ReleaseState, ConnectionDetail>>()
                .Do(entity => {
                    entity[ReleaseState.PreProd] = fixture.Build<ConnectionDetail>()
                                                        .With(m => m.Protocol, Policies.Current.Protocols.Ids.CosmosDeleteSignalV2)
                                                        .With(m => m.AuthenticationType, AuthenticationType.AadAppBasedAuth)
                                                        .With(m => m.AadAppId, Guid.Empty)
                                                        .With(m => m.MsaSiteId, 0L)
                                                        .With(m => m.ReleaseState, ReleaseState.PreProd)
                                                        .Create();
                })
                .Create();
        }

        private Task<IHttpResult<Collection<DeleteAgent>>> CookHttpResultCollectionTaskFor(Collection<DeleteAgent> dataAgents)
        {
            IHttpResult<Collection<DeleteAgent>> result = new HttpResult<Collection<DeleteAgent>>(
                cookerUtility.CookHttpResultFor("ReadByFiltersAsync"), dataAgents);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<IEnumerable<DeleteAgent>>> CookHttpResultEnumerableTaskFor(IEnumerable<DeleteAgent> dataAgents)
        {
            IHttpResult<IEnumerable<DeleteAgent>> result = new HttpResult<IEnumerable<DeleteAgent>>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), dataAgents);
            return Task.FromResult(result);
        }

        private Task<IHttpResult<DeleteAgent>> CookHttpResultTaskFor(DeleteAgent dataAgent)
        {
            IHttpResult<DeleteAgent> result = new HttpResult<DeleteAgent>(
                cookerUtility.CookHttpResultFor("ReadAllByFiltersAsync"), dataAgent);
            return Task.FromResult(result);
        }
    }
}
