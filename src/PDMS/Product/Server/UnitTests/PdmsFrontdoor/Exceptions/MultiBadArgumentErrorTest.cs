namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.UnitTest
{
    using Xunit;

    public class MultiBadArgumentErrorTest
    {
        [Fact(DisplayName = "Verify MultiBadArgumentError fields.")]
        public void VerifyMultiBadArgumentError()
        {
            BadArgumentError error1 = new NullArgumentError(string.Empty);
            BadArgumentError error2 = new InvalidArgumentError(string.Empty, string.Empty);

            var error = new MultiBadArgumentError(new[] { error1, error2 });
            Assert.Equal("BadArgument", error.ServiceError.Code);
            Assert.Equal("NullValue", error.ServiceError.Details[0].Code);
            Assert.Equal("InvalidValue", error.ServiceError.Details[1].Code);
        }
    }
}
