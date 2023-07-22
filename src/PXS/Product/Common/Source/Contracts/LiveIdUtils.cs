//--------------------------------------------------------------------------------
// <copyright file="LiveIdUtils.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

//// Do not change. This is as acquired from Ad platform.
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// This is a utility class to convert PUID to ANID.
    /// </summary>
    public static class LiveIdUtils
    {
        /// <summary>
        /// The HMAC.
        /// </summary>
        [ThreadStatic]
        private static HMAC hmac;

        /// <summary>
        /// The MD5.
        /// </summary>
        [ThreadStatic]
        private static MD5 md5;

        /// <summary>
        /// Method to convert PUID to Anonymous-Id (ANID).
        /// </summary>
        /// <param name="puid">The PUID, in decimal format, to be converted.</param>
        /// <returns>ANID id is returned.</returns>
        public static string ToAnonymousHex(long puid)
        {
            string puidHex = ToHexFormat(puid);
            return ToAnonymousHex(puidHex);
        }

#pragma warning disable CA5351
#pragma warning disable CA5373
        /// <summary>
        /// Method to convert PUID to Anonymous-Id (ANID).
        /// </summary>
        /// <param name="hexPuid">The PUID, in hex format, to be converted.</param>
        /// <returns>The ANID is returned in hex format.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security.Cryptography", "CA5350:MD5CannotBeUsed", Justification = "This is legacy code.")]
        public static string ToAnonymousHex(string hexPuid)
        {
            if (hmac == null)
            {
                var derivedBytes = DisposableUtility.SafeCreate<PasswordDeriveBytes>(() =>
                {
                    return new PasswordDeriveBytes(new byte[] { 0x58, 0x41, 0x0F, 0x3B, 0x3F, 0xBE, 0x34, 0x64, 0xA4, 0xAA, 0x7B, 0x5D, 0xD2, 0xD8, 0xCE, 0x1B }, null);
                });
                
                //suppressing Obsolete password lgtm warning by using lgtm query identifier
                //Reasons for suppression is similar to suppressing weak-hmac as below
                var derivedKey = derivedBytes.CryptDeriveKey("RC2", "MD5", 128, new byte[8]); // lgtm[cs/obsolete-password-key-derivation]

                hmac = DisposableUtility.SafeCreate<HMACMD5>(() =>
                {
                    //suppressing Weak-hmac lgtm warning as the purpose of this function is to generate Id
                    //Reasons for suppressing:
                    //1. This is an old code, effort in analyzing the conflict that it can create would require more time
                    //2. We are not doing any encryption/decryption of sensitive information to be more concerned
                    return new HMACMD5(derivedKey); // lgtm[cs/weak-hmacs]
                });
            }

            const int PuidLength = 32;

            byte[] bytes = new byte[sizeof(char) * (PuidLength + 1)];
            Encoding.Unicode.GetBytes(hexPuid).CopyTo(bytes, 0);

            byte[] hash = hmac.ComputeHash(bytes);

            StringBuilder hex = new StringBuilder(16);
            for (int i = 0; i < hash.Length - 4; i++)
            {
                hex.Append(hash[i].ToString("X", CultureInfo.InvariantCulture).PadLeft(2, '0'));
            }

            hex.Append('F', 8);

            return hex.ToString();
        }
#pragma warning restore CA5373
#pragma warning restore CA5351
        /// <summary>
        /// Converts the specified <paramref name="value"/> to hex-representation.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The hex-representation of the specified <paramref name="value"/>.</returns>
        public static string ToHexFormat(long value)
        {
            return value.ToString("X", CultureInfo.InvariantCulture).PadLeft(16, '0');
        }

        /// <summary>
        /// Converts the specified <paramref name="hex"/> to decimal-representation.
        /// </summary>
        /// <param name="hex">The value to convert.</param>
        /// <returns>The decimal-representation of the specified <paramref name="hex"/>.</returns>
        public static long ToDecimalFormat(string hex)
        {
            return long.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Method to create HASH.
        /// </summary>
        /// <param name="userId">UserID for the user.</param>
        /// <returns>Hash is returned.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security.Cryptography", "CA5350:MD5CannotBeUsed", Justification = "This is legacy code.")]
        public static long? ToSampledHash(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            if (md5 == null)
            {
                md5 = MD5.Create(); // lgtm[cs/weak-crypto] Reason for Suppressing is this is a legacy code.
            }

            // Compute Hash Bytes
            byte[] bytes = Encoding.UTF8.GetBytes(userId);
            byte[] hash = md5.ComputeHash(bytes);

            // Convert Bytes to Long
            long high = BitConverter.ToInt64(hash, 0);
            long low = BitConverter.ToInt64(hash, 8);

            return high ^ low;
        }

        /// <summary>
        /// Checks whether one UserID is a valid ANID. The valid ANID should be an
        /// hexadecimal and its length is 32.
        /// </summary>
        /// <param name="userId">The UserID.</param>
        /// <returns>True, if it is valid; otherwise, false.</returns>
        public static bool IsValidAnid(string userId)
        {
            // First checks if it is null or the length is valid
            if (userId == null || userId.Length != 32)
            {
                return false;
            }

            Guid result;
            return Guid.TryParseExact(userId, "N", out result);
        }
    }
}
