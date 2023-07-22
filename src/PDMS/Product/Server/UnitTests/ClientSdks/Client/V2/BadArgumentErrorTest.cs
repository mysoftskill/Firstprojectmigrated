namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Newtonsoft.Json;

    using Ploeh.AutoFixture;
    using Xunit;

    public class BadArgumentErrorTest
    {
        [Theory(DisplayName = "When a bad argument error without inner error is returned, then parse correctly."), AutoMoqData]
        public void VerifyNullInnerErrorBadArgumentError(Fixture fixture)
        {
            var error = new NullInnerErrorBadArgumentError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a generic bad argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyGenericBadArgumentError(Fixture fixture)
        {
            var error = new UnknownBadArgumentError();

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
            }
        }

        [Theory(DisplayName = "When a multiple bad argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBadArgumentError_Multiple(Fixture fixture, string param1, string value1, string param2)
        {
            BadArgumentError exn1 = new InvalidArgumentError(param1, value1);
            BadArgumentError exn2 = new NullArgumentError(param2);
            var error = new MultiBadArgumentError(new[] { exn1, exn2 });

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.BadArgumentError.Multiple e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);

                Func<Detail, Action<Client.ResponseError.Detail>> compare = expected =>
                {
                    return actual =>
                    {
                        Assert.Equal(expected.Code, actual.Code);
                        Assert.Equal(expected.Message, actual.Message);
                        Assert.Equal(expected.Target, actual.Target);
                    };
                };

                Assert.Collection(e.Details, compare(exn1.ToDetail()), compare(exn2.ToDetail()));
            }
        }

        [Theory(DisplayName = "Verify Multiple.ToString contains all properties."), AutoMoqData]
        public void VerifyBadArgumentError_Multiple_ToString(Fixture fixture, string param1, string value1, string param2)
        {
            try
            {
                BadArgumentError exn1 = new InvalidArgumentError(param1, value1);
                BadArgumentError exn2 = new NullArgumentError(param2);
                var error = new MultiBadArgumentError(new[] { exn1, exn2 });
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.Multiple e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"details\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When an invalid argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument(Fixture fixture, string param, string value)
        {
            var error = new InvalidArgumentError(param, value);

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
                Assert.Equal(e.Target, error.ServiceError.Target);
                Assert.Equal(e.Value, value);
            }
        }

        [Theory(DisplayName = "Verify InvalidArgument.ToString contains all properties."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument_ToString(Fixture fixture, string param, string value)
        {
            try
            {
                var error = new InvalidArgumentError(param, value);
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"target\"", asString);
                Assert.Contains($"\"value\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When an invalid argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument_UnsupportedCharacter(Fixture fixture, string param, string value)
        {
            var error = new InvalidArgumentError(param, value);
            error.ServiceError.InnerError.NestedError = new UnsupportedCharacterError();

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument.UnsupportedCharacter e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
                Assert.Equal(e.Target, error.ServiceError.Target);
                Assert.Equal(e.Value, value);
            }
        }

        [Theory(DisplayName = "Verify InvalidArgument.UnsupportedCharacter.ToString contains all properties."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument_UnsupportedCharacter_ToString(Fixture fixture, string param, string value)
        {
            try
            {
                var error = new InvalidArgumentError(param, value);
                error.ServiceError.InnerError.NestedError = new UnsupportedCharacterError();

                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument.UnsupportedCharacter e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"target\"", asString);
                Assert.Contains($"\"value\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When an mutually exclusive invalid argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument_MutuallyExclusive(Fixture fixture, string param, string value)
        {
            var error = new InvalidArgumentError(param, value);
            error.ServiceError.InnerError.NestedError = new MutuallyExclusiveError("serviceTree");

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument.MutuallyExclusive e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
                Assert.Equal(e.Target, error.ServiceError.Target);
                Assert.Equal(e.Value, value);
                Assert.Equal("serviceTree", e.Source);
            }
        }

        [Theory(DisplayName = "Verify InvalidArgument.MutuallyExclusive.ToString contains all properties."), AutoMoqData]
        public void VerifyBadArgumentError_InvalidArgument_MutuallyExclusive_ToString(Fixture fixture, string param, string value)
        {
            try
            {
                var error = new InvalidArgumentError(param, value);
                error.ServiceError.InnerError.NestedError = new MutuallyExclusiveError("serviceTree");

                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.InvalidArgument.MutuallyExclusive e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"target\"", asString);
                Assert.Contains($"\"value\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        [Theory(DisplayName = "When a null argument error is returned, then parse correctly."), AutoMoqData]
        public void VerifyBadArgumentError_NullArgument(Fixture fixture, string param)
        {
            var error = new NullArgumentError(param);

            try
            {
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.NullArgument e)
            {
                Assert.Equal(e.Code, error.ServiceError.ToString());
                Assert.Equal(e.Message, error.ServiceError.Message);
                Assert.Equal(e.Target, error.ServiceError.Target);
            }
        }

        [Theory(DisplayName = "Verify NullArgument.ToString contains all properties."), AutoMoqData]
        public void VerifyBadArgumentError_NullArgument_ToString(Fixture fixture, string param)
        {
            try
            {
                var error = new NullArgumentError(param);
                CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.BadArgumentError.NullArgument e)
            {
                var asString = e.ToString();
                Assert.Contains($"\"code\"", asString);
                Assert.Contains($"\"message\"", asString);
                Assert.Contains($"\"target\"", asString);
                Assert.Contains($"\"source\"", asString);
                Assert.Contains($"\"stackTrace\"", asString);
            }
        }

        internal static HttpResult CreateHttpResult(Fixture fixture, ServiceException exn)
        {
            var json = JsonConvert.SerializeObject(new ResponseError { Error = exn.ServiceError }, SerializerSettings.Instance);

            return 
                new HttpResult(
                    exn.StatusCode, 
                    json, 
                    fixture.Create<HttpHeaders>(), 
                    fixture.Create<HttpMethod>(), 
                    fixture.Create<string>(), 
                    fixture.Create<string>(), 
                    fixture.Create<long>(),
                    fixture.Create<string>());
        }

        [Serializable]
        public class NullInnerErrorBadArgumentError : BadArgumentError
        {
            public NullInnerErrorBadArgumentError()
                : base("target", "message", null)
            {
            }

            public override Detail ToDetail()
            {
                throw new NotImplementedException();
            }
        }

        [Serializable]
        public class UnknownBadArgumentError : BadArgumentError
        {
            public UnknownBadArgumentError()
                : base("target", "message", new StandardInnerError("Unknown"))
            {
            }

            public override Detail ToDetail()
            {
                throw new NotImplementedException();
            }
        }
    }
}