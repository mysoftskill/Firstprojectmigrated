// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;

    using Moq;

    using Newtonsoft.Json;

    internal class MockJsonKeyCache : IMocked<ICache>
    {
        private const string RawJwkDocument = @"{""keys"":[{""kty"":""RSA"",""use"":""sig"",""kid"":""1HX039rD8w5wYfwMqJTJV4BHrTE"",""x5t"":""1HX039rD8w5wYfwMqJTJV4BHrTE"",""n"":""qQYrp9W4uuavlmcDxqAHRPz-mONE_avJrxY8oo8aIStiPAqr0R836I2YMT2SwKShHhlKuFIKOlnfbQcnVsUJFjqByJBjofJRgtoY9wcIRP0SEBZZ5YNymlTQZdB88-b-1JlhCI7Upx6ZY63qjxfUxV_GzgAvdAi-ozUgx8d6cO7FOR58vVT8GR_nzYvQC8r8HVaAycYhIQ_XIQcHWVjrD67zE3PKPhEssmE0pD0v9eX0Hfx4RxolgHJiSzbBpAfDxkjlYDL98wvKQXjONU_0RYAvHYkUrbnQUWVy3eVeqjzwc5cDRHAZrg_i5Xj98R-OLZbtf2lvoWd6J25F0Z1chQ"",""e"":""AQAB"",""x5c"":[""MIIDTDCCAjSgAwIBAgIJAP5TkbopF2YuMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMzM4WhcNMjIxMTAyMjAwMzM4WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqQYrp9W4uuavlmcDxqAHRPz+mONE/avJrxY8oo8aIStiPAqr0R836I2YMT2SwKShHhlKuFIKOlnfbQcnVsUJFjqByJBjofJRgtoY9wcIRP0SEBZZ5YNymlTQZdB88+b+1JlhCI7Upx6ZY63qjxfUxV/GzgAvdAi+ozUgx8d6cO7FOR58vVT8GR/nzYvQC8r8HVaAycYhIQ/XIQcHWVjrD67zE3PKPhEssmE0pD0v9eX0Hfx4RxolgHJiSzbBpAfDxkjlYDL98wvKQXjONU/0RYAvHYkUrbnQUWVy3eVeqjzwc5cDRHAZrg/i5Xj98R+OLZbtf2lvoWd6J25F0Z1chQIDAQABo4GYMIGVMB0GA1UdDgQWBBRfErqAUYhM38wMu4eryX0qXjR8JDBIBgNVHSMEQTA/gBRfErqAUYhM38wMu4eryX0qXjR8JKEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAP5TkbopF2YuMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAA5XFbbMzOzDcSbsi0h5VBnrDphzjid04qEc2qY9fWy1lGjjPxWKjPufn1/WTO/bqsmUXRJoh/6jONtMtE2WLRG+mXeB3zvgA6pw8XACJt6YzIl2tHWcD1ts25TU1qJ+PZGd1SgWK9UHqCBSyoHHCmeBaVmif3qjD+ziPVozMj+1KX3IrBF0OTTsVSMo0l4hlQlSsdg0SN87xNQMqDRljYlbmQgEbK6+nF8iYspjgSzG5hwVXsVWtlqTLOcEF00seFrF3j+ZpDt+v/b7D5D/MrbCwsHsXUbH1XI0rENGhRKI+T33stEHhjnyuvxAlx2J/I82bmpZbKbVO8elRaVZQ24=""]},{""kty"":""RSA"",""use"":""sig"" ,""kid"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""x5t"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""n"":""r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w"",""e"":""AQAB"",""x5c"":[""MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM=""]}]}";

        private readonly Dictionary<string, CacheItem> documentCache;

        /// <inheritdoc />
        public Mock<ICache> Mock { get; }

        public MockJsonKeyCache()
        {
            this.Mock = new Mock<ICache>();

            // Setup
            this.Mock.Setup(cache => cache.ReadAsync(It.IsAny<string>())).ReturnsAsync((string s) => this.documentCache[s]);
            this.Mock.Setup(cache => cache.WriteAsync(It.IsAny<Dictionary<string, CacheItem>>(), It.IsAny<CancellationToken>()));

            // Pre-load values
            this.documentCache = JsonConvert.DeserializeObject<JwkDocument>(RawJwkDocument).Keys.Select(k => new CacheItem(k, true, DateTimeOffset.MaxValue)).ToDictionary(ci => ci.Item.KeyId);
        }

        /// <inheritdoc />
        Mock IMocked.Mock => this.Mock;
    }
}
