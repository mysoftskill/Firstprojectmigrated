namespace PCF.UnitTests.Pdms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Service.Tests.Common;
    using Microsoft.PrivacyServices.Policy;

    using Xunit;

    using PcfSubject = Microsoft.PrivacyServices.CommandFeed.Service.Common.SubjectType;

    /// <summary>
    /// MethodName_WhatsBeingTested_ExpectedResult
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class PdmsUnitTests
    {
        /// <summary>
        /// Tests parsing unexpected PDMS subjects.
        /// </summary>
        [Fact]
        public void UnrecognizedSubjectTypeTest()
        {
            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParsePdmsSubjectType("foobar", out _));

            PdmsSubjectType pdmsSubjectType = (PdmsSubjectType)(-1);
            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParsePcfSubject(pdmsSubjectType, out _));
        }

        /// <summary>
        /// Tests parsing unexpected PDMS subjects.
        /// </summary>
        [Fact]
        public void UnrecognizedDataTypeTest()
        {
            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParseDataType("foobar", out _));
        }

        /// <summary>
        /// Tests parsing unexpected PDMS subjects.
        /// </summary>
        [Fact]
        public void UnrecognizedCapabilityTest()
        {
            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParseCapability("foobar", out _));
        }

        /// <summary>
        /// Tests parsing known PDMS capabilities.
        /// </summary>
        [Fact]
        public void CapabilityParseTests()
        {
            var capabilities = new[]
            {
                ("View", PdmsInfoParserResult.Ignore, PrivacyCommandType.None),
                ("Export", PdmsInfoParserResult.Success, PrivacyCommandType.Export),
                ("Delete", PdmsInfoParserResult.Success, PrivacyCommandType.Delete),
                ("AccountClose", PdmsInfoParserResult.Success, PrivacyCommandType.AccountClose),
                ("AgeOut", PdmsInfoParserResult.Success, PrivacyCommandType.AgeOut),
                ("foobar", PdmsInfoParserResult.Failure, PrivacyCommandType.None)
            };

            foreach (var tuple in capabilities)
            {
                Assert.Equal(tuple.Item2, PdmsInfoParser.TryParseCapability(tuple.Item1, out var expectedCommandType));
                Assert.Equal(expectedCommandType, tuple.Item3);
            }
        }

        /// <summary>
        /// Tests the tolerant parsing logic for subjects, data types, and capabilities.
        /// </summary>
        [Fact]
        public void TolerantParsingTests()
        {
            List<PrivacyCommandType> commandTypeResults = new List<PrivacyCommandType>();
            PdmsInfoParser.ParseCapabilities(new[] { "Delete", "Export", "bananas", "none", "View" }, commandTypeResults, true);
            Assert.Equal(new[] { PrivacyCommandType.Delete, PrivacyCommandType.Export }, commandTypeResults);

            var pdmsSubjectResults = new List<PdmsSubjectType>();
            PdmsInfoParser.ParseSubjects(new[] { "Msauser", "AADUSER", "aadUser2", "windows10device", "deviceother", "demographicUSER", "MicrosoftEmployee", "iNVALID", "none", "other" }, pdmsSubjectResults, true);
            Assert.Equal(
                new[]
                {
                    PdmsSubjectType.MSAUser, PdmsSubjectType.AADUser, PdmsSubjectType.AADUser2, PdmsSubjectType.Windows10Device, PdmsSubjectType.DeviceOther, PdmsSubjectType.DemographicUser, PdmsSubjectType.MicrosoftEmployee,
                    PdmsSubjectType.Other
                },
                pdmsSubjectResults);

            var dataTypeResults = new List<DataTypeId>();
            PdmsInfoParser.ParseDataTypes(new[] { "CustomerContact", "BrowsingHistory", "Any", "banana" }, dataTypeResults, true);
            Assert.Equal(
                new[] { Policies.Current.DataTypes.Ids.CustomerContact, Policies.Current.DataTypes.Ids.BrowsingHistory, Policies.Current.DataTypes.Ids.Any },
                dataTypeResults);

            var pcfSubjectResults = new List<PcfSubject>();
            PdmsInfoParser.ParsePcfSubjects(pdmsSubjectResults, pcfSubjectResults, true);
            Assert.Equal(new[] { PcfSubject.Msa, PcfSubject.Aad, PcfSubject.Aad2, PcfSubject.Device, PcfSubject.Device, PcfSubject.Demographic, PcfSubject.MicrosoftEmployee }, pcfSubjectResults);

            var sovereignCloudInstances = new List<CloudInstanceId>();
            PdmsInfoParser.ParseSovereignCloudInstances(new[] { "Public", "Any", "invalid", null, "US.Azure.Fairfax", string.Empty }, sovereignCloudInstances, true);
            Assert.Equal(new[] { Policies.Current.CloudInstances.Ids.Public, Policies.Current.CloudInstances.Ids.US_Azure_Fairfax }, sovereignCloudInstances);
            sovereignCloudInstances.Clear();

            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                PdmsInfoParser.ParseSovereignCloudInstances(null, sovereignCloudInstances, true);
                Assert.False(sovereignCloudInstances.Any());
                sovereignCloudInstances.Clear();

                PdmsInfoParser.ParseSovereignCloudInstances(new string[0], sovereignCloudInstances, true);
                Assert.False(sovereignCloudInstances.Any());
                sovereignCloudInstances.Clear();
            }

            PdmsInfoParser.ParseSovereignCloudInstances(null, sovereignCloudInstances, true);
            Assert.Equal(new[] { Policies.Current.CloudInstances.Ids.All }, sovereignCloudInstances.OrderBy(ci => ci.Value));
            sovereignCloudInstances.Clear();

            PdmsInfoParser.ParseSovereignCloudInstances(new string[0], sovereignCloudInstances, true);
            Assert.Equal(new[] { Policies.Current.CloudInstances.Ids.All }, sovereignCloudInstances.OrderBy(ci => ci.Value));
        }

        /// <summary>
        /// Tests the tolerant parsing logic for AgentReadinessState
        /// </summary>
        [Fact]
        public void TolerantParsingAgentReadinessTests()
        {
            AgentReadinessState state = PdmsInfoParser.ParseAgentReadinessState("TestInProd", true);
            Assert.Equal(AgentReadinessState.TestInProd, state);

            state = PdmsInfoParser.ParseAgentReadinessState("ProdReady", true);
            Assert.Equal(AgentReadinessState.ProdReady, state);

            state = PdmsInfoParser.ParseAgentReadinessState(null, true);
            Assert.Equal(AgentReadinessState.ProdReady, state);

            state = PdmsInfoParser.ParseAgentReadinessState(string.Empty, true);
            Assert.Equal(AgentReadinessState.ProdReady, state);

            state = PdmsInfoParser.ParseAgentReadinessState("invalid", true);
            Assert.Equal(AgentReadinessState.ProdReady, state);
        }

        /// <summary>
        /// Tests the tolerant parsing logic for subjects, data types, and capabilities.
        /// </summary>
        [Fact]
        public void IntolerantParsingTests()
        {
            List<PrivacyCommandType> commandTypeResults = new List<PrivacyCommandType>();
            Assert.Throws<InvalidOperationException>(() => PdmsInfoParser.ParseCapabilities(new[] { "Delete", "Export", "bananas", "none" }, commandTypeResults, false));

            commandTypeResults = new List<PrivacyCommandType>();
            PdmsInfoParser.ParseCapabilities(new[] { "Delete", "Export", "View", "AgeOut" }, commandTypeResults, true);
            Assert.Equal(new[] { PrivacyCommandType.Delete, PrivacyCommandType.Export, PrivacyCommandType.AgeOut }, commandTypeResults);

            var pdmsSubjectResults = new List<PdmsSubjectType>();
            Assert.Throws<InvalidOperationException>(
                () => PdmsInfoParser.ParseSubjects(
                    new[] { "Msauser", "AADUSER", "windows10device", "deviceother", "demographicUSER", "iNVALID", "none", "other" },
                    pdmsSubjectResults,
                    false));

            var dataTypeResults = new List<DataTypeId>();
            Assert.Throws<InvalidOperationException>(() => PdmsInfoParser.ParseDataTypes(new[] { "CustomerContact", "BrowsingHistory", "Any", "banana" }, dataTypeResults, false));

            var pcfSubjectResults = new List<PcfSubject>();
            Assert.Throws<InvalidOperationException>(() => PdmsInfoParser.ParsePcfSubjects(new[] { (PdmsSubjectType)(-1) }, pcfSubjectResults, false));

            using (new FlightEnabled(FlightingNames.CloudInstanceConfigMissingFallbackDisabled))
            {
                var supportedCloudInstances = new List<CloudInstanceId>();
                Assert.Throws<InvalidOperationException>(
                    () => PdmsInfoParser.ParseSovereignCloudInstances(new[] { "Any", "All", "invalid", null }, supportedCloudInstances, false));
                Assert.Throws<InvalidOperationException>(() => PdmsInfoParser.ParseSovereignCloudInstances(new string[0], supportedCloudInstances, false));
                Assert.Throws<InvalidOperationException>(() => PdmsInfoParser.ParseSovereignCloudInstances(null, supportedCloudInstances, false));

                Assert.Throws<InvalidOperationException>(() => { PdmsInfoParser.ParseDeploymentLocation("Any", false); });
                Assert.Throws<InvalidOperationException>(() => { PdmsInfoParser.ParseDeploymentLocation(string.Empty, false); });
                Assert.Throws<InvalidOperationException>(() => { PdmsInfoParser.ParseDeploymentLocation(null, false); });
            }
        }

        /// <summary>
        /// Conversion from User SubjectTypes to CommandSubject types
        /// </summary>
        [Theory]
        [InlineData("AADUser", PcfSubject.Aad, PdmsSubjectType.AADUser)]
        [InlineData("AADUser2", PcfSubject.Aad2, PdmsSubjectType.AADUser2)]
        [InlineData("DemographicUser", PcfSubject.Demographic, PdmsSubjectType.DemographicUser)]
        [InlineData("MicrosoftEmployee", PcfSubject.MicrosoftEmployee, PdmsSubjectType.MicrosoftEmployee)]
        [InlineData("DeviceOther", PcfSubject.Device, PdmsSubjectType.DeviceOther)]
        [InlineData("MSAUser", PcfSubject.Msa, PdmsSubjectType.MSAUser)]
        [InlineData("Xbox", PcfSubject.Msa, PdmsSubjectType.Xbox)]
        [InlineData("Windows10Device", PcfSubject.Device, PdmsSubjectType.Windows10Device)]
        [InlineData("NonWindowsDevice", PcfSubject.NonWindowsDevice, PdmsSubjectType.NonWindowsDevice)]
        [InlineData("EdgeBrowser", PcfSubject.EdgeBrowser, PdmsSubjectType.EdgeBrowser)]
        public void SubjectTypeTest(
            string subject,
            PcfSubject? expectedCommonSubject,
            PdmsSubjectType expectedPdmsSubject)
        {
            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParsePdmsSubjectType(subject, out PdmsSubjectType pdmsSubject));
            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParsePcfSubject(pdmsSubject, out PcfSubject pcfSubject));

            Assert.Equal(expectedPdmsSubject, pdmsSubject);
            Assert.Equal(expectedCommonSubject, pcfSubject);
        }

        /// <summary>
        /// Conversion of ignored subject types.
        /// </summary>
        [Theory]
        [InlineData("Other")]
        public void IgnoredSubjectTypeTest(string subject)
        {
            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParsePdmsSubjectType(subject, out PdmsSubjectType pdmsSubject));
            Assert.Equal(PdmsInfoParserResult.Ignore, PdmsInfoParser.TryParsePcfSubject(pdmsSubject, out PcfSubject pcfSubject));
        }

        /// <summary>
        /// Conversion of sovereign cloud Id.
        /// </summary>
        [Fact]
        public void CloudInstanceIdTest()
        {
            CloudInstanceId cloudId;
            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParseCloudInstanceId("Public", out cloudId));
            Assert.Equal(Policies.Current.CloudInstances.Ids.Public, cloudId);

            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParseCloudInstanceId("CN.Azure.Mooncake", out cloudId));
            Assert.Equal(Policies.Current.CloudInstances.Ids.CN_Azure_Mooncake, cloudId);

            Assert.Equal(PdmsInfoParserResult.Success, PdmsInfoParser.TryParseCloudInstanceId("US.Azure.Fairfax", out cloudId));
            Assert.Equal(Policies.Current.CloudInstances.Ids.US_Azure_Fairfax, cloudId);

            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParseCloudInstanceId(null, out cloudId));
            Assert.Null(cloudId);

            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParseCloudInstanceId(string.Empty, out cloudId));
            Assert.Null(cloudId);

            Assert.Equal(PdmsInfoParserResult.Failure, PdmsInfoParser.TryParseCloudInstanceId("invalid", out cloudId));
            Assert.Null(cloudId);
        }
    }
}
