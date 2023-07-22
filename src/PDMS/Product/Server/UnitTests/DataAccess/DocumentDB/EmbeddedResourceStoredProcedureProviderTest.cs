namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System.Linq;

    using Xunit;

    public class EmbeddedResourceStoredProcedureProviderTest
    {
        [Fact(DisplayName = "Verify loading stored procedure installation data from the embedded resources.")]
        public void VeifyStoredProcedureProviderData()
        {
            var provider = new EmbeddedResourceStoredProcedureProvider(
                "Microsoft.PrivacyServices.DataManagement.UnitTests.DataAccess.DocumentDB.StoredProcedures",
                "Installation.xml",
                typeof(EmbeddedResourceStoredProcedureProviderTest).Assembly);

            var sprocs = provider.GetStoredProcedures();
            
            // There should always be at least 1 sproc.
            Assert.Equal(2, sprocs.Count());

            // File extension should be removed.
            Assert.All(sprocs, s => Assert.False(s.Name.EndsWith(".js")));

            // Install actions should contain data.
            var sproc = sprocs.First(s => s.Name == "Test1");
            Assert.Equal("Test sproc 1", sproc.Value);
            Assert.Equal(StoredProcedure.Actions.Install, sproc.Action);

            // Remove actions should not contain data.
            sproc = sprocs.First(s => s.Name == "Test0");
            Assert.Null(sproc.Value);
            Assert.Equal(StoredProcedure.Actions.Remove, sproc.Action);
        }
    }
}