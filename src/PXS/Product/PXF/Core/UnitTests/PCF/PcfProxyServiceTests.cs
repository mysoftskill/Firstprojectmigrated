// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.PCF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Graph;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.PrivacyRequest;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class PcfProxyServiceTests : PcfProxyServiceTestBase
    {
        public enum TestIdentityType
        {
            MsaSelf,

            Aad,

            AadWithMsaUser,

            BingAadWithMsaUser
        }

        [DataTestMethod]
        [DataRow(TestIdentityType.MsaSelf, 123456, 123456, 654321, false)] // Mismatching command id
        [DataRow(TestIdentityType.MsaSelf, 123456, 123456, 123456, true)] // Matching command id
        [DataRow(TestIdentityType.MsaSelf, 654321, 123456, 123456, true)] // Parent requesting on behalf of child
        [DataRow(TestIdentityType.MsaSelf, 654321, 123456, 654321, false)] // Parent requesting on behalf of child, but command matches parent
        public async Task ForceCompleteAccountCheck(TestIdentityType identityType, long authorizingPuid, long targetPuid, long commandPuid, bool shouldSucceed)
        {
            this.mockPcfAdapter.Setup(pcf => pcf.GetRequestByIdAsync(default(Guid), It.IsAny<bool>())).ReturnsAsync(
                new AdapterResponse<CommandStatusResponse>
                {
                    Result = new CommandStatusResponse
                    {
                        Subject = new MsaSubject
                        {
                            Puid = commandPuid
                        }
                    }
                });

            this.mockPcfAdapter.Setup(pcf => pcf.ForceCompleteAsync(default(Guid))).ReturnsAsync(new AdapterResponse());

            PcfProxyService proxyService = this.CreatePcfProxyService();
            ServiceResponse response = await proxyService.TestForceCommandCompletionAsync(
                CreateContext(identityType, targetPuid, authorizingPuid),
                default(Guid)).ConfigureAwait(false);
            Assert.AreEqual(shouldSucceed, response.IsSuccess);

            this.mockPcfAdapter.Verify(pcf => pcf.ForceCompleteAsync(default(Guid)), Times.Exactly(shouldSucceed ? 1 : 0));
            this.mockPcfAdapter.Verify(pcf => pcf.GetRequestByIdAsync(default(Guid), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task ListRequestByCallerId_NotAuthorized()
        {
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.ThrottleConfigurations).Returns(new Dictionary<string, IPrivacyThrottleConfiguration>());

            Guid exportRequestGuid = Guid.NewGuid();
            Guid accountCloseRequestGuid = Guid.NewGuid();
            Guid deleteRequestGuid = Guid.NewGuid();
            var returnedCommands = new List<CommandStatusResponse>
            {
                new CommandStatusResponse
                {
                    CommandId = exportRequestGuid,
                    CommandType = "Export"
                },
                new CommandStatusResponse
                {
                    CommandId = accountCloseRequestGuid,
                    CommandType = "AccountClose"
                },
                new CommandStatusResponse
                {
                    CommandId = deleteRequestGuid,
                    CommandType = "Delete"
                }
            };
            this.mockPcfAdapter.Setup(a => a.QueryCommandStatusAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<RequestType>>(), It.IsAny<DateTimeOffset>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<IList<CommandStatusResponse>>
                        {
                            Result = returnedCommands
                        }));

            AadRvsScopeResponse aadRvsScopeResponse = new AadRvsScopeResponse
            {
                Outcome = AadRvsOutcome.OperationSuccess.ToString(),
                Scopes = AadRvsScope.UserProcessorDelete
            };
            this.mockAadRvsAdapter.Setup(a => a.ActorListAuthorizationAsync(It.IsAny<AadRvsActorRequest>(), It.IsAny<IRequestContext>()))
                .Returns(Task.FromResult(new AdapterResponse<AadRvsScopeResponse> { Result = aadRvsScopeResponse }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<IList<PrivacyRequestStatus>> actualResult =
                await pcfProxyService.ListRequestsByCallerSiteAsync(CreateContext(TestIdentityType.Aad, 123456, 123456)).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.AreEqual(actualResult.Error.Code, ErrorCode.Unauthorized.ToString());
        }

        [DataTestMethod]
        [DataRow(AdapterErrorCode.Unknown, ErrorCode.PartnerError)]
        [DataRow(AdapterErrorCode.Forbidden, ErrorCode.Forbidden)]
        [DataRow(AdapterErrorCode.Unauthorized, ErrorCode.Unauthorized)]
        [DataRow(AdapterErrorCode.TooManyRequests, ErrorCode.TooManyRequests)]
        public async Task ListRequestByCallerIdShouldMapServiceErrorCode(AdapterErrorCode adapterErrorCode, ErrorCode serviceErrorCode)
        {
            Guid exportRequestGuid = Guid.NewGuid();
            Guid accountCloseRequestGuid = Guid.NewGuid();
            Guid deleteRequestGuid = Guid.NewGuid();
            var returnedCommands = new List<CommandStatusResponse>
            {
                new CommandStatusResponse
                {
                    CommandId = exportRequestGuid,
                    CommandType = "Export"
                },
                new CommandStatusResponse
                {
                    CommandId = accountCloseRequestGuid,
                    CommandType = "AccountClose"
                },
                new CommandStatusResponse
                {
                    CommandId = deleteRequestGuid,
                    CommandType = "Delete"
                }
            };
            this.mockPcfAdapter.Setup(a => a.QueryCommandStatusAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<RequestType>>(), It.IsAny<DateTimeOffset>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<IList<CommandStatusResponse>>
                        {
                            Result = returnedCommands
                        }));

            this.mockAadRvsAdapter.Setup(a => a.ActorListAuthorizationAsync(It.IsAny<AadRvsActorRequest>(), It.IsAny<IRequestContext>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<AadRvsScopeResponse>
                        {
                            Error = new AdapterError(adapterErrorCode, "UnknownError", 500)
                        }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<IList<PrivacyRequestStatus>> actualResult =
                await pcfProxyService.ListRequestsByCallerSiteAsync(CreateContext(TestIdentityType.Aad, 123456, 123456)).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.AreEqual(actualResult.Error.Code, serviceErrorCode.ToString());
        }

        [DataTestMethod]
        [DataRow(AdapterErrorCode.Unknown, ErrorCode.PartnerError)]
        [DataRow(AdapterErrorCode.Forbidden, ErrorCode.Forbidden)]
        [DataRow(AdapterErrorCode.Unauthorized, ErrorCode.Unauthorized)]
        [DataRow(AdapterErrorCode.TooManyRequests, ErrorCode.TooManyRequests)]
        public async Task ListMyRequestByIdAsyncShouldMapServiceErrorCode(AdapterErrorCode adapterErrorCode, ErrorCode serviceErrorCode)
        {
            Guid exportRequestGuid = Guid.NewGuid();
            Guid accountCloseRequestGuid = Guid.NewGuid();
            Guid deleteRequestGuid = Guid.NewGuid();
            var returnedCommands = new List<CommandStatusResponse>
            {
                new CommandStatusResponse
                {
                    CommandId = exportRequestGuid,
                    CommandType = "Export"
                },
                new CommandStatusResponse
                {
                    CommandId = accountCloseRequestGuid,
                    CommandType = "AccountClose"
                },
                new CommandStatusResponse
                {
                    CommandId = deleteRequestGuid,
                    CommandType = "Delete"
                }
            };
            this.mockPcfAdapter.Setup(a => a.QueryCommandStatusAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<RequestType>>(), It.IsAny<DateTimeOffset>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<IList<CommandStatusResponse>>
                        {
                            Result = returnedCommands
                        }));

            this.mockAadRvsAdapter.Setup(a => a.ActorListAuthorizationAsync(It.IsAny<AadRvsActorRequest>(), It.IsAny<IRequestContext>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<AadRvsScopeResponse>
                        {
                            Error = new AdapterError(adapterErrorCode, "UnknownError", 500)
                        }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<PrivacyRequestStatus> actualResult =
                await pcfProxyService.ListMyRequestByIdAsync(CreateContext(TestIdentityType.Aad, 123456, 123456), Guid.Empty).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.AreEqual(serviceErrorCode.ToString(), actualResult.Error.Code);
        }

        [TestMethod]
        public async Task ListRequestByCallerId_Success()
        {
            Guid exportRequestGuid = Guid.NewGuid();
            Guid accountCloseRequestGuid = Guid.NewGuid();
            Guid deleteRequestGuid = Guid.NewGuid();
            var returnedCommands = new List<CommandStatusResponse>
            {
                new CommandStatusResponse
                {
                    CommandId = exportRequestGuid,
                    CommandType = "Export"
                },
                new CommandStatusResponse
                {
                    CommandId = accountCloseRequestGuid,
                    CommandType = "AccountClose"
                },
                new CommandStatusResponse
                {
                    CommandId = deleteRequestGuid,
                    CommandType = "Delete"
                }
            };
            this.mockPcfAdapter.Setup(a => a.QueryCommandStatusAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<RequestType>>(), It.IsAny<DateTimeOffset>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<IList<CommandStatusResponse>>
                        {
                            Result = returnedCommands
                        }));

            AadRvsScopeResponse aadRvsScopeResponse = new AadRvsScopeResponse
            {
                Outcome = AadRvsOutcome.OperationSuccess.ToString(),
                Scopes = AadRvsScope.UserProcesscorExportAll
            };
            this.mockAadRvsAdapter.Setup(a => a.ActorListAuthorizationAsync(It.IsAny<AadRvsActorRequest>(), It.IsAny<IRequestContext>()))
                .Returns(Task.FromResult(new AdapterResponse<AadRvsScopeResponse> { Result = aadRvsScopeResponse }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<IList<PrivacyRequestStatus>> actualResult =
                await pcfProxyService.ListRequestsByCallerSiteAsync(CreateContext(TestIdentityType.Aad, 123456, 123456)).ConfigureAwait(false);
            Assert.IsTrue(actualResult.IsSuccess);
            Assert.AreEqual(2, actualResult.Result.Count);
            Assert.IsFalse(actualResult.Result.Any(s => s.Id == accountCloseRequestGuid));
        }

        [TestMethod]
        public async Task ListRequestByCallerId_ViralUser_Success()
        {
            Guid exportRequestGuid = Guid.NewGuid();
            Guid accountCloseRequestGuid = Guid.NewGuid();
            Guid deleteRequestGuid = Guid.NewGuid();
            var returnedCommands = new List<CommandStatusResponse>
            {
                new CommandStatusResponse
                {
                    CommandId = exportRequestGuid,
                    CommandType = "Export"
                },
                new CommandStatusResponse
                {
                    CommandId = accountCloseRequestGuid,
                    CommandType = "AccountClose"
                },
                new CommandStatusResponse
                {
                    CommandId = deleteRequestGuid,
                    CommandType = "Delete"
                }
            };
            this.mockPcfAdapter.Setup(a => a.QueryCommandStatusAsync(It.IsAny<IPrivacySubject>(), It.IsAny<string>(), It.IsAny<IList<RequestType>>(), It.IsAny<DateTimeOffset>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<IList<CommandStatusResponse>>
                        {
                            Result = returnedCommands
                        }));

            AadRvsScopeResponse aadRvsScopeResponse = new AadRvsScopeResponse
            {
                Outcome = AadRvsOutcome.OperationSuccess.ToString(),
                Scopes = AadRvsScope.UserProcessorExport
            };
            this.mockAadRvsAdapter.Setup(a => a.ActorListAuthorizationAsync(It.IsAny<AadRvsActorRequest>(), It.IsAny<IRequestContext>()))
                .Returns(Task.FromResult(new AdapterResponse<AadRvsScopeResponse> { Result = aadRvsScopeResponse }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<IList<PrivacyRequestStatus>> actualResult =
                await pcfProxyService.ListRequestsByCallerSiteAsync(CreateContext(TestIdentityType.Aad, 123456, 123456)).ConfigureAwait(false);
            Assert.IsTrue(actualResult.IsSuccess);
            Assert.AreEqual(2, actualResult.Result.Count);
            Assert.IsFalse(actualResult.Result.Any(s => s.Id == accountCloseRequestGuid));
        }

        [TestMethod]
        public async Task ListRequestById_NotAuthorized()
        {
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.GetRequestByIdSecurityGroups).Returns(new List<string> { Guid.NewGuid().ToString() });
            this.mockGraphAdapter.Setup(c => c.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(
                Task.FromResult(
                    new AdapterResponse<IsMemberOfResponse> { Result = new IsMemberOfResponse { Value = false } }));

            Guid exportRequestGuid = Guid.NewGuid();
            var returnedCommand = new CommandStatusResponse
            {
                CommandId = exportRequestGuid,
                CommandType = "Export"
            };

            this.mockPcfAdapter.Setup(a => a.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<CommandStatusResponse>
                        {
                            Result = returnedCommand
                        }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<CommandStatusResponse> actualResult =
                await pcfProxyService.ListRequestByIdAsync(CreateContext(TestIdentityType.AadWithMsaUser, 123456, 123456), exportRequestGuid).ConfigureAwait(false);
            Assert.IsFalse(actualResult.IsSuccess);
            Assert.AreEqual(actualResult.Error.Code, ErrorCode.Unauthorized.ToString());
        }

        [TestMethod]
        public async Task ListRequestById_Success()
        {
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.GetRequestByIdSecurityGroups).Returns(new List<string> { Guid.NewGuid().ToString() });
            this.mockGraphAdapter.Setup(c => c.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(
                Task.FromResult(
                    new AdapterResponse<IsMemberOfResponse> { Result = new IsMemberOfResponse { Value = true } }));

            Guid exportRequestGuid = Guid.NewGuid();
            var returnedCommand = new CommandStatusResponse
            {
                CommandId = exportRequestGuid,
                CommandType = "Export"
            };

            this.mockPcfAdapter.Setup(a => a.GetRequestByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<CommandStatusResponse>
                        {
                            Result = returnedCommand
                        }));

            PcfProxyService pcfProxyService = this.CreatePcfProxyService();
            ServiceResponse<CommandStatusResponse> actualResult =
                await pcfProxyService.ListRequestByIdAsync(CreateContext(TestIdentityType.AadWithMsaUser, 123456, 123456), exportRequestGuid).ConfigureAwait(false);
            Assert.IsTrue(actualResult.IsSuccess);
            Assert.AreEqual(actualResult.Result.CommandId, exportRequestGuid);
        }

        [TestMethod]
        public async Task PcfProxyServiceExportFetchXuidTest()
        {
            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = CreateContext(TestIdentityType.MsaSelf, 123456, 123456);

            this.mockXboxAccountsAdapter
                .Setup(xbox => xbox.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(
                    new AdapterResponse<Dictionary<long, string>>
                    {
                        Result = new Dictionary<long, string>
                        {
                            { 123456, "123456" }
                        }
                    });

            this.mockMsaIdentityServiceAdapter
                .Setup(msa => msa.GetGdprExportVerifierAsync(It.IsAny<Guid>(), It.IsAny<IPxfRequestContext>(), It.IsAny<Uri>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "gdprverifier"
                    });

            this.mockVerificationTokenValidationService
                .Setup(ver => ver.ValidateVerifierAsync(It.IsAny<PrivacyRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse());

            this.mockPcfAdapter
                .Setup(pcf => pcf.GetPcfStorageUrisAsync())
                .ReturnsAsync(new AdapterResponse<IList<Uri>> { Result = new List<Uri> { new Uri("https://unittest") } });

            this.mockPcfAdapter
                .Setup(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()))
                .ReturnsAsync(new AdapterResponse());

            var types = new List<string>
            {
                Policies.Current.DataTypes.Ids.PreciseUserLocation.Value
            };

            ServiceResponse<Guid> response = await service.PostExportRequestAsync(
                ctx,
                PrivacyRequestConverter.CreateExportRequest(
                    PrivacyRequestConverter.CreateMsaSubjectFromContext(ctx),
                    ctx,
                    Guid.NewGuid(),
                    Guid.NewGuid().ToString(),
                    DateTimeOffset.UtcNow,
                    "ctx",
                    "verifier",
                    null,
                    types,
                    false,
                    "i-am-cloud",
                    "portal1",
                    false)).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess);

            this.mockXboxAccountsAdapter
                .Verify(xb => xb.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);

            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprExportVerifierAsync(It.IsAny<Guid>(), It.IsAny<IPxfRequestContext>(), It.IsAny<Uri>(), It.IsAny<string>()), Times.Once);
        }

        [DataTestMethod]
        public async Task PostDeleteRequestsAsyncBatchMsaSubjectByCardIdsSuccess()
        {
            const int Puid = 111;
            const int NumberPcfDeleteRequests = 25;
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.PRCSecurityGroup).Returns(Guid.NewGuid().ToString());

            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = this.CreateContext(TestIdentityType.MsaSelf, Puid, 222);

            this.mockXboxAccountsAdapter
                .Setup(xbox => xbox.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(new AdapterResponse<Dictionary<long, string>> { Result = new Dictionary<long, string> { { Puid, "4026260262" } } });

            this.mockMsaIdentityServiceAdapter
                .Setup(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "gdprverifier" + Guid.NewGuid() // want this to be unique for this test
                    });

            IList<LocationCard> cards = new List<LocationCard> { PrivacyRequestConverterTests.CreateRandomLocationCard(NumberPcfDeleteRequests) };

            List<DeleteRequest> pcfRequests = cards.ToUserPcfDeleteRequests(
                ctx,
                Guid.NewGuid(),
                "i.am.cv",
                DateTimeOffset.UtcNow,
                Policies.Current,
                "morecloudsyousay?",
                "no, more portals.",
                false).ToList();

            // Act
            ServiceResponse<IList<Guid>> response = await service.PostDeleteRequestsAsync(ctx, pcfRequests).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess, $"{response?.Error}");
            Assert.IsTrue(
                NumberPcfDeleteRequests < PcfProxyService.MaxMsaCommandCountPerVerifier,
                "Number requests should be less than # per claim for this test to function properly");

            // Should only have called MSA once to get a verifier because they should be batched.
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Should be exactly 'NumberPcfDeleteRequests' command ids sent to MSA RVS and PCF
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.Is<IList<PrivacyRequest>>(c => c.Count == NumberPcfDeleteRequests)), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.Is<IList<Guid>>(c => c.Count == NumberPcfDeleteRequests), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            this.mockGraphAdapter
                .Verify((ga => ga.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())), Times.Never);
            this.mockXboxAccountsAdapter
                .Verify(xb => xb.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.mockAadRvsAdapter
                .Verify(xb => xb.ConstructDeleteAsync(It.IsAny<AadRvsRequest>(), It.IsAny<IRequestContext>()), Times.Never);
        }

        [DataTestMethod]
        public async Task PostDeleteRequestsAsyncEmptyOrNullListFailedWithInvalidInput()
        {
            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = this.CreateContext(TestIdentityType.Aad, 1234, 1234);

            ServiceResponse<IList<Guid>> response = await service.PostDeleteRequestsAsync(ctx, null).ConfigureAwait(false);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(response.Error.Code, ErrorCode.InvalidInput.ToString());

            response = await service.PostDeleteRequestsAsync(ctx, new List<DeleteRequest>()).ConfigureAwait(false);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(response.Error.Code, ErrorCode.InvalidInput.ToString());

            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Never);
            this.mockAadRvsAdapter.Verify(xb => xb.ConstructDeleteAsync(It.IsAny<AadRvsRequest>(), It.IsAny<IRequestContext>()), Times.Never);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [DataTestMethod]
        public async Task PostDeleteRequestsAsyncBatchMsaSubjectByCardIdsExceedingMaxCommandIdsSuccess()
        {
            // By exceeding the count, this should become > 1 requests to MSA RVS
            const int NumberOfCallsToMsaRvs = 2;
            const int NumberPcfDeleteRequests = PcfProxyService.MaxMsaCommandCountPerVerifier + 1;
            const int Puid = 111;
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.PRCSecurityGroup).Returns(Guid.NewGuid().ToString());

            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = this.CreateContext(TestIdentityType.MsaSelf, Puid, 222);

            this.mockXboxAccountsAdapter
                .Setup(xbox => xbox.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(new AdapterResponse<Dictionary<long, string>> { Result = new Dictionary<long, string> { { Puid, "4026260262" } } });

            this.mockMsaIdentityServiceAdapter
                .Setup(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "gdprverifier" + Guid.NewGuid() // want this to be unique for this test
                    });

            IList<LocationCard> cards = new List<LocationCard> { PrivacyRequestConverterTests.CreateRandomLocationCard(NumberPcfDeleteRequests) };

            List<DeleteRequest> pcfRequests = cards.ToUserPcfDeleteRequests(
                ctx,
                Guid.NewGuid(),
                "i.am.cv",
                DateTimeOffset.UtcNow,
                Policies.Current,
                "morecloudsyousay?",
                "no, more portals.",
                false).ToList();

            // Act
            ServiceResponse<IList<Guid>> response = await service.PostDeleteRequestsAsync(ctx, pcfRequests).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess, $"{response?.Error}");
            Assert.IsTrue(
                NumberPcfDeleteRequests > PcfProxyService.MaxMsaCommandCountPerVerifier,
                "Number requests should be greater than # per claim for this test to function properly");

            // Should have called PCF and MSA correct # of timers because they should be batched in both cases.
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.Is<IList<PrivacyRequest>>(c => c.Count == NumberPcfDeleteRequests)), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(NumberOfCallsToMsaRvs));

            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.Is<IList<Guid>>(c => c.Count == PcfProxyService.MaxMsaCommandCountPerVerifier), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.Is<IList<Guid>>(c => c.Count == 1), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            this.mockGraphAdapter
                .Verify((ga => ga.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())), Times.Never);
            this.mockXboxAccountsAdapter
                .Verify(xb => xb.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.mockAadRvsAdapter
                .Verify(xb => xb.ConstructDeleteAsync(It.IsAny<AadRvsRequest>(), It.IsAny<IRequestContext>()), Times.Never);
        }

        [DataTestMethod]
        public async Task PostDeleteRequestsAsyncBatchMsaSubjectVerifiersCorrectly()
        {
            const int Puid = 111;
            var dataTypes = new List<string> { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value, Policies.Current.DataTypes.Ids.BrowsingHistory.Value };
            int expectedNumberDeleteRequests = dataTypes.Count;
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.PRCSecurityGroup).Returns(Guid.NewGuid().ToString());

            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = this.CreateContext(TestIdentityType.MsaSelf, Puid, 222);

            this.mockXboxAccountsAdapter
                .Setup(xbox => xbox.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(new AdapterResponse<Dictionary<long, string>> {Result = new Dictionary<long, string> { { Puid, "4026260262" } }});

            this.mockMsaIdentityServiceAdapter
                .Setup(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "gdprverifier" + Guid.NewGuid() // want this to be unique for this test
                    });

            List<DeleteRequest> pcfRequests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                PrivacyRequestConverter.CreateMsaSubjectFromContext(ctx),
                ctx,
                Guid.NewGuid(),
                "context",
                null,
                DateTimeOffset.UtcNow,
                dataTypes,
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                cloudInstance: "cloud-is-the-future",
                portal: "no-portal-is",
                isTest: false).ToList();

            // Act
            ServiceResponse<IList<Guid>> response = await service.PostDeleteRequestsAsync(ctx, pcfRequests).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess, $"{response?.Error}");

            // Should have called PCF and MSA correct # of timers because they should be batched in both cases.
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.Is<IList<PrivacyRequest>>(c => c.Count == expectedNumberDeleteRequests)), Times.Once);

            // And all of them should have verifiers.
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.Is<IList<PrivacyRequest>>(c => c.All(r => !string.IsNullOrWhiteSpace(r.VerificationToken)))), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Should be exactly right # of command ids sent to MSA RVS.
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.Is<IList<Guid>>(c => c.Count == expectedNumberDeleteRequests), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never);

            this.mockGraphAdapter
                .Verify((ga => ga.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())), Times.Never);

            this.mockXboxAccountsAdapter
                .Verify(xb => xb.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.mockAadRvsAdapter
                .Verify(xb => xb.ConstructDeleteAsync(It.IsAny<AadRvsRequest>(), It.IsAny<IRequestContext>()), Times.Never);
        }

        [DataTestMethod]
        public async Task PostDeleteRequestsAsyncBatchMsaSubjecBingScopedDeleteSuccess()
        {
            const int Puid = 111;
            const int NumberPcfDeleteRequests = 25;

            IEnumerable<IPrivacyPredicate> predicates = Enumerable.Range(0, NumberPcfDeleteRequests)
                .Select(i => new SearchRequestsAndQueryPredicate() { ImpressionGuid = Guid.NewGuid().ToString("n") });

            IPcfProxyService service = this.CreatePcfProxyService();
            IRequestContext ctx = this.CreateContext(TestIdentityType.BingAadWithMsaUser, Puid, Puid);

            this.mockXboxAccountsAdapter
                .Setup(xbox => xbox.GetXuidsAsync(It.IsAny<IEnumerable<long>>()))
                .ReturnsAsync(new AdapterResponse<Dictionary<long, string>> { Result = new Dictionary<long, string> { { Puid, "4026260262" } } });

            this.mockMsaIdentityServiceAdapter
                .Setup(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Result = "gdprverifier" + Guid.NewGuid() // want this to be unique for this test
                    });

            List<DeleteRequest> pcfRequests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                PrivacyRequestConverter.CreateMsaSubjectFromContext(ctx),
                ctx,
                Guid.NewGuid(),
                "context",
                null,
                DateTimeOffset.UtcNow,
                predicates,
                Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value,
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                cloudInstance: "cloud-is-the-future",
                portal: "no-portal-is",
                isTest: false).ToList();

            // Act
            ServiceResponse<IList<Guid>> response = await service.PostDeleteRequestsAsync(ctx, pcfRequests).ConfigureAwait(false);

            Assert.IsTrue(response.IsSuccess, $"{response?.Error}");
            Assert.IsTrue(
                NumberPcfDeleteRequests < PcfProxyService.MaxMsaCommandCountPerVerifier,
                "Number requests should be less than # per claim for this test to function properly");

            // Should only have called MSA once to get a verifier because they should be batched.
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.IsAny<IList<Guid>>(), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Should be exactly 'NumberPcfDeleteRequests' command ids sent to MSA RVS and PCF
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>()), Times.Once);
            this.mockPcfAdapter.Verify(pcf => pcf.PostCommandsAsync(It.Is<IList<PrivacyRequest>>(c => c.Count == NumberPcfDeleteRequests)), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprUserDeleteVerifierAsync(It.Is<IList<Guid>>(c => c.Count == NumberPcfDeleteRequests), It.IsAny<IPxfRequestContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.mockMsaIdentityServiceAdapter
                .Verify(msa => msa.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            this.mockGraphAdapter
                .Verify((ga => ga.IsMemberOfAsync(It.IsAny<Guid>(), It.IsAny<Guid>())), Times.Never);
            this.mockXboxAccountsAdapter
                .Verify(xb => xb.GetXuidsAsync(It.IsAny<IEnumerable<long>>()), Times.Once);
            this.mockAadRvsAdapter
                .Verify(xb => xb.ConstructDeleteAsync(It.IsAny<AadRvsRequest>(), It.IsAny<IRequestContext>()), Times.Never);
        }

        private IRequestContext CreateContext(TestIdentityType testIdentityType, long puid, long authorizingPuid)
        {
            string appId = Guid.NewGuid().ToString();
            string aadSiteName = null;
            switch(testIdentityType)
            {
                case TestIdentityType.Aad:
                    aadSiteName = "MSGraph_test";
                    break;
                case TestIdentityType.AadWithMsaUser:
                    aadSiteName = "PCD_Test";
                    break;
                case TestIdentityType.BingAadWithMsaUser:
                    aadSiteName = "Bing_Test";
                    break;
                case TestIdentityType.MsaSelf:
                default:
                    break;
            }
            this.privacyExperienceServiceConfiguration.SetupGet(c => c.SiteIdToCallerName).Returns(new Dictionary<string, string>()
            {
                { "1234", "MEEPortal_Test" },
                { appId, aadSiteName}
            });

            IIdentity identity = null;
            switch (testIdentityType)
            {
                case TestIdentityType.AadWithMsaUser:
                case TestIdentityType.BingAadWithMsaUser:
                    identity = new AadIdentityWithMsaUserProxyTicket(
                        appId,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "access token",
                        "Unit Test App",
                        puid,
                        "proxy ticket",
                        654321);
                    break;
                case TestIdentityType.Aad:
                    identity = new AadIdentity(
                        appId,
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "access token",
                        "Unit Test App");
                    break;
                case TestIdentityType.MsaSelf:
                    identity = new MsaSelfIdentity(
                        "proxy ticket",
                        "fwt",
                        authorizingPuid,
                        puid,
                        null,
                        nameof(PcfProxyServiceTests),
                        1234,
                        654321,
                        "US",
                        null,
                        false,
                        AuthType.MsaSelf,
                        LegalAgeGroup.Undefined);
                    break;
            }

            return new RequestContext(
                identity,
                new Uri("https://unittest"),
                new Dictionary<string, string[]>());
        }
    }
}
