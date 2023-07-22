namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class NotFoundErrorTest
    {
        [Theory(DisplayName = "When a not found error without inner error is returned, then parse correctly."), AutoMoqData]
        public void VerifyNullInnerErrorNotFoundError(Fixture fixture)
        {
            var error = new NullInnerErrorNotFoundError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a generic not found error is returned, then parse correctly."), AutoMoqData]
        public void VerifyGenericNotFoundError(Fixture fixture)
        {
            var error = new UnknownNotFoundError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When an entity not found error is returned, then parse correctly."), AutoMoqData]
        public void VerifyEntityNotFoundError(Fixture fixture, Guid id)
        {
            var error = new EntityNotFoundError(id, "Test");

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.Entity e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(id, e.Id);
                Assert.Equal("Test", e.Type);
            }
        }

        [Theory(DisplayName = "Verify Entity.ToString contains all properties."), AutoMoqData]
        public void VerifyEntityNotFoundError_ToString(Fixture fixture, Guid id)
        {
            try
            {
                var error = new EntityNotFoundError(id, "Test");
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.Entity e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"id\"", asString);
                Assert.Contains($"\"type\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When a service tree not found error is returned, then parse correctly."), AutoMoqData]
        public void VerifyServiceTreeNotFoundError(Fixture fixture, Guid id)
        {
            var error = new ServiceTreeNotFoundError(id);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.ServiceTree e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(id, e.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceTree.ToString contains all properties."), AutoMoqData]
        public void VerifyServiceTreeNotFoundError_ToString(Fixture fixture, Guid id)
        {
            try
            {
                var error = new ServiceTreeNotFoundError(id);
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.ServiceTree e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"id\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When a service tree service not found error is returned, then parse correctly."), AutoMoqData]
        public void VerifyServiceTreeNotFoundError_Service(Fixture fixture, Guid id)
        {
            var error = new ServiceTreeNotFoundError(id);
            error.ServiceError.InnerError.NestedError = new ServiceNotFoundError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.ServiceTree.Service e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(id, e.Id);
            }
        }

        [Theory(DisplayName = "Verify ServiceTree.Service.ToString contains all properties."), AutoMoqData]
        public void VerifyServiceTreeNotFoundError_Service_ToString(Fixture fixture, Guid id)
        {
            try
            {
                var error = new ServiceTreeNotFoundError(id);
                error.ServiceError.InnerError.NestedError = new ServiceNotFoundError();
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.ServiceTree.Service e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"id\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When a security group not found error is returned, then parse correctly."), AutoMoqData]
        public void VerifySecurityGroupNotFoundError(Fixture fixture, Guid id)
        {
            var error = new SecurityGroupNotFoundError(id);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.SecurityGroup e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(id, e.Id);
            }
        }

        [Theory(DisplayName = "Verify SecurityGroup.ToString contains all properties."), AutoMoqData]
        public void VerifySecurityGroupNotFoundError_ToString(Fixture fixture, Guid id)
        {
            try
            {
                var error = new SecurityGroupNotFoundError(id);
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotFoundError.SecurityGroup e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"id\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Serializable]
        public class NullInnerErrorNotFoundError : NotFoundError
        {
            public NullInnerErrorNotFoundError()
                : base("message", null)
            {
            }
        }

        [Serializable]
        public class UnknownNotFoundError : NotFoundError
        {
            public UnknownNotFoundError()
                : base("message", new StandardInnerError("Unknown"))
            {
            }
        }
    }
}