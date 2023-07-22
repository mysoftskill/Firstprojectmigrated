namespace Microsoft.PrivacyServices.DataManagement.Client.V2.UnitTest
{
    using System;

    using Client = Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.Testing;

    using Ploeh.AutoFixture;
    using Xunit;

    public class ConflictErrorTest
    {
        [Theory(DisplayName = "When a conflict error without inner error is returned, then parse correctly."), AutoMoqData]
        public void VerifyNullInnerErrorConflictError(Fixture fixture)
        {
            var error = new NullInnerErrorConflictError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a generic conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyGenericConflictError(Fixture fixture)
        {
            var error = new UnknownConflictError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When a generic data agent conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyGenericNestedConflictError(Fixture fixture)
        {
            var error = new UnknownDataAgentConflictError();

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
            }
        }

        [Theory(DisplayName = "When AlreadyExists conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyAlreadyExistsConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.AlreadyExists, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.ConflictError.AlreadyExists e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When AlreadyExists.ClaimedByOwner conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyAlreadyExistsClaimedByOwnerConflictError(Fixture fixture)
        {
            var exn = new AlreadyOwnedException(ConflictType.AlreadyExists_ClaimedByOwner, "message", "target", "value", "owner");

            var error = new EntityConflictError(exn);
            error.ServiceError.InnerError.NestedError = new AlreadyOwnedError(exn.OwnerId);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.AlreadyExists.ClaimedByOwner e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
                Assert.Equal("owner", e.OwnerId);
            }
        }

        [Theory(DisplayName = "When DoesNotExist conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyDoesNotExistConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.DoesNotExist, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.DoesNotExist e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When NullValue conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyNullValueConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.NullValue, "message", "target");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.NullValue e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
            }
        }

        [Theory(DisplayName = "When InvalidValue conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyInvalidValueConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.InvalidValue, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.InvalidValue e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When InvalidValue.BadCombination conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyInvalidValueBadCombinationConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.InvalidValue_BadCombination, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.InvalidValue.BadCombination e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When InvalidValue.Immutable conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyInvalidValueImmutableConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.InvalidValue_Immutable, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.InvalidValue.Immutable e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When InvalidValue.StateTransition conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyInvalidValueStateTransitionConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.InvalidValue_StateTransition, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.InvalidValue.StateTransition e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("value", e.Value);
            }
        }

        [Theory(DisplayName = "When a MaxExpansionSizeExceeded conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyCapabilityMaxExpansionSizeExceededConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.MaxExpansionSizeExceeded, "message", "Target", "Value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (Client.V2.ConflictError.MaxExpansionSizeExceeded e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
                Assert.Equal("Value", e.Value);
            }
        }

        [Theory(DisplayName = "When LinkedEntityExists conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyLinkedEntityExistsConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.LinkedEntityExists, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.LinkedEntityExists e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
            }
        }

        [Theory(DisplayName = "When PendingCommandsExists conflict error is returned, then parse correctly."), AutoMoqData]
        public void VerifyPendingCommandsExistsConflictError(Fixture fixture)
        {
            var exn = new ConflictException(ConflictType.PendingCommandsExists, "message", "target", "value");

            var error = new EntityConflictError(exn);

            try
            {
                BadArgumentErrorTest.CreateHttpResult(fixture, error).Get(2);
            }
            catch (V2.ConflictError.PendingCommandsExists e)
            {
                Assert.Equal(error.ServiceError.ToString(), e.Code);
                Assert.Equal(error.ServiceError.Message, e.Message);
                Assert.Equal(error.ServiceError.Target, e.Target);
            }
        }

        [Serializable]
        public class NullInnerErrorConflictError : ConflictError
        {
            public NullInnerErrorConflictError()
                : base("message", null)
            {
            }
        }

        [Serializable]
        public class UnknownConflictError : ConflictError
        {
            public UnknownConflictError()
                : base("message", new StandardInnerError("Unknown"))
            {
            }
        }

        [Serializable]
        public class UnknownDataAgentConflictError : ConflictError
        {
            public UnknownDataAgentConflictError()
                : base("message", new StandardInnerError("DataAgent", new StandardInnerError("Unknown")))
            {
            }
        }
    }
}