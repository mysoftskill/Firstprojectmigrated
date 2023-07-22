namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation.UnitTest
{
    using System;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class ExceptionExtensionsTest
    {
        [Theory(DisplayName = "When Exception.GetName is called, then return the full name."), AutoMoqData]
        public void When_ExceptionGetNameCalled_Then_ReturnFullName(Exception exn)
        {
            Assert.Equal("System.Exception", exn.GetName());
        }

        [Theory(DisplayName = "When ArgumentNullException.GetName is called, then return the full name."), AutoMoqData]
        public void When_ArgumentNullExceptionGetNameCalled_Then_ReturnFullName(ArgumentNullException exn)
        {
            Assert.Equal("System.ArgumentNullException", exn.GetName());
        }

        [Fact(DisplayName = "When GetName is called for null, then return null.")]
        public void When_GetNameCalledForNull_Then_ReturnNull()
        {
            Assert.Equal("null", ((Exception)null).GetName());
        }
    }
}
