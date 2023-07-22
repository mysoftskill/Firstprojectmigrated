
namespace PrivacyCommandCustomActivity
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Newtonsoft.Json;
    using System;

    public class CustomActivityPrivacyCommand
    {
        public string CommandId { get; set; }
     
        public string OperationType { get; set; }
     
        public string Operation { get; set; }

        public int CommandTypeId { get; set; }

        public string CommandProperties { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset StartTimeRange { get; set; }

        public DateTimeOffset EndTimeRange { get; set; }

        public string RowPredicate { get; set; }

        public string Subject { get; set; }

        public string Verifier { get; set; }

        public bool ProcessorApplicable { get; set; }

        public bool ControllerApplicable { get; set; }

        public CustomActivityPrivacyCommand(PrivacyCommandV2 command)
        {
            Operation = command.Operation;
            CommandId = command.CommandId;
            OperationType = command.OperationType;
            CommandTypeId = command.CommandTypeId;
            CommandProperties = JsonConvert.SerializeObject(command.CommandProperties);
            Subject = command.Subject.ToString(Formatting.None);
            Timestamp = command.Timestamp;
            StartTimeRange = command.TimeRangePredicate.StartTime;
            EndTimeRange = command.TimeRangePredicate.EndTime;
            RowPredicate = command.RowPredicate?.ToString(Formatting.None);
            CommandTypeId = command.CommandTypeId;
            ControllerApplicable = command.ProcessorApplicable;
            ProcessorApplicable = command.ControllerApplicable;
        }
    }
}
