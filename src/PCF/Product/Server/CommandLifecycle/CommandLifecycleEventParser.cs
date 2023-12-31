namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// Parses event hub events into Command Lifecycle events.
    /// </summary>
    internal static class CommandLifecycleEventParser
    {
        // Special event name that we just use to keep the event hub sludge moving and make sure each
        // partition gets poked periodically so that checkpoints happen on schedule.
        internal const string NoopEventName = "noop";
        internal const string LegacyAuditEventName = "CommandIngestionAudit";

        // Special header in each event indicating which type of event it is.
        internal const string EventNameProperty = "TypeName";
        internal const string CompressionAlgorithmProperty = "CompressionAlgorithm";

        internal const string BulkEventName = "$bulk$";
        internal static int MaxBulkMessageSize => CommandLifecycleEventPublisher.MaxPublishSizeBytes;

        // Use flighting manager to discover whether uncompression at publisher is enabled or not
        public static bool IsCompressionDisabled => FlightingUtilities.IsEnabled(FeatureNames.PCF.PublishUncompressedMessage, false);

        // Constant representing none compression algorithm is applied
        internal const string NoneCompression = "None";

        /// <summary>
        /// Serializes the set of lifecycle events into EventHub event data objects.
        /// </summary>
        public static IEnumerable<JsonEventData> Serialize(IEnumerable<CommandLifecycleEvent> events)
        {
            return SerializeToBulkEvent(events);
        }

        /// <summary>
        /// Parses the JsonEventData object into one or more lifecycle events.
        /// </summary>
        public static IEnumerable<CommandLifecycleEvent> ParseEvents(JsonEventData data)
        {
            if (data.Properties[EventNameProperty] == BulkEventName)
            {
                data.Properties.TryGetValue(CompressionAlgorithmProperty, out string compressionAlgorithm);
                Dictionary<string, List<JObject>> bulkEvent;

                if (compressionAlgorithm == CompressionTools.Brotli.Name)
                {
                    bulkEvent = CompressionTools.Brotli.DecompressJson<Dictionary<string, List<JObject>>>(data.Data);
                }
                else
                {
                    // PCF currently compresses messages when writing to EventHub. This is causing problems for running EventHub Capture.
                    // Hence removing compression logic. 
                    bulkEvent = JsonConvert.DeserializeObject<Dictionary<string, List<JObject>>>(Encoding.UTF8.GetString(data.Data));
                }
                return ParseBulkEvent(bulkEvent);
            }
            else
            {
                JObject jsonEvent = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(data.Data));
                return new[] { ParseEvent(data.Properties[EventNameProperty], jsonEvent) };
            }
        }

        /// <summary>
        /// Parse bulk event
        /// </summary>
        /// <param name="bulkEvent">Bulk event to be parsed.</param>
        /// <returns></returns>
        private static IEnumerable<CommandLifecycleEvent> ParseBulkEvent(Dictionary<string, List<JObject>> bulkEvent)
        {
            List<CommandLifecycleEvent> events = new List<CommandLifecycleEvent>();
            foreach (var item in bulkEvent)
            {
                string name = item.Key;
                foreach (var rawEvent in item.Value)
                {
                    events.Add(ParseEvent(name, rawEvent));
                }
            }

            return events;
        }

        private static IEnumerable<JsonEventData> SerializeToBulkEvent(IEnumerable<CommandLifecycleEvent> events)
        {
            List<JsonEventData> results = new List<JsonEventData>();

            int i = 0;
            List<(string, JObject)> serializedEvents = new List<(string, JObject)>();

            foreach (var item in events)
            {
                serializedEvents.Add((item.EventName, JObject.FromObject(item)));
                i++;

                if (i >= 50)
                {
                    results.AddRange(BuildBulkEventsRecursive(serializedEvents, 0, serializedEvents.Count));
                    i = 0;
                    serializedEvents.Clear();
                }
            }

            // Take care of any remainder now.
            results.AddRange(BuildBulkEventsRecursive(serializedEvents, 0, serializedEvents.Count));

            return results;
        }

        internal static IEnumerable<JsonEventData> BuildBulkEventsRecursive(List<(string eventName, JObject data)> items, int startIndex, int length)
        {
            var bulkEvent = new Dictionary<string, List<JObject>>();
            byte[] messageContent;

            for (int i = 0; i < length; ++i)
            {
                var item = items[i + startIndex];

                if (!bulkEvent.TryGetValue(item.eventName, out List<JObject> value))
                {
                    value = new List<JObject>();
                    bulkEvent[item.eventName] = value;
                }

                value.Add(item.data);
            }

            if (IsCompressionDisabled)
            {
                messageContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(bulkEvent));
            }
            else
            {
                messageContent = CompressionTools.Brotli.CompressJson(bulkEvent);
            }

            // If the data is bigger than 500k, then we should split in half.
            // Removing queue limit from consideration while publishing it, and Event Hub has a limit
            // of 1MB.
            if (messageContent.Length > MaxBulkMessageSize)
            {
                if (length == 1)
                {
                    // log the event details
                    DeserializeAndLogBulkEventInfo(messageContent);
                    throw new InvalidOperationException("Single event is too big :(");
                }

                int leftStart = startIndex;
                int leftLength = length / 2;

                int rightStart = startIndex + leftLength;
                int rightLength = length - leftLength;

                var left = BuildBulkEventsRecursive(items, leftStart, leftLength);
                var right = BuildBulkEventsRecursive(items, rightStart, rightLength);

                return left.Concat(right);
            }

            return new[]
            {
                new JsonEventData
                {
                    Data = messageContent,
                    Properties =
                    {
                        [EventNameProperty] = BulkEventName,
                        [CompressionAlgorithmProperty] = IsCompressionDisabled ? NoneCompression : CompressionTools.Brotli.Name,
                    }
                }
            };
        }

        /// <summary>
        /// Deserialize bulk event and log event details
        /// </summary>
        /// <param name="messageContent">Bulk message bytes.</param>
        private static void DeserializeAndLogBulkEventInfo(byte[] messageContent)
        {
            try
            {
                Dictionary<string, List<JObject>> items;
                StringBuilder eventDetails = new StringBuilder("[");

                if (IsCompressionDisabled)
                {
                    items = JsonConvert.DeserializeObject<Dictionary<string, List<JObject>>>(Encoding.UTF8.GetString(messageContent));
                }
                else
                {
                    items = CompressionTools.Brotli.DecompressJson<Dictionary<string, List<JObject>>>(messageContent);
                }

                IEnumerable<CommandLifecycleEvent> lifecycleEvents = ParseBulkEvent(items);
                foreach (var item in lifecycleEvents)
                {
                    eventDetails.Append($"EventName: {item.EventName}, CommandType: {item.CommandType}, Size: {messageContent.Length}, ");
                    if (item.EventName.Equals(CommandRawDataEvent.Name))
                    {
                        var rawDataEvent = (CommandRawDataEvent)item;
                        eventDetails.Append($"TotalPXSCommands: {rawDataEvent.PxsCommands.Count}, PxsCommands: [");

                        var index = 0;
                        foreach (var pxsCommand in rawDataEvent.PxsCommands)
                        {
                            eventDetails.Append($"{pxsCommand}, ");
                            if(++index >= 5)
                            {
                                // Log first 5 commands to know more about the content.
                                eventDetails.Append("]");
                                break;
                            }
                        }

                        if(index < 5)
                        {
                            eventDetails.Append("]");
                        }
                    }
                }
                eventDetails.Append("]");
                DualLogger.Instance.Error(nameof(DeserializeAndLogBulkEventInfo), $"{eventDetails}");
            } 
            catch (Exception ex)
            {
                // This can only occur if message is deserialized using different algorithm, because we have a control on IsCompressionDisabled flag using FeatureManager, which can be changed on the fly.
                DualLogger.Instance.Error(nameof(DeserializeAndLogBulkEventInfo), $"Exception: {ex.ToString()}");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static CommandLifecycleEvent ParseEvent(string name, JObject jsonEvent)
        {
            switch (name)
            {
                case CommandCompletedEvent.Name:
                    return jsonEvent.ToObject<CommandCompletedEvent>();

                case CommandSoftDeleteEvent.Name:
                    return jsonEvent.ToObject<CommandSoftDeleteEvent>();

                case CommandStartedEvent.Name:
                    return jsonEvent.ToObject<CommandStartedEvent>();

                case CommandSentToAgentEvent.Name:
                    return jsonEvent.ToObject<CommandSentToAgentEvent>();

                case CommandPendingEvent.Name:
                    return jsonEvent.ToObject<CommandPendingEvent>();

                case CommandFailedEvent.Name:
                    return jsonEvent.ToObject<CommandFailedEvent>();

                case CommandUnexpectedEvent.Name:
                    return jsonEvent.ToObject<CommandUnexpectedEvent>();

                case CommandVerificationFailedEvent.Name:
                    return jsonEvent.ToObject<CommandVerificationFailedEvent>();

                case CommandUnexpectedVerificationFailureEvent.Name:
                    return jsonEvent.ToObject<CommandUnexpectedVerificationFailureEvent>();

                case CommandRawDataEvent.Name:
                    return jsonEvent.ToObject<CommandRawDataEvent>();

                case CommandDroppedEvent.Name:
                    return jsonEvent.ToObject<CommandDroppedEvent>();

                // No-ops can just be ignored.
                case LegacyAuditEventName:
                case NoopEventName:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException("Unexpected event type: " + name);
            }
        }
    }
}
