using Microsoft.PrivacyServices.UX.Utilities;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.I9n.Cookers
{
    public class MockCookerAccessor
    {
        private readonly IFixture fixture;

        public DataOwnerMockCooker DataOwnerMockCooker { get; }
        public DataAssetMockCooker DataAssetMockCooker { get; }
        public DataAgentMockCooker DataAgentMockCooker { get; }
        public ServiceTreeMockCooker ServiceTreeMockCooker { get; }
        public ManualRequestMockCooker ManualRequestMockCooker { get; }

        public MockCookerAccessor()
        {
            fixture = new Fixture().Initialize();

            DataOwnerMockCooker = new DataOwnerMockCooker(fixture);
            DataAssetMockCooker = new DataAssetMockCooker(fixture);
            DataAgentMockCooker = new DataAgentMockCooker(fixture);
            ServiceTreeMockCooker = new ServiceTreeMockCooker(fixture);
            ManualRequestMockCooker = new ManualRequestMockCooker(fixture);
        }
    }
}
