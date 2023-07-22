namespace PCF.UnitTests
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using System;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    public abstract class PxsCommandBuilders<T> : TestDataBuilder<T>, INeedDataBuilders where T : PXSV1.PrivacyRequest
    {
    }

    public class DeletePxsCommandBuilder : PxsCommandBuilders<PXSV1.DeleteRequest>
    {
        protected override PXSV1.DeleteRequest CreateNewObject()
        {
            return new PXSV1.DeleteRequest()
            {
                RequestType = PXSV1.RequestType.Delete,
                Subject = new AutoFixtureTestDataBuilder<MsaSubject>().Build(),
                RequestId = Guid.NewGuid(),
                RequestGuid = Guid.NewGuid(),
                Requester = "requester",
                Timestamp = DateTimeOffset.UtcNow,
                Portal = "portal",
                Context = "context",
                CloudInstance = "Public",
                AuthorizationId = string.Empty,
                VerificationToken = string.Empty,
                VerificationTokenV3 = string.Empty,
                CorrelationVector = "correlationVector",
                ProcessorApplicable = true,
                ControllerApplicable = true,
                IsSyntheticRequest = false,
                IsWatchdogRequest = false,
                Predicate = this.APredicate<BrowsingHistoryPredicate>().Build(),
                TimeRangePredicate = this.APredicate<TimeRangePredicate>(),
                PrivacyDataType = "BrowsingHistory",
                IsTestRequest = false
            };
        }
    }

    public class ExportPxsCommandBuilder : PxsCommandBuilders<PXSV1.ExportRequest>
    {
        protected override PXSV1.ExportRequest CreateNewObject()
        {
            return new PXSV1.ExportRequest()
            {
                RequestType = PXSV1.RequestType.Export,
                Subject = new AutoFixtureTestDataBuilder<MsaSubject>().Build(),
                RequestId = Guid.NewGuid(),
                RequestGuid = Guid.NewGuid(),
                Requester = "requester",
                Timestamp = DateTimeOffset.UtcNow,
                Portal = "portal",
                Context = "context",
                CloudInstance = "Public",
                AuthorizationId = string.Empty,
                VerificationToken = string.Empty,
                VerificationTokenV3 = string.Empty,
                CorrelationVector = "correlationVector",
                ProcessorApplicable = true,
                ControllerApplicable = true,
                IsSyntheticRequest = false,
                IsWatchdogRequest = false,
                StorageUri = new Uri("https://abc.com"),
                PrivacyDataTypes = new[] { "BrowsingHistory", "ContentConsumption" },
                IsTestRequest = false
            };
        }
    }

    public class AccountClosePxsCommandBuilder : PxsCommandBuilders<PXSV1.AccountCloseRequest>
    {
        protected override PXSV1.AccountCloseRequest CreateNewObject()
        {
            return new PXSV1.AccountCloseRequest()
            {
                RequestType = PXSV1.RequestType.AccountClose,
                Subject = new AutoFixtureTestDataBuilder<MsaSubject>().Build(),
                RequestId = Guid.NewGuid(),
                RequestGuid = Guid.NewGuid(),
                Requester = "requester",
                Timestamp = DateTimeOffset.UtcNow,
                Portal = "portal",
                Context = "context",
                CloudInstance = "Public",
                AuthorizationId = string.Empty,
                VerificationToken = string.Empty,
                VerificationTokenV3 = string.Empty,
                CorrelationVector = "correlationVector",
                ProcessorApplicable = true,
                ControllerApplicable = true,
                IsSyntheticRequest = false,
                IsWatchdogRequest = false,
                AccountCloseReason = PXSV1.AccountCloseReason.UserAccountClosed,
                IsTestRequest = false
            };
        }
    }

    public class AgeOutPxsCommandBuilder : PxsCommandBuilders<PXSV1.AgeOutRequest>
    {
        protected override PXSV1.AgeOutRequest CreateNewObject()
        {
            return new PXSV1.AgeOutRequest()
            {
                RequestType = PXSV1.RequestType.AgeOut,
                Subject = new AutoFixtureTestDataBuilder<MsaSubject>().Build(),
                RequestId = Guid.NewGuid(),
                RequestGuid = Guid.NewGuid(),
                Requester = "requester",
                Timestamp = DateTimeOffset.UtcNow,
                Portal = "portal",
                Context = "context",
                CloudInstance = "Public",
                AuthorizationId = string.Empty,
                VerificationToken = string.Empty,
                VerificationTokenV3 = string.Empty,
                CorrelationVector = "correlationVector",
                ProcessorApplicable = true,
                ControllerApplicable = true,
                IsSyntheticRequest = false,
                IsWatchdogRequest = false,
                IsTestRequest = false,
                LastActive = DateTimeOffset.UtcNow,
                IsSuspended = false
            };
        }
    }
}
