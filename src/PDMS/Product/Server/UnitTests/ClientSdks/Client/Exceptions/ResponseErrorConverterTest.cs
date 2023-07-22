namespace Microsoft.PrivacyServices.DataManagement.Client.Exceptions.UnitTest
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.DataManagement.Client;

    using Newtonsoft.Json;

    using Xunit;

    public class ResponseErrorConverterTest
    {
        [Fact(DisplayName = "When the minimal set of properties are returned, then parse successfully.")]
        public void VerifyMinimumProperties()
        {
            var json = "{ \"error\": { \"code\": \"c\", \"message\": \"m\" } }";
            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);
            Assert.Equal("c", responseError.Code);
            Assert.Equal("m", responseError.Message);
        }

        [Fact(DisplayName = "When details are returned, then parse successfully.")]
        public void VerifyDetails()
        {
            var json = @"
                {
                  ""error"": {
                    ""code"": ""c"",
                    ""message"": ""m"",
                    ""details"": [
                      {""code"": ""c1"", ""message"": ""m1"", ""target"": ""t1""},
                      {""code"": ""c2"", ""message"": ""m2""},
                    ]
                  }
                }";

            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);

            Assert.Equal("c1", responseError.Details[0].Code);
            Assert.Equal("m1", responseError.Details[0].Message);
            Assert.Equal("t1", responseError.Details[0].Target);

            Assert.Equal("c2", responseError.Details[1].Code);
            Assert.Equal("m2", responseError.Details[1].Message);
            Assert.Null(responseError.Details[1].Target);
        }

        [Fact(DisplayName = "When an inner error is returned, then parse successfully.")]
        public void VerifyInnerError()
        {
            // Multiple inner errors with different data types and complex objects.
            var json = @"
            {
                ""error"": {
                    ""code"": ""c"",
                    ""message"": ""m"",
                    ""innererror"": {
                        ""code"": ""c1"",
                        ""custom"": 1,
                        ""object"": {
                            ""value"": 2.5
                        },
                        ""innererror"": {
                            ""code"": ""c2"",
                            ""array"": [true, false]
                        }
                    }
                }
            }";

            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);

            Assert.Equal("c1", responseError.InnerError.Code);
            Assert.Equal(1, (long)responseError.InnerError.Data["custom"]);

            var obj = responseError.InnerError.Data["object"] as IDictionary<string, object>;
            Assert.Equal(2.5, (double)obj["value"]);

            Assert.Equal("c2", responseError.InnerError.InnerError.Code);

            var data = responseError.InnerError.InnerError.Data["array"] as IEnumerable<object>;
            var dataValue = data.ToArray();

            Assert.True((bool)dataValue[0]);
            Assert.False((bool)dataValue[1]);
        }

        [Fact(DisplayName = "When an inner error has a 2D array, then parse successfully.")]
        public void VerifyNestedArray()
        {
            var json = @"
            {
                ""error"": {
                    ""code"": ""c"",
                    ""message"": ""m"",
                    ""innererror"": {
                        ""code"": ""c2"",
                        ""array"": [[true, false], [false, true]]
                    }
                }
            }";

            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);
            var data = (responseError.InnerError.Data["array"] as IEnumerable<object>).ToArray();

            var data1 = (data[0] as IEnumerable<object>).ToArray();
            Assert.True((bool)data1[0]);
            Assert.False((bool)data1[1]);

            var data2 = (data[1] as IEnumerable<object>).ToArray();
            Assert.False((bool)data2[0]);
            Assert.True((bool)data2[1]);
        }

        [Fact(DisplayName = "When there is a comment, then parse successfully.")]
        public void VerifyComments()
        {
            var json = @"
            {
                ""error"": { // Comment.
                    ""code"": // Comment.
                        ""c"", // Comment.
                    ""message"":  // Comment.
                        ""m"", // Comment.
                    ""innererror"": // Comment.
                    {
                        ""code"": ""c2"", // Comment.
                        ""array"": // Comment.
                            [[true, false], // Comment.
                            [false, true] // Comment.
                        ] // Comment.
                    } // Comment.
                }
            }";

            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);
            var data = (responseError.InnerError.Data["array"] as IEnumerable<object>).ToArray();

            var data1 = (data[0] as IEnumerable<object>).ToArray();
            Assert.True((bool)data1[0]);
            Assert.False((bool)data1[1]);

            var data2 = (data[1] as IEnumerable<object>).ToArray();
            Assert.False((bool)data2[0]);
            Assert.True((bool)data2[1]);
        }

        [Fact(DisplayName = "When there is an empty array or object, then parse successfully.")]
        public void VerifyEmptyValues()
        {
            var json = @"
            {
                ""error"": { 
                    ""code"": ""c"",
                    ""message"": ""m"",
                    ""innererror"": {
                        ""code"": ""c2"",
                        ""array"": [],
                        ""object"": {}
                    }
                }
            }";

            var responseError = JsonConvert.DeserializeObject<ResponseError>(json, SerializerSettings.Instance);
            Assert.Empty(responseError.InnerError.Data["array"] as IEnumerable<object>);
            Assert.Empty(responseError.InnerError.Data["object"] as IDictionary<string, object>);
        }
    }
}
