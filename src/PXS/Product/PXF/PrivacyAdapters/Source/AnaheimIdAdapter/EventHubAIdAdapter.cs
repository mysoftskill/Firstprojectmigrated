namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using global::Azure.Messaging.EventHubs;
    using System;
    using System.Threading.Tasks;

    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.PrivacyServices.Common.Azure;

    using Newtonsoft.Json;

    public class EventHubAIdAdapter : IAnaheimIdAdapter
    {
        private const string ComponentName = nameof(EventHubAIdAdapter);

        private readonly IEventHubProducer producer;

        private readonly ILogger logger;

        private const string OperationNameSendDeviceIDRequestToEventHub = "AnaheimID_SendDeviceDeleteIDRequestToEventHub";

        /// <summary>
        ///     Creates a new instance of <see cref="EventHubAIdAdapter" />
        /// </summary>
        /// <param name="eventHubProducer"></param>
        /// <param name="logger"></param>
        public EventHubAIdAdapter(IEventHubProducer eventHubProducer, ILogger logger)
        {
            this.producer = eventHubProducer ?? throw new ArgumentNullException(nameof(eventHubProducer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Call EventHubProducer to send requests to EventHub />
        /// </summary>
        /// <param name="deleteDeviceIdRequest"></param>
        public async Task<AdapterResponse> SendDeleteDeviceIdRequestAsync(DeleteDeviceIdRequest deleteDeviceIdRequest)
        {
            this.logger.MethodEnter(ComponentName, nameof(this.SendDeleteDeviceIdRequestAsync));

            AdapterResponse response;
            OutgoingApiEventWrapper apiEvent = this.CreateApiEvent(EventHubAIdAdapter.OperationNameSendDeviceIDRequestToEventHub);
            apiEvent.Start();

            apiEvent.ExtraData["RequestId"] = deleteDeviceIdRequest.RequestId.ToString();
            apiEvent.ExtraData["CorrelationVector"] = deleteDeviceIdRequest.CorrelationVector;
            apiEvent.ExtraData["CreateTime"] = deleteDeviceIdRequest.CreateTime.ToString();
            apiEvent.ExtraData["TestSignal"] = deleteDeviceIdRequest.TestSignal.ToString();

            try
            {
                await this.producer.SendAsync(JsonConvert.SerializeObject(deleteDeviceIdRequest)).ConfigureAwait(false);
                response = new AdapterResponse();
                apiEvent.Success = true;
            }
            catch (JsonSerializationException e)
            {
                response =  new AdapterResponse { Error = new AdapterError(AdapterErrorCode.JsonDeserializationFailure, e.Message, 500) };
                this.logger.Error(ComponentName, response.Error.Message);
                apiEvent.ErrorMessage = e.ToString();
            }
            catch (EventHubsException e)
            {
                var errorMessage = $"Error message: {e.Message}. Failed Reason: {e.Reason}";
                response = new AdapterResponse { Error = new AdapterError(AdapterErrorCode.Unknown, errorMessage, 500) };
                this.logger.Error(ComponentName, response.Error.Message);
                apiEvent.ErrorMessage = e.ToString();
            }
            catch (Exception e)
            {
                response =  new AdapterResponse { Error = new AdapterError(AdapterErrorCode.Unknown, e.Message, 500) };
                this.logger.Error(ComponentName, response.Error.Message);
                apiEvent.ErrorMessage = e.ToString();
            }
            finally
            {
                apiEvent.Finish();
            }
            return response;
        }


        /// <summary>
        ///     Generates an ApiEventWrapper for the EventHub calls
        /// </summary>
        /// <param name="operation">operation tag</param>
        /// <returns>resulting value</returns>
        private OutgoingApiEventWrapper CreateApiEvent(string operation)
        {
            return new OutgoingApiEventWrapper
            {
                DependencyOperationVersion = string.Empty,
                DependencyOperationName = operation,
                DependencyName = nameof(EventHubAIdAdapter),
                DependencyType = "EventHub",
                PartnerId = "EventHub",
                Success = false,
            };
        }
    }
}
