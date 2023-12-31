namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Newtonsoft.Json.Linq;

    using PXSV1 = PXS.Command.Contracts.V1;

    internal class PxsInsertCommandActionResult : BaseHttpActionResult
    {
        private static readonly IPerformanceCounter CommandTypeCounter = PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "IncomingCommandType");

        private readonly IAzureWorkItemQueuePublisher<PublishCommandBatchWorkItem> publisher;
        private readonly IValidationService validationService;
        private readonly JObject[] pxsRequests;
        private readonly long currentDataSetVersion;
        private readonly AuthenticationScope authenticationScope;
        private readonly ICommandLifecycleEventPublisher lifecycleEventPublisher;
        private readonly IAuthorizer authorizer;
        private readonly HttpRequestMessage requestMessage;

        public PxsInsertCommandActionResult(
            HttpRequestMessage request,
            IAzureWorkItemQueuePublisher<PublishCommandBatchWorkItem> publisher,
            IValidationService validationService,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope,
            ICommandLifecycleEventPublisher lifecycleEventPublisher,
            long currentDataSetVersion,
            JObject[] pxsRequests)
        {
            this.requestMessage = request;
            this.publisher = publisher;
            this.validationService = validationService;
            this.pxsRequests = pxsRequests;
            this.currentDataSetVersion = currentDataSetVersion;
            this.authenticationScope = authenticationScope;
            this.lifecycleEventPublisher = lifecycleEventPublisher;
            this.authorizer = authorizer;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.authenticationScope);

            List<JObject> requestsToSend = new List<JObject>();
            var pxsCommands = new List<PXSV1.PrivacyRequest>();

            // Parse and validate each member.
            foreach (var item in this.pxsRequests)
            {
                var (pcfCommand, pxsCommand) = PxsCommandParser.DummyParser.Process(item);

#if INCLUDE_TEST_HOOKS
                // Requests forwarded to the stress environment don't come with an export URI.
                if (Config.Instance.Common.IsStressEnvironment && pxsCommand is PXSV1.ExportRequest exportRequest)
                {
                    exportRequest.StorageUri = ExportStorageManager.Instance.GetManagedStorageUri();
                }
#endif

                string statusKey = $"Command.{pcfCommand.CommandId}";

                if (pxsCommand?.IsTestRequest == true)
                {
                    IncomingEvent.Current?.SetProperty("Test", "true");
                }

                // Bail immediately if watchdog. No need to write anything into the command lifecyle.
                if (pxsCommand?.IsWatchdogRequest == true)
                {
                    IncomingEvent.Current?.SetProperty(statusKey, "Watchdog");
                    PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "WatchdogRequestsDropped").Increment();
                    continue;
                }

                pxsCommands.Add(pxsCommand);
                CommandTypeCounter.Increment(pcfCommand.CommandType.ToString());

                // Check the verifier.
                bool isValid = await pcfCommand.IsVerifierValidAsync(this.validationService);
                if (!isValid)
                {
                    IncomingEvent.Current?.SetProperty(statusKey, "InvalidVerifier");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("The verifier for command ID " + pcfCommand.CommandId + " was invalid")
                    };
                }

                IncomingEvent.Current?.SetProperty(statusKey, $"Sending=true;CommandType={pcfCommand.CommandType},CA={pcfCommand.ControllerApplicable},PA={pcfCommand.ProcessorApplicable}");
                requestsToSend.Add(item);
            }

#if INCLUDE_TEST_HOOKS
            if (Config.Instance.Common.IsTestEnvironment)
            {
                ProductionSafetyHelper.EnsureNotInProduction();

                // Hack: PXS INT sends an overwhelming amount of traffic to us, such that our test agents can't receive commands in a reasonable amount of time.
                // As a workaround, we tell our test cases to send a special header so that we actually process them.
                bool isPcfTestCase = this.requestMessage.Headers.TryGetValues("X-PCF-Test-Case", out var values) && !string.IsNullOrEmpty(values?.FirstOrDefault());
                if (!isPcfTestCase)
                {
                    IncomingEvent.Current?.SetProperty("DroppedDueToMissingHeader", "true");
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
#endif
            Task lifecyclePublishTask = this.lifecycleEventPublisher.PublishCommandRawDataAsync(requestsToSend);

            Task queuePublishTask = this.publisher.PublishWithSplitAsync(
                requestsToSend,
                this.CreateWorkItem,
                x => TimeSpan.Zero);

            await Task.WhenAll(lifecyclePublishTask, queuePublishTask);

            // The command has been published at this point. Now, we redact the parts that are sensitive and potentially send a copy to the stress environment.
            List<PXSV1.PrivacyRequest> commandsToForward = new List<PXSV1.PrivacyRequest>();

            for (int i = 0; i < pxsCommands.Count; ++i)
            {
                RedactPersonalFields(pxsCommands[i]);

                // Not ideal since this forces us to make N requests. However,
                // we also gain the ability to do per-command ID based filtering.
                StressRequestForwarder.Instance.SendForwardedRequest(
                    authContext,
                    this.requestMessage,
                    new JsonContent(new[] { pxsCommands[i] }),
                    commandId: new CommandId(pxsCommands[i].RequestId));
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        internal static void RedactPersonalFields(PXSV1.PrivacyRequest request)
        {
            request.VerificationToken = string.Empty;
            request.AuthorizationId = SHA256(request.AuthorizationId);
            request.Context = SHA256(request.Context);
            request.Requester = SHA256(request.Requester);

            if (request is PXSV1.ExportRequest exportRequest)
            {
                // Stress will need to repopulate this.
                exportRequest.StorageUri = null;
            }

            if (request.Subject is AadSubject aadSubject)
            {
                request.Subject = RedactAadSubject(aadSubject);
            }
            else if (request.Subject is MsaSubject msaSubject)
            {
                request.Subject = RedactMsaSubject(msaSubject);
            }
            else if (request.Subject is DemographicSubject demographicSubject)
            {
                request.Subject = RedactDemographicSubject(demographicSubject);
            }
            else if (request.Subject is MicrosoftEmployee microsoftEmployeeSubject)
            {
                request.Subject = RedactMicrosoftEmployeeSubject(microsoftEmployeeSubject);
            }
            else if (request.Subject is DeviceSubject deviceSubject)
            {
                request.Subject = RedactDeviceSubject(deviceSubject);
            }
            else
            {
                // Guard against unknown subject type by setting to null.
                request.Subject = null;
            }
        }

        internal static AadSubject RedactAadSubject(AadSubject subject)
        {
            // Return a new subject here instead of modifying existing. This is to 
            // guard against the possibility of new fields being added but forgetting to update this code.
            return new AadSubject
            {
                ObjectId = SHA256(subject.ObjectId),
                OrgIdPUID = SHA256(subject.OrgIdPUID),
                TenantId = SHA256(subject.TenantId),
            };
        }

        internal static MsaSubject RedactMsaSubject(MsaSubject subject)
        {
            // Return a new subject here instead of modifying existing. This is to 
            // guard against the possibility of new fields being added but forgetting to update this code.
            return new MsaSubject
            {
                Anid = SHA256(subject.Anid),
                Cid = SHA256(subject.Cid),
                Opid = SHA256(subject.Opid),
                Puid = SHA256(subject.Puid),
                Xuid = SHA256(subject.Xuid)
            };
        }

        internal static DeviceSubject RedactDeviceSubject(DeviceSubject subject)
        {
            // Return a new subject here instead of modifying existing. This is to 
            // guard against the possibility of new fields being added but forgetting to update this code.
            return new DeviceSubject
            {
                GlobalDeviceId = SHA256(subject.GlobalDeviceId),
                XboxConsoleId = SHA256(subject.XboxConsoleId ?? 0),
            };
        }

        internal static MicrosoftEmployee RedactMicrosoftEmployeeSubject(MicrosoftEmployee subject)
        {
            // Return a new subject here instead of modifying existing. This is to 
            // guard against the possibility of new fields being added but forgetting to update this code.
            return new MicrosoftEmployee
            {
                Emails = subject.Emails?.Select(SHA256),
                EmployeeId = SHA256(subject.EmployeeId ?? string.Empty),
                EndDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow,
            };
        }

        internal static DemographicSubject RedactDemographicSubject(DemographicSubject subject)
        {
            // Return a new subject here instead of modifying existing. This is to 
            // guard against the possibility of new fields being added but forgetting to update this code.
            return new DemographicSubject
            {
                Address = new AddressQueryParams
                {
                    Cities = subject.Address?.Cities?.Select(SHA256),
                    PostalCodes = subject.Address?.PostalCodes?.Select(SHA256),
                    States = subject.Address?.States?.Select(SHA256),
                    StreetNumbers = subject.Address?.StreetNumbers?.Select(SHA256),
                    Streets = subject.Address?.Streets?.Select(SHA256),
                    UnitNumbers = subject.Address?.UnitNumbers?.Select(SHA256)
                },
                EmailAddresses = subject.EmailAddresses?.Select(SHA256),
                Names = subject.Names?.Select(SHA256),
                PhoneNumbers = subject.PhoneNumbers?.Select(SHA256),
            };
        }

        private static long SHA256(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            byte[] hash = SHA256(bytes);
            return BitConverter.ToInt64(hash, 0);
        }

        private static Guid SHA256(Guid guid)
        {
            byte[] guidBytes = guid.ToByteArray();
            byte[] hashed = SHA256(guidBytes);

            Buffer.BlockCopy(hashed, 0, guidBytes, 0, guidBytes.Length);
            return new Guid(guidBytes);
        }

        private static string SHA256(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Convert.ToBase64String(SHA256(Encoding.UTF8.GetBytes(value)));
        }

        private static byte[] SHA256(byte[] bytes)
        {
            using (var sha2 = new SHA256Managed())
            {
                return sha2.ComputeHash(bytes);
            }
        }

        private PublishCommandBatchWorkItem CreateWorkItem(IEnumerable<JObject> requestsToSend)
        {
            return new PublishCommandBatchWorkItem
            {
                DataSetVersion = this.currentDataSetVersion,
                PxsCommands = requestsToSend.ToList(),
            };
        }
    }
}
