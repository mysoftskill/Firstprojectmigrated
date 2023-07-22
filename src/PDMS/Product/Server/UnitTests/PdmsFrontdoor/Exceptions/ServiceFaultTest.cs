namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System;
    using System.Net;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class ServiceFaultTest
    {
        [Theory(DisplayName = "Verify all ServiceFault fields."), AutoMoqData]
        public void VerifyServiceFault(string rootMessage, InvalidOperationException exn)
        {
            var rootExn = new Exception(rootMessage, exn);

            // Verify root values.
            var serviceFault = new ServiceFault(rootExn);            
            Assert.Equal(HttpStatusCode.InternalServerError, serviceFault.StatusCode);
            Assert.Equal("ServiceFault", serviceFault.ServiceError.Code);

            // Verify root exception.
            var innerError = serviceFault.ServiceError.InnerError as ServiceFault.ExceptionError;
            Assert.Equal("System.Exception", innerError.Code);
            Assert.Equal(rootExn.StackTrace, innerError.StackTrace);
            Assert.Equal(rootExn.Message, innerError.Message);

            // Verify flattened value.
            Assert.Equal("ServiceFault:System.Exception", serviceFault.ServiceError.ToString());

            // Verify inner exception.
            innerError = innerError.InnerException as ServiceFault.ExceptionError;
            Assert.Equal("System.InvalidOperationException", innerError.Code);
            Assert.Null(innerError.StackTrace);
            Assert.Equal(exn.Message, innerError.Message);
        }

        [Fact]
        public void VerifyServiceFaultErroMessageHtmlEncoded()
        {
            var rootExn = new Exception("Exception <script> alert() <\\script>.");

            // Verify root values.
            var serviceFault = new ServiceFault(rootExn);
            Assert.Equal(HttpStatusCode.InternalServerError, serviceFault.StatusCode);
            Assert.Equal("ServiceFault", serviceFault.ServiceError.Code);

            // Verify root exception.
            var innerError = serviceFault.ServiceError.InnerError as ServiceFault.ExceptionError;
            Assert.Equal("System.Exception", innerError.Code);
            Assert.Equal("Exception &lt;script&gt; alert() &lt;\\script&gt;.", innerError.Message);

            // Verify flattened value.
            Assert.Equal("ServiceFault:System.Exception", serviceFault.ServiceError.ToString());            
        }
    }
}
