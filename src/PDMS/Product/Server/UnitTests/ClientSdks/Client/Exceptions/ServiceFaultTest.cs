namespace Microsoft.PrivacyServices.DataManagement.Client.Exceptions.UnitTest
{
    using System;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class ServiceFaultTest
    {
        [Theory(DisplayName = "When a service fault is returned, then parse correctly."), AutoMoqData]
        public void VerifyServiceFault(Fixture fixture, string message1, string message2)
        {
            var exn = new Exception(message1, new Exception(message2));
            var error = new Frontdoor.Exceptions.ServiceFault(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.ServiceFault e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
                Assert.Equal(e.InnerException.Message, message1);
                Assert.Equal(e.InnerException.InnerException.Message, message2);
            }
        }
    }
}