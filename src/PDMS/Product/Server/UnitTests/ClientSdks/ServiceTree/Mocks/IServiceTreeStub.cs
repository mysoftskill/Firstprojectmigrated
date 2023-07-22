namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks
{
    using System.Net.Http;

    public interface IServiceTreeStub
    {
        HttpResponseMessage Execute(string apiName, object[] parameters);
    }
}