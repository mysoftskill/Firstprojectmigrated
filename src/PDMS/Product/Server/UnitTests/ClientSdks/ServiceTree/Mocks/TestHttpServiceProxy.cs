namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree.UnitTest.Mocks
{
    using Microsoft.PrivacyServices.DataManagement.Client;

    public class TestHttpServiceProxy : BaseHttpServiceProxy
    {
        public TestHttpServiceProxy(Owin.Testing.TestServer testServer)
            : base(testServer.HttpClient)
        {
        }
    }
}