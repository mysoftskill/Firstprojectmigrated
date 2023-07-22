namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using System;
    using System.Web.Http.ModelBinding;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class InvalidModelErrorTest
    {
        [Theory(DisplayName = "Handle nulls in ModelState errors."), AutoMoqData]
        public void When_NoErrors_Then_ThrowGenericMessage(ModelStateDictionary state)
        {
            var error = new InvalidModelError(state);
            Assert.Equal("Invalid model state.", error.ServiceError.Message);
        }

        [Theory(DisplayName = "Extract the error message from the ModelState errors."), AutoMoqData]
        public void When_Error_Then_ExtractMessage(ModelStateDictionary state)
        {
            var m = new ModelState();
            m.Errors.Add(new Exception("Message"));
            state.Add("t", m);

            var error = new InvalidModelError(state);
            Assert.Equal("Message", error.ServiceError.Message);
        }
    }
}