namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Xunit;

    /// <summary>
    /// Unit tests for JsonToCsvWriter.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class JsonToCsvWriterTests
    {
        /// <summary>
        /// Verify special characters are properly escaped. 
        /// </summary>
        [Theory]
        [InlineData("va,lue,value", "\"va,lue,value\"")]
        [InlineData("va\"l\"ue", "\"va\"\"l\"\"ue\"")]
        [InlineData("va/lue", "\"va/lue\"")]
        [InlineData("val\nue", "\"val\nue\"")]
        public async Task ValidateEscapedCharacters(string value, string expectedEscapedValue)
        {
            dynamic item = new
            {
                prop = value
            };

            var data = JsonConvert.SerializeObject(item);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    // Read past headers
                    reader.ReadLine();

                    // Read results
                    var result = reader.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                    Assert.Equal(expectedEscapedValue, result);
                }
            }
        }

        /// <summary>
        /// Verify headers are composed correctly.
        /// </summary>
        [Fact]
        public async Task ValidateComposeHeaders()
        {
            List<dynamic> values = new List<dynamic>()
            {
                new {
                    id = "00",
                    user = new {
                        id = "10",
                        phone = "11-11",
                        country = "usa",
                    }
                },
                new {
                    id = "01",
                    user = new {
                        id = "11",
                        phone = "22-22",
                        region = "us"
                    },
                    product = new {
                        id = "101",
                        state = "wa",
                    },
                    tag = "vip",
                    categories = new dynamic[]
                    {
                        new
                        {
                            name = "Dev",
                            code = 111
                        },
                        new
                        {
                            name = "Test"
                        }
                    }
                }
            };

            string expectedHeaders = "id,\"user/id\",\"user/phone\",\"user/country\",\"user/region\",\"product/id\",\"product/state\",tag,\"categories/0/name\",\"categories/0/code\",\"categories/1/name\"";

            var value = JsonConvert.SerializeObject(values);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    var headers = reader.ReadLine();
                    Assert.Equal(expectedHeaders, headers);
                }
            }
        }

        /// <summary>
        /// Verify records are written correctly.
        /// </summary>
        [Fact]
        public async Task ValidateWrite()
        {
            List<dynamic> values = new List<dynamic>()
            {
                new {
                    Name = "record 1",
                    id = "101",
                    user = new {
                        id = "user101",
                        country = "usa",
                    }
                },
                new {
                    Name = "record 2",
                    id = "102",
                    user = new {
                        id = "user102",
                        country = "brazil",
                    },
                    product = new {
                        id = "product 102",
                        country = "usa",
                        region = "us",
                    }
                },
                new {
                    Name = "record 3",
                    id = "103",
                    product = new {
                        id = "product 103",
                        country = "germany",
                        region = "europe",
                    },
                },
                new {
                    Name = "record 4",
                    id = "104",
                    user = new {
                        id = "user104",
                        country = "japan",
                    },
                    product = new {
                        id = "product 104",
                        region = "asia",
                    },
                    properties = new
                    {
                        tag = "toy",
                        time = "Monday, June 15, 2009 1:45 PM"
                    }
                }
            };

            var expectedResults = new List<string>()
            {
                "Name,id,\"user/id\",\"user/country\",\"product/id\",\"product/country\",\"product/region\",\"properties/tag\",\"properties/time\"",
                "record 1,101,user101,usa,,,,,",
                "record 2,102,user102,brazil,product 102,usa,us,,",
                "record 3,103,,,product 103,germany,europe,,",
                "record 4,104,user104,japan,product 104,,asia,toy,\"Monday, June 15, 2009 1:45 PM\""
            };

            var value = JsonConvert.SerializeObject(values);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;

                    foreach (var expectedResult in expectedResults)
                    {
                        var result = reader.ReadLine();
                        Assert.Equal(expectedResult, result);
                    }
                }
            }
        }

        /// <summary>
        /// Verify simple array values.
        /// </summary>
        [Fact]
        public async Task ValidateArrayValues()
        {
            dynamic item = new
            {
                id = "101",
                countries = new string[] { "usa", "brazil", "japan", "germany" }
            };

            var expectedResult = "101,usa,brazil,japan,germany";

            var value = JsonConvert.SerializeObject(item);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    reader.ReadLine();
                    var result = reader.ReadLine();

                    Assert.Equal(expectedResult, result);
                }
            }
        }

        /// <summary>
        /// Verify JSON containing nested types deeper than two levels.
        /// </summary>
        [Fact]
        public async Task ValidateNestedType()
        {
            dynamic item = new
            {
                id = "101",
                user = new
                {
                    name = "bob",
                    contact = new
                    {
                        email = "bob@test.com",
                        region = new
                        {
                            code = 11,
                            abbreviation = "WA"
                        }
                    }
                }
            };

            var expectedResult = "101,bob,bob@test.com,11,WA";

            var value = JsonConvert.SerializeObject(item);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    reader.ReadLine();
                    var result = reader.ReadLine();

                    Assert.Equal(expectedResult, result);
                }
            }
        }

        /// <summary>
        /// Verify writer with complex array values.
        /// </summary>
        [Fact]
        public async Task ValidateComplexArrayValues()
        {
            dynamic item = new
            {
                id = "101",
                countries = new dynamic[] {
                    new {
                        name = "usa",
                        code = 222
                    },
                    new {
                        name = "brazil",
                        code = 800
                    },
                    new {
                        name = "japan",
                        code = 222
                    },
                    new {
                        name = "germany",
                        code = 345
                    }
                }
            };

            var expectedResult = "101,usa,222,brazil,800,japan,222,germany,345";

            var value = JsonConvert.SerializeObject(item);
            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await writer.WriteAsync(CancellationToken.None).ConfigureAwait(false);
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    reader.ReadLine();
                    var result = reader.ReadLine();

                    Assert.Equal(expectedResult, result);
                }
            }
        }

        /// <summary>
        /// Verify writer throws for invalid JSON.
        /// </summary>
        [Fact]
        public async Task ValidateThrowInvalidJson()
        {
            var value = "[{time: value, correlationId:}]";

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            using (var destinationStream = new MemoryStream())
            using (var writer = new JsonToCsvWriter(sourceStream, destinationStream))
            {
                await Assert.ThrowsAsync<JsonReaderException>(() => writer.WriteAsync(CancellationToken.None)).ConfigureAwait(false);
            }
        }
    }
}
