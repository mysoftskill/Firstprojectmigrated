namespace PCF.UnitTests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.ServiceModel.Configuration;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class DefenderTests
    {
        private readonly Defender defender = new Defender();

        public DefenderTests()
        {
            Config.Instance.Worker.DefenderAPIKey = "key";
            Config.Instance.Worker.DefenderFileMetaDataServiceUri = "https://fms-test.dummy";
        }

        [Fact]
        public void ShouldReturnCorrectSha()
        {
            var bytes = new MemoryStream(Encoding.ASCII.GetBytes("test this content with AV Scan"));

            var sha = defender.ComputeSha256Hash(bytes);

            Assert.Equal("9EE3AEFC41F958D861FE4FAAD25814F5C81F8831CFDE55AC05AFD5C3199F5342".ToUpperInvariant(), sha.ToUpperInvariant());
        }

        [Fact]
        public async void ValidateMalwareNotFoundCase()
        {
            var expectedResult = new DefenderScanResult
            {
                IsMalware = false
            };

            var defenderMock = new Mock<IDefender>();
            defenderMock.Setup(x => x.GetScanResultAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(expectedResult));
            var actualResult = await defenderMock.Object.ScanForMalwareAsync(new MemoryStream(Encoding.UTF8.GetBytes("abcd")), CancellationToken.None);

            Assert.Equal(expectedResult.IsMalware, actualResult.IsMalware);
        }

        [Fact]
        public async void ValidateMalwareFoundCase()
        {
            var expectedResult = new DefenderScanResult
            {
                IsMalware = true
            };

            var defenderMock = new Mock<IDefender>();
            defenderMock.Setup(x => x.GetScanResultAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(expectedResult));
            var actualResult = await defenderMock.Object.ScanForMalwareAsync(new MemoryStream(Encoding.UTF8.GetBytes("abcd")), CancellationToken.None);

            Assert.Equal(expectedResult.IsMalware, actualResult.IsMalware);            
        }

        [Fact]
        public async void ValidateSampleUploadAndShaGeneration()
        {
            var expectedResult = new DefenderScanResult
            {
                IsMalware = false
            };

            var defenderMock = new Mock<IDefender>();
            var sourceBytes = new MemoryStream(Encoding.UTF8.GetBytes("abcd"));
            defenderMock.Setup(x => x.GetScanResultAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.FromResult(expectedResult));
            defenderMock.Setup(x => x.ComputeSha256Hash(sourceBytes)).Returns("88D4266FD4E6338D13B845FCF289579D209C897823B9217DA3E161936F031589");
            var actualResult = await defenderMock.Object.ScanForMalwareAsync(sourceBytes, CancellationToken.None);

            Assert.Equal(expectedResult.IsMalware, actualResult.IsMalware);

            // Do not upload file to AVaaS - This is expected behavior until AVaaS System improves performance
            // We are going with assumption that if SHA is not found in AVaaS database, its clean
            defenderMock.Verify(m => m.GetScanResultAsync("88D4266FD4E6338D13B845FCF289579D209C897823B9217DA3E161936F031589", CancellationToken.None), Times.Once);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async void ValidateInvalidScanResponseReturnsNull(HttpStatusCode statusCode)
        {
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Result should be null for bad response codes, this would allow the flow to retry
            Assert.Null(result);
        }

        [Fact]
        public async void ValidateV1WithNoDeterminationResponse_ShouldRelyOnMsAMPreRelRel()
        {
            // Response contains clean
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        new TestResponseObject.V1Def("NoDetermination", new TestResponseObject.StaticScanResultDef(
                                new TestResponseObject.StaticScanResultDef.MsAmPreRelRelDef("Any value"))),
                        null,
                        null),
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsMalware);
            Assert.Equal(DeterminationType.V1StaticScanResultMsAmPreRelRelFound, result.DeterminationType);
        }

        [Fact]
        public async void ValidateV1DeterminationAvailable_WithCleanResponse()
        {
            // Response contains clean
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        new TestResponseObject.V1Def("clean", null),
                        new TestResponseObject.StatesDef(new TestResponseObject.StaticScan("3")),
                        null), 
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(result.IsMalware);
            Assert.Equal(DeterminationType.V1DeterminationValueFound, result.DeterminationType);
        }

        [Fact]
        public async void ValidateV1DeterminationAvailable_WithMalwareResponse()
        {
            string[] malwareFlags = new string[] { "Malware", "MalwareContainer", "AutomationMalware", "AutomationPUA", "UWS", "AutomationGrayware", "AutomationUWS", "ClassifiedPUA", "Spyware", "SpywareContainer", "PUAContainer" };

            // V1.DeterminationType - Contains Random Malware flag and States flags is not present
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        new TestResponseObject.V1Def(malwareFlags[RandomHelper.Next(0, malwareFlags.Length - 1)], null),
                        null,
                        null),
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsMalware);
            Assert.Equal(DeterminationType.V1DeterminationValueFound, result.DeterminationType);
        }

        [Fact]
        public async void DefenderResponse_V1DeterNotAvailable_ShouldRelyOnMsAmPreRelRel()
        {
            // Contains no malware flag and States flags is not present
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        new TestResponseObject.V1Def(
                            string.Empty, 
                            new TestResponseObject.StaticScanResultDef(
                                new TestResponseObject.StaticScanResultDef.MsAmPreRelRelDef(string.Empty))),
                        null,
                        null),
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(result.IsMalware);

            // Contains Malware flag
            var malwareFoundResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        new TestResponseObject.V1Def(
                            string.Empty,
                            new TestResponseObject.StaticScanResultDef(
                                new TestResponseObject.StaticScanResultDef.MsAmPreRelRelDef("Any value"))),
                        null,
                        null),
                    new JsonMediaTypeFormatter())
            };

            var malwareFoundResult = await defender.ProcessScanResponseAsync(malwareFoundResponse, CancellationToken.None);

            Assert.NotNull(malwareFoundResult);
            Assert.True(malwareFoundResult.IsMalware);
            Assert.Equal(DeterminationType.V1StaticScanResultMsAmPreRelRelFound, result.DeterminationType);
        }

        [Fact]
        public async void DefenderResponse_MsAmPreRelRelNotAvailable_ShouldRelyOnVTScan()
        {
            // Contains no malware flag and States flags is not present
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        null,
                        new TestResponseObject.EXDef(
                            new TestResponseObject.EXDef.FeedsDef(
                                new TestResponseObject.EXDef.VTDef(
                                    new TestResponseObject.EXDef.StaticScanResultsDef(string.Empty))))),
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            Assert.NotNull(result);
            Assert.False(result.IsMalware);

            // Contains malware flag and States flags is not present
            var malwareResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        null,
                        new TestResponseObject.EXDef(
                            new TestResponseObject.EXDef.FeedsDef(
                                new TestResponseObject.EXDef.VTDef(
                                    new TestResponseObject.EXDef.StaticScanResultsDef("Some malware text"))))),
                    new JsonMediaTypeFormatter())
            };

            var malwareResult = await defender.ProcessScanResponseAsync(malwareResponse, CancellationToken.None);

            Assert.NotNull(malwareResult);
            Assert.True(malwareResult.IsMalware);
            Assert.Equal(DeterminationType.EXFeedsVTFound, result.DeterminationType);
        }

        [Fact]
        public async void DefenderResponse_ResultNotReadilyAvailable()
        {
            // Result not readily available
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        null,
                        null),
                    new JsonMediaTypeFormatter())
            };

            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Ensure null is returned; Indicating retry
            Assert.Null(result);

            // Result not readily available 
            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        new TestResponseObject.StatesDef(null),
                        null),
                    new JsonMediaTypeFormatter())
            };

            result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Ensure null is returned; Indicating retry
            Assert.Null(result);

            // Result not readily available
            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        new TestResponseObject.StatesDef(new TestResponseObject.StaticScan(null)),
                        null),
                    new JsonMediaTypeFormatter())
            };

            result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Ensure null is returned; Indicating retry
            Assert.Null(result);

            // Result contains Failed Status - Should not retry
            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<TestResponseObject>(
                    new TestResponseObject(
                        null,
                        new TestResponseObject.StatesDef(new TestResponseObject.StaticScan("4")),
                        null),
                    new JsonMediaTypeFormatter())
            };

            result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Static Scan 4 means Stop retrying, error occured; hence assume clean
            Assert.NotNull(result);
            Assert.False(result.IsMalware);
            Assert.Equal(DeterminationType.ScanFailed, result.DeterminationType);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        public async void ValidateSHANotFoundScanResponse(HttpStatusCode statusCode)
        {
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            var result = await defender.ProcessScanResponseAsync(response, CancellationToken.None);

            // Assume no malware when SHA not found
            Assert.False(result.IsMalware);
            Assert.Equal(DeterminationType.ShaNotFound, result.DeterminationType);
        }
    }

    /// <summary>
    /// Dummy class for rest response
    /// </summary>
    public class TestResponseObject
    {
        public TestResponseObject(V1Def v1Value, StatesDef states, EXDef ex)
        {
            V1 = v1Value;
            States = states;
            EX = ex;
        }

        public V1Def V1 { get; set; }

        public StatesDef States { get; set; }

        public EXDef EX { get; set; }

        public class StatesDef
        {
            public StatesDef(StaticScan scan)
            {
                StaticScan = scan;
            }

            public StaticScan StaticScan { get; set; }
        }

        public class StaticScan
        {
            public StaticScan(string state)
            {
                State = state;
            }

            public string State { get; set; }
        }

        public class V1Def
        {
            public V1Def(string determinationValue, StaticScanResultDef staticScanResult)
            {
                DeterminationValue = determinationValue;
                StaticScanResult = staticScanResult;
            }

            public string DeterminationValue { get; set; }

            public StaticScanResultDef StaticScanResult { get; set; }
        }

        public class StaticScanResultDef
        {
            public StaticScanResultDef(MsAmPreRelRelDef msAmPreRelRel)
            {
                this.MsAmPreRelRel = msAmPreRelRel;
            }

            public MsAmPreRelRelDef MsAmPreRelRel { get; set; }

            public class MsAmPreRelRelDef
            {
                public MsAmPreRelRelDef(string result)
                {
                    this.Result = result;
                }

                public string Result { get; set; }
            }
        }

        public class EXDef
        {
            public EXDef(FeedsDef feeds)
            {
                Feeds = feeds;
            }

            public FeedsDef Feeds { get; set; }

            public class FeedsDef
            {
                public VTDef VT { get; set; }

                public FeedsDef(VTDef vt)
                {
                    VT = vt;
                }
            }

            public class VTDef
            {
                public StaticScanResultsDef StaticScanResults { get; set; }

                public VTDef(StaticScanResultsDef staticScanResults)
                {
                    StaticScanResults = staticScanResults;
                }
            }

            public class StaticScanResultsDef
            {
                public string Microsoft { get; set; }

                public StaticScanResultsDef(string microsoft)
                {
                    Microsoft = microsoft;
                }
            }
        }
    }
}
