namespace PCF.UnitTests
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests the status codes from the audit log receiver. Note: we don't test the formatting here, since that's done elsewhere. We're just ensuring that the right status codes get put
    /// in the right places.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class AuditLogReceiverTests : INeedDataBuilders
    {
        [Fact]
        public void AuditLogReceiver_HandlesComplete_Delete()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.HardDelete, PrivacyCommandType.Delete);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_Delete_IgnoredByVariant()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.IgnoredByVariant, PrivacyCommandType.Delete, ignoredByVariant: true);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_Export()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.ExportComplete, PrivacyCommandType.Export);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_Export_ForceCompletedFromDashboard()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.ExportFailedByManualComplete, PrivacyCommandType.Export, forceCompleteReason: ForceCompleteReasonCode.ForceCompleteFromPartnerTestPage);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_Export_ForceCompletedExpired()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.ExportFailedByAutoComplete, PrivacyCommandType.Export, forceCompleteReason: ForceCompleteReasonCode.ForceCompleteFromAgeoutTimer);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_AgeOut()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.AgeOut);
        }

        [Fact]
        public void AuditLogReceiver_HandlesComplete_AccountClose()
        {
            this.Complete_TestSingleLine(AuditLogCommandAction.HardDelete, PrivacyCommandType.AccountClose);
        }

        [Fact]
        public void AuditLogReceiver_HandlesStarted_Delete()
        {
            this.Started_TestSingleLine(AuditLogCommandAction.DeleteStart, PrivacyCommandType.Delete);
        }

        [Fact]
        public void AuditLogReceiver_HandlesStarted_Export()
        {
            this.Started_TestSingleLine(AuditLogCommandAction.ExportStart, PrivacyCommandType.Export);
        }

        [Fact]
        public void AuditLogReceiver_HandlesStarted_AgeOut()
        {
            this.Started_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.AgeOut);
        }

        [Fact]
        public void AuditLogReceiver_HandlesStarted_AccountClose()
        {
            this.Started_TestSingleLine(AuditLogCommandAction.DeleteStart, PrivacyCommandType.AccountClose);
        }

        [Fact]
        public void AuditLogReceiver_HandlesSoftDelete_Delete()
        {
            this.SoftDelete_TestSingleLine(AuditLogCommandAction.SoftDelete, PrivacyCommandType.Delete);
        }

        [Fact]
        public void AuditLogReceiver_HandlesSoftDelete_Export()
        {
            this.SoftDelete_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.Export);
        }

        [Fact]
        public void AuditLogReceiver_HandlesSoftDelete_AgeOut()
        {
            this.SoftDelete_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.AgeOut);
        }

        [Fact]
        public void AuditLogReceiver_HandlesSoftDelete_AccountClose()
        {
            this.SoftDelete_TestSingleLine(AuditLogCommandAction.SoftDelete, PrivacyCommandType.AccountClose);
        }

        [Fact]
        public void AuditLogReceiver_HandlesDropped_Delete()
        {
            this.Dropped_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.Delete);
        }

        [Fact]
        public void AuditLogReceiver_HandlesDropped_Export()
        {
            this.Dropped_TestSingleLine(AuditLogCommandAction.NotApplicable, PrivacyCommandType.Export);
        }

        [Fact]
        public void AuditLogReceiver_HandlesDropped_AgeOut()
        {
            this.Dropped_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.AgeOut);
        }

        [Fact]
        public void AuditLogReceiver_HandlesDropped_AccountClose()
        {
            this.Dropped_TestSingleLine(AuditLogCommandAction.None, PrivacyCommandType.AccountClose);
        }

        private void Complete_TestSingleLine(AuditLogCommandAction action, PrivacyCommandType type, bool ignoredByVariant = false, ForceCompleteReasonCode? forceCompleteReason = null)
        {
            var receiver = new AuditLogReceiver();
            var @event = this.ACompletedEvent()
                    .WithValue(ev => ev.CommandType, type)
                    .WithValue(ev => ev.AffectedRows, 1)
                    .WithValue(ev => ev.IgnoredByVariant, ignoredByVariant)
                    .WithValue(ev => ev.ForceCompleteReasonCode, forceCompleteReason).Build();

            @event.AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandCompletedAuditLogAction(@event);

            receiver.Process(@event);

            if (action == AuditLogCommandAction.None)
            {
                Assert.Empty(receiver.Lines);
            }
            else
            {
                Assert.Single(receiver.Lines);
                Assert.Contains(action.ToString(), receiver.Lines.First());
            }
        }
        
        private void SoftDelete_TestSingleLine(AuditLogCommandAction action, PrivacyCommandType type)
        {
            var receiver = new AuditLogReceiver();
            var @event = this.ASoftDeletedEvent().WithValue(ev => ev.CommandType, type).Build();
            @event.AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandSoftDeleteAuditLogAction(@event.CommandType);
            receiver.Process(@event);

            if (action == AuditLogCommandAction.None)
            {
                Assert.Empty(receiver.Lines);
            }
            else
            {
                Assert.Single(receiver.Lines);
                Assert.Contains(action.ToString(), receiver.Lines.First());
            }
        }

        private void Started_TestSingleLine(AuditLogCommandAction action, PrivacyCommandType type)
        {
            var receiver = new AuditLogReceiver();
            var @event = this.AStartedEvent().WithValue(ev => ev.CommandType, type).Build();
            @event.AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandStartedAuditLogAction(@event.CommandType);
            receiver.Process(@event);

            if (action == AuditLogCommandAction.None)
            {
                Assert.Empty(receiver.Lines);
            }
            else
            {
                Assert.Single(receiver.Lines);
                Assert.Contains(action.ToString(), receiver.Lines.First());
            }
        }

        private void Dropped_TestSingleLine(AuditLogCommandAction action, PrivacyCommandType type)
        {
            var receiver = new AuditLogReceiver();
            var @event = this.ADroppedEvent().WithValue(ev => ev.CommandType, type).Build();
            @event.AuditLogCommandAction = PrivacyCommandTypeToAuditLogActionMapper.GetCommandDroppedAuditLogAction(@event.CommandType);
            receiver.Process(@event);

            if (action == AuditLogCommandAction.None)
            {
                Assert.Empty(receiver.Lines);
            }
            else
            {
                Assert.Single(receiver.Lines);
                Assert.Contains(action.ToString(), receiver.Lines.First());
            }
        }
    }
}
