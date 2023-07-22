namespace PCF.UnitTests
{
    using System;
    using System.Text;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class NonCryptoHashTests
    {
        [Fact]
        public void NonCryptoHashTests_Murmur3_Correctness()
        {
            var testBytes = Encoding.UTF8.GetBytes("Hello, world! This string is longer than 16 bytes.").AsSpan();

            for (int i = 0; i <= testBytes.Length; ++i)
            {
                ulong hash1a, hash2a, hash1b, hash2b;

                NonCryptoMurmur3Hash.GetHash128(testBytes.Slice(0, i), out hash1a, out hash2a);
                GetMurmur3Hash128(testBytes.ToArray(), i, out hash1b, out hash2b);
                
                Assert.Equal(hash1a, hash1b);
                Assert.Equal(hash2a, hash2b);
            }

            // Compare with known good outputs
            ulong hash1, hash2;
            NonCryptoMurmur3Hash.GetHash128(testBytes, out hash1, out hash2);
            Assert.Equal(11297019115326207533ul, hash1);
            Assert.Equal(16876754693914769251ul, hash2);

            testBytes = Encoding.UTF8.GetBytes("Foo bar baz bat").AsSpan();
            NonCryptoMurmur3Hash.GetHash128(testBytes, out hash1, out hash2);
            Assert.Equal(16471830257927555838ul, hash1);
            Assert.Equal(6338798948600228179ul, hash2);
        }

        /// <summary>
        /// A known correct reference implementation of murmur3. Not the fastest thing, but is great for testing against.
        /// </summary>
        private static void GetMurmur3Hash128(byte[] input, int length, out ulong outHash1, out ulong outHash2)
        {
            const int READ_SIZE = 16;
            const ulong C1 = 0x87c37b91114253d5L;
            const ulong C2 = 0x4cf5ad432745937fL;

            ulong hash1 = 0;
            ulong hash2 = 0;
            int pos = 0;
            ulong remaining = (ulong)length;

            // Body. Process blocks of 16 bytes all at once.
            while (remaining >= READ_SIZE)
            {
                ulong k1 = BitConverter.ToUInt64(input, pos);
                pos += 8;

                ulong k2 = BitConverter.ToUInt64(input, pos);
                pos += 8;

                remaining -= READ_SIZE;

                k1 = unchecked(k1 * C1);
                k1 = RotateLeft(k1, 31);
                k1 = unchecked(k1 * C2);
                hash1 ^= k1;

                hash1 = RotateLeft(hash1, 27);
                hash1 = unchecked(hash1 + hash2);
                hash1 = unchecked((hash1 * 5) + 0x52dce729);

                k2 = unchecked(k2 * C2);
                k2 = RotateLeft(k2, 33);
                k2 = unchecked(k2 * C1);
                hash2 ^= k2;

                hash2 = RotateLeft(hash2, 31);
                hash2 = unchecked(hash2 + hash1);
                hash2 = unchecked((hash2 * 5) + 0x38495ab5);
            }

            // Tail. If the byte array was not a multiple of 16, process the remaining bytes.
            if (remaining > 0)
            {
                ulong k1 = 0;
                ulong k2 = 0;

                switch (remaining)
                {
                    case 15:
                        k2 ^= (ulong)input[pos + 14] << 48;
                        goto case 14;
                    case 14:
                        k2 ^= (ulong)input[pos + 13] << 40;
                        goto case 13;
                    case 13:
                        k2 ^= (ulong)input[pos + 12] << 32;
                        goto case 12;
                    case 12:
                        k2 ^= (ulong)input[pos + 11] << 24;
                        goto case 11;
                    case 11:
                        k2 ^= (ulong)input[pos + 10] << 16;
                        goto case 10;
                    case 10:
                        k2 ^= (ulong)input[pos + 9] << 8;
                        goto case 9;
                    case 9:
                        k2 ^= (ulong)input[pos + 8];
                        k2 = unchecked(k2 * C2);
                        k2 = RotateLeft(k2, 33);
                        k2 = unchecked(k2 * C1);
                        hash2 ^= k2;
                        goto case 8;
                    case 8:
                        k1 ^= (ulong)input[pos + 7] << 56;
                        goto case 7;
                    case 7:
                        k1 ^= (ulong)input[pos + 6] << 48;
                        goto case 6;
                    case 6:
                        k1 ^= (ulong)input[pos + 5] << 40;
                        goto case 5;
                    case 5:
                        k1 ^= (ulong)input[pos + 4] << 32;
                        goto case 4;
                    case 4:
                        k1 ^= (ulong)input[pos + 3] << 24;
                        goto case 3;
                    case 3:
                        k1 ^= (ulong)input[pos + 2] << 16;
                        goto case 2;
                    case 2:
                        k1 ^= (ulong)input[pos + 1] << 8;
                        goto case 1;
                    case 1:
                        k1 ^= (ulong)input[pos];
                        k1 = unchecked(k1 * C1);
                        k1 = RotateLeft(k1, 31);
                        k1 = unchecked(k1 * C2);
                        hash1 ^= k1;
                        break;
                }
            }

            // Finalization. 
            hash1 ^= (ulong)length;
            hash2 ^= (ulong)length;

            hash1 = unchecked(hash1 + hash2);
            hash2 = unchecked(hash2 + hash1);

            hash1 = FinalMix(hash1);
            hash2 = FinalMix(hash2);

            hash1 = unchecked(hash1 + hash2);
            hash2 = unchecked(hash2 + hash1);

            outHash1 = hash1;
            outHash2 = hash2;
        }

        private static ulong RotateLeft(ulong original, int bits)
        {
            return (original << bits) | (original >> (64 - bits));
        }

        private static ulong FinalMix(ulong k)
        {
            k ^= k >> 33;
            k = unchecked(k * 0xff51afd7ed558ccd);
            k ^= k >> 33;
            k = unchecked(k * 0xc4ceb9fe1a85ec53);
            k ^= k >> 33;
            return k;
        }
    }
}