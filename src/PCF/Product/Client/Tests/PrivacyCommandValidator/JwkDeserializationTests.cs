namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    [TestClass]
    public class JwkDeserializationTests
    {
        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public void RsaJwk_ParseKe_NullKeyId_ThrowsException()
        {
            // Arrange
            string inputWithNullKeyId =
                @"{""kty"":""RSA"",""use"":""sig"",""x5t"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""n"":""r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w"",""e"":""AQAB"",""x5c"":[""MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM=""]}";

            // Act and assert
            var key = JsonWebKey.ParseKey(inputWithNullKeyId) as RsaJsonWebKey;
            Assert.IsNull(key);
        }

        [TestMethod]
        public void RsaJwk_ParseKey_NoPublicKeyUse_ReturnsRsaJwk()
        {
            // Arrange
            string input =
                @"{""kty"":""RSA"",""kid"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""x5t"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""n"":""r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w"",""e"":""AQAB"",""x5c"":[""MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM=""]}";
            var expected = new RsaJsonWebKey
            {
                KeyType = JwkKeyType.RSA,
                KeyId = "hOmo0Ou8tkZz9aD9ZrYP0fnskco",
                X509Thumbprint = "hOmo0Ou8tkZz9aD9ZrYP0fnskco",
                X509Chain = new[]
                {
                    "MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM="
                },
                Modulus =
                    "r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w",
                Exponent = "AQAB"
            };

            // Act
            var result = JsonWebKey.ParseKey(input) as RsaJsonWebKey;

            // Assert
            AssertRsaJwtObjectsAreEqual(expected, result);
        }

        [TestMethod]
        public void RsaJwk_ParseKey_ReturnsRsaJwk()
        {
            // Arrange
            string input =
                @"{""kty"":""RSA"",""use"":""sig"",""kid"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""x5t"":""hOmo0Ou8tkZz9aD9ZrYP0fnskco"",""n"":""r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w"",""e"":""AQAB"",""x5c"":[""MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM=""]}";
            var expected = new RsaJsonWebKey
            {
                KeyType = JwkKeyType.RSA,
                PublicKeyUse = JwkKeyUse.Signature,
                KeyId = "hOmo0Ou8tkZz9aD9ZrYP0fnskco",
                X509Thumbprint = "hOmo0Ou8tkZz9aD9ZrYP0fnskco",
                X509Chain = new[]
                {
                    "MIIDTDCCAjSgAwIBAgIJAIYiIj5YRpALMA0GCSqGSIb3DQEBCwUAMBgxFjAUBgNVBAMTDVBhc3Nwb3J0IEdEUFIwHhcNMTcxMTAzMjAwMjI0WhcNMjIxMTAyMjAwMjI0WjAYMRYwFAYDVQQDEw1QYXNzcG9ydCBHRFBSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAr9vdYPKcafCVWok2j3+h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu+SQ+ZpB5VzpxBvtDPZJNiUVWb0C+cB9FcySVWmDMjiSh/cy+0Pvg7Th4ma/SZ+snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM+U4Uh7uLMtR5/+L4ihzawCr+30aNBcBDb6S7yABpyzmTDDaZrnE/sXhYxeU+v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6wIDAQABo4GYMIGVMB0GA1UdDgQWBBSoTh2DpPrYWB+YyF9xH4t7t9JwmTBIBgNVHSMEQTA/gBSoTh2DpPrYWB+YyF9xH4t7t9JwmaEcpBowGDEWMBQGA1UEAxMNUGFzc3BvcnQgR0RQUoIJAIYiIj5YRpALMAsGA1UdDwQEAwIE8DAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDQYJKoZIhvcNAQELBQADggEBAGolx0gP+yPhyiyZWfYcMvy7y1HmhxQ8b10hMxkK+MNOyYjl83GunE8Dhi23qMHT7O3zXpxyr6WL1VKa+dApQfTcTxLjbAR8/4jjhcVh3i6j6AyF9gqEbZisf378iYgWYXytDmglYazoi8XfLKDu5AS0D8iLB8h7FmQ2N+HPuhd7inOtJCSWzZrMVEn64b0NaWec56vaXutoWVIKErJBiriKxPvsQvXiuZDBnu+P4ZQ9cdn6ZMV/9BN/p5HI9UP5TWPVT5PVO6IFjQrym67vTMZMbNeymndha79SdpBkjR0t3+2/TzBZ9PKXP2Dikfl/hP4uKRf29KyqoufQkkb0IqM="
                },
                Modulus =
                    "r9vdYPKcafCVWok2j3-h6DKmSsWw0iYQwIjcxC3uoTUzmH5p3RKZeH4TqlQOiE8zBmWEvAfEImN5N4gtswntXIK6DM9yzepzxUbXYxHbyhiQtFRFHUDT7Bu-SQ-ZpB5VzpxBvtDPZJNiUVWb0C-cB9FcySVWmDMjiSh_cy-0Pvg7Th4ma_SZ-snyIXEDE7xjdd6A3DgL5KWaz8at7iw1UpAN8XLhm22QGTjaDHH8Notw243EM-U4Uh7uLMtR5_-L4ihzawCr-30aNBcBDb6S7yABpyzmTDDaZrnE_sXhYxeU-v9949I4Cw3b4VM7nkMxPANpNkhlXvQYx0apMKJx6w",
                Exponent = "AQAB"
            };

            // Act
            var result = JsonWebKey.ParseKey(input) as RsaJsonWebKey;

            // Assert
            AssertRsaJwtObjectsAreEqual(result, expected);
        }

        private static void AssertRsaJwtObjectsAreEqual(RsaJsonWebKey expected, RsaJsonWebKey actual)
        {
            Assert.AreEqual(expected.KeyType, actual.KeyType);
            Assert.AreEqual(expected.KeyId, actual.KeyId);
            CollectionAssert.AreEqual(expected.X509Chain, actual.X509Chain);
            Assert.AreEqual(expected.Modulus, actual.Modulus);
            Assert.AreEqual(expected.Exponent, actual.Exponent);

            if (expected.PublicKeyUse.HasValue)
            {
                Assert.AreEqual(expected.PublicKeyUse.Value, actual.PublicKeyUse.Value);
            }
        }
    }
}
