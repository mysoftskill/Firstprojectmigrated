namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    /// <summary>
    /// Converts a Base64Url.
    /// </summary>
    /// <remarks>
    /// Please refer to RFC7515 appendix C.
    /// <see ref="https://tools.ietf.org/html/rfc7515#appendix-C" />
    /// </remarks>
    public static class Base64Url
    {
        /// <summary>
        /// Decode the base64url.
        /// </summary>
        /// <param name="input">The base64url encoded string</param>
        /// <param name="loggableInformation">Loggable to provide information about the command and verifier in exceptions</param>
        /// <returns>The byte array for the input</returns>
        internal static byte[] Decode(string input, LoggableInformation loggableInformation)
        {
            string s = input;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding

            // Pad with trailing '='s
            switch (s.Length % 4)
            {
                case 0: break; // No pad chars in this case
                case 2:
                    s += "==";
                    break; // Two pad chars
                case 3:
                    s += "=";
                    break; // One pad char
                default:
                    throw new KeyDiscoveryException("Illegal base64url string!", loggableInformation);
            }

            return Convert.FromBase64String(s); // Standard base64 decoder
        }

        /// <summary>
        /// Encode the base64url.
        /// </summary>
        /// <param name="input">The byte array data.</param>
        /// <returns>The base64url encoded string</returns>
        public static string Encode(byte[] input)
        {
            string s = Convert.ToBase64String(input); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }
    }
}
