namespace Microsoft.PrivacyServices.DataManagement.Client.Models.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Testing;

    using Newtonsoft.Json;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Kernel;
    using Xunit;

    public class DataAgentSerializationTest 
    {
        [Theory(DisplayName = "When serializing a dataAgent, then write the odata type as the first property.")]
        [InlineAutoMoqData(typeof(DeleteAgent), "#v2.DeleteAgent")]
        public void When_Serializing_Then_WriteODataTypeFirst(Type type, string expectedType, IFixture fixture)
        {
            fixture.DisableRecursionCheck();
            fixture.Customizations.Add(new TypeRelay(typeof(DataAgent), typeof(DeleteAgent)));
            var agent = new SpecimenContext(fixture).Resolve(type);
            var json = JsonConvert.SerializeObject(agent);
            
            Assert.StartsWith($"{{\"@odata.type\":\"{expectedType}\"", json);
        }

        [Theory(DisplayName = "When serializing a dataAgent, then copy the release state key to the value."), AutoMoqData(true)]
        public void When_Serializing_Then_CopyKeyToValue(IFixture fixture)
        {
            fixture.Customizations.Add(new TypeRelay(typeof(DataAgent), typeof(DeleteAgent)));

            var agent = fixture.Create<DeleteAgent>();
            agent.AssetGroups = null;
            agent.Owner = null;

            foreach (var a in agent.ConnectionDetails)
            {
                a.Value.ReleaseState = (ReleaseState)0;
            }

            var json = JsonConvert.SerializeObject(agent);
            var obj = JsonConvert.DeserializeObject<DeleteAgent>(json);

            foreach (var a in obj.ConnectionDetails)
            {
                var key = a.Key.ToString();
                var value = a.Value.ReleaseState.ToString();
                Assert.Equal(key, value);
                Assert.NotEqual("0", value);
            }
        }
    }
}