// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Moq;

    internal static class VortexTestSettings
    {
        private static readonly string deviceId = $"g:{DeviceIdValue}";

        public static readonly string JsonEvent = @"{
                      ""time"": ""2010-08-05T21:58:32.6047360Z"",
                      ""cV"": """ + CorrelationVector + @""",
                      ""ext"": {
                          ""device"": {
                              ""id"": """ + deviceId + @"""
                          },
                          ""user"": {
                              ""id"": """ + AuthorizationId + @"""
                          }
                      },
                      ""data"": {     
                            ""IsInitiatedByUser"": "  + RandomHelper.Next(0, 2) + @"
                          }
                      }";

        public static readonly string BadJsonEvent = @"{
                      ""time"": ""2010-08-05T21:58:32.6047360Z"",
                      ""cV"": """ + CorrelationVector + @""",
                      ""ext"": {
                          ""device"": {
                              ""id"": ""s:123-654-notgood""
                          },
                          ""user"": {
                              ""id"": """ + AuthorizationId + @"""
                          }
                      }
                  }";

        public static readonly string LegacyJsonEvent = @"{
                      ""time"": ""2010-08-05T21:58:32.6047360Z"",
                      ""deviceId"": """ + deviceId + @""",
                      ""userId"": """ + AuthorizationId + @""",
                      ""tags"": {
                          ""cV"": """ + CorrelationVector + @"""
                      }
                  }";

        public static readonly string LegacyJsonEventNoCv = @"{
                      ""time"": ""2010-08-05T21:58:32.6047360Z"",
                      ""deviceId"": """ + deviceId + @""",
                      ""userId"": """ + AuthorizationId + @""",
                  }";

        public static readonly string JsonEvents = @"{
                      ""Events"": [" + JsonEvent + @"]
                  }";

        public static string CreateEventsString(params string[] events)
        {
                return @"{
                      ""Events"": [" + string.Join(",", events) + @"]
                  }";
        }

        private const string AuthorizationId = "p:123456";

        private const string CorrelationVector = "correlationvector";

        private const ulong DeviceIdValue = 123456;

        /// <summary>
        ///     Creates the signal writer mocks.
        /// </summary>
        /// <param name="ids">The privacy data type ids.</param>
        /// <param name="pcfAdapter">The PCF adapter.</param>
        /// <param name="msaIdentityServiceAdapter">The Msa Identity Service Adapter.</param>
        public static void CreateSignalWriterMocks(
            DataTypes.KnownIds ids,
            out Mock<IPcfAdapter> pcfAdapter,
            out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter)
        {
            pcfAdapter = new Mock<IPcfAdapter>(MockBehavior.Strict);

            // Only allow it to be called with the specific data types
            var allowedDataTypes = new List<string>
            {
                ids.BrowsingHistory.Value,
                ids.ProductAndServicePerformance.Value,
                ids.ProductAndServiceUsage.Value,
                ids.InkingTypingAndSpeechUtterance.Value,
                ids.SoftwareSetupAndInventory.Value,
                ids.DeviceConnectivityAndConfiguration.Value
            };

            pcfAdapter.Setup(
                    ega => ega.PostCommandsAsync(It.Is<List<PrivacyRequest>>(dr => dr.Any(d => allowedDataTypes.Contains(((DeleteRequest)d).PrivacyDataType)))))
                .ReturnsAsync(new AdapterResponse());

            msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            msaIdentityServiceAdapter
                .Setup(c => c.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Result = "i_am_a_device_delete_verifier_token" });
        }

        /// <summary>
        /// Verifies the delete request processed the expected calls
        /// </summary>
        /// <param name="pcfAdapter">The PCF adapter.</param>
        /// <param name="msaIdentityServiceAdapter">The msa identity service adapter.</param>
        /// <param name="ids">The ids.</param>
        /// <param name="hasCv">if set to <c>true</c> [has cv].</param>
        /// <param name="eventsProcessed">The expected events passed to partners.</param>
        public static void VerifyDeleteRequestProcessed(
            Mock<IPcfAdapter> pcfAdapter,
            Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter,
            DataTypes.KnownIds ids,
            bool hasCv = true,
            int eventsProcessed = 1)
        {
            // Only allow it to be called with the specific data types
            var allowedDataTypes = new List<string>
            {
                ids.BrowsingHistory.Value,
                ids.ProductAndServicePerformance.Value,
                ids.ProductAndServiceUsage.Value,
                ids.InkingTypingAndSpeechUtterance.Value,
                ids.SoftwareSetupAndInventory.Value,
                ids.DeviceConnectivityAndConfiguration.Value
            };


            pcfAdapter.Verify(
                pcf => pcf
                    .PostCommandsAsync(
                        It.Is<List<PrivacyRequest>>(
                            d => d.Any(dr => allowedDataTypes.Contains(((DeleteRequest)dr).PrivacyDataType)) && d.All(dr => ValidatePcfSignal((DeleteRequest)dr, hasCv)))), Times.Exactly(eventsProcessed));

            msaIdentityServiceAdapter.Verify(
                msa => msa.GetGdprDeviceDeleteVerifierAsync(It.Is<Guid>(w => w != Guid.Empty), It.Is<long>(l => l != default(long)), It.IsAny<string>()), Times.Exactly(6 * eventsProcessed));
        }

        private static bool ValidatePcfSignal(DeleteRequest request, bool hasCv)
        {
            bool[] checks =
            {
                AuthorizationId == request.AuthorizationId,
                (long)DeviceIdValue == (request.Subject as DeviceSubject).GlobalDeviceId,
                !hasCv || CorrelationVector == request.CorrelationVector
            };

            return checks.All(v => v);
        }
    }
}
