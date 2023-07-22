namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class NotAuthorizedErrorTest
    {
        [Theory(DisplayName = "When a base not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBaseNotAuthorizedError(Fixture fixture, string message)
        {
            var error = new NotAuthorizedError(message);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a generic not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyGenericNotAuthorizedError(Fixture fixture)
        {
            var error = new UnknownNotAuthorizedError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a user not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyUserNotAuthorizedError(Fixture fixture, string userName)
        {
            var error = new UserNotAuthorizedError(userName, "missingRole", "message");

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(userName, e.UserName);
                Assert.Equal("missingRole", e.Role);
            }
        }

        [Theory(DisplayName = "Verify User.ToString contains all properties."), AutoMoqData]
        public void VerifyUserNotAuthorizedError_ToString(Fixture fixture, string userName)
        {
            try
            {
                var error = new UserNotAuthorizedError(userName, "missingRole", "message");
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
                Assert.Contains($"\"userName\"", asString);
                Assert.Contains($"\"role\"", asString);
            }
        }

        [Theory(DisplayName = "When a user service tree not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyUserNotAuthorizedError_ServiceTree(Fixture fixture, string userName, Guid serviceId)
        {
            var error = new UserNotAuthorizedError(userName, "missingRole", "message");
            error.ServiceError.InnerError.NestedError = new ServiceTreeNotAuthorizedError(serviceId.ToString());

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User.ServiceTree e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(userName, e.UserName);
                Assert.Equal(serviceId.ToString(), e.ServiceId);
                Assert.Equal("missingRole", e.Role);
            }
        }

        [Theory(DisplayName = "Verify User.ServiceTree.ToString contains all properties."), AutoMoqData]
        public void VerifyUserNotAuthorizedError_ServiceTree_ToString(Fixture fixture, string userName, Guid serviceId)
        {
            try
            {
                var error = new UserNotAuthorizedError(userName, "missingRole", "message");
                error.ServiceError.InnerError.NestedError = new ServiceTreeNotAuthorizedError(serviceId.ToString());
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User.ServiceTree e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
                Assert.Contains($"\"userName\"", asString);
                Assert.Contains($"\"serviceId\"", asString);
                Assert.Contains($"\"role\"", asString);
            }
        }

        [Theory(DisplayName = "When a user security group not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyUserNotAuthorizedError_SecurityGroup(Fixture fixture, string userName)
        {
            var error = new UserNotAuthorizedError(userName, "missingRole", "message");
            error.ServiceError.InnerError.NestedError = new SecurityGroupNotAuthorizedError("securityGroup1;securityGroup2");

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User.SecurityGroup e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(userName, e.UserName);
                Assert.Equal("securityGroup1;securityGroup2", e.SecurityGroups);
                Assert.Equal("missingRole", e.Role);
            }
        }

        [Theory(DisplayName = "Verify User.SecurityGroup.ToString contains all properties."), AutoMoqData]
        public void VerifyUserNotAuthorizedError_SecurityGroup_ToString(Fixture fixture, string userName)
        {
            try
            {
                var error = new UserNotAuthorizedError(userName, "missingRole", "message");
                error.ServiceError.InnerError.NestedError = new SecurityGroupNotAuthorizedError("securityGroup1;securityGroup2");
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.User.SecurityGroup e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
                Assert.Contains($"\"userName\"", asString);
                Assert.Contains($"\"securityGroups\"", asString);
                Assert.Contains($"\"role\"", asString);
            }
        }

        [Theory(DisplayName = "When an application not authorized error is returned, then parse correctly."), AutoMoqData]
        public void VerifyApplicationNotAuthorizedError(Fixture fixture, string applicationId)
        {
            var error = new ApplicationNotAuthorizedError(applicationId);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.Application e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(applicationId, e.ApplicationId);
            }
        }

        [Theory(DisplayName = "Verify Application.ToString contains all properties."), AutoMoqData]
        public void VerifyApplicationNotAuthorizedError_ToString(Fixture fixture, string applicationId)
        {
            try
            {
                var error = new ApplicationNotAuthorizedError(applicationId);
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.NotAuthorizedError.Application e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
                Assert.Contains($"\"applicationId\"", asString);
            }
        }

        [Serializable]
        public class UnknownNotAuthorizedError : NotAuthorizedError
        {
            public UnknownNotAuthorizedError()
                : base("message", new StandardInnerError("Unknown"))
            {
            }
        }
    }
}