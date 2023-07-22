namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    // ############################################################################################
    // This implements the MurmurHash3 hash function. MurmurHash3 was written by Austin Appleby, and was placed
    // in the public domain by the author in November 2010.
    // See https://code.google.com/p/smhasher/source/browse/branches/chandlerc_dev/MurmurHash3.cpp for the C++
    // reference version of this code.
    public static class NonCryptoMurmur3Hash
    {
        private const ulong READSIZE = 16;
        private const ulong C1 = 0x87c37b91114253d5L;
        private const ulong C2 = 0x4cf5ad432745937fL;

        #region Public methods (all static)

        public static uint GetHash32(string input)
        {
            return GetHash32(Encoding.UTF8.GetBytes(input));
        }

        public static uint GetHash32(ReadOnlySpan<byte> input)
        {
            return (uint)(GetHash64(input) & 0x00000000FFFFFFFF);
        }

        public static ulong GetHash64(string input)
        {
            return GetHash64(Encoding.UTF8.GetBytes(input));
        }

        public static ulong GetHash64(ReadOnlySpan<byte> input)
        {
            GetHash128(input, out ulong hash1, out _);
            return hash1;
        }

        /// <summary>
        /// Computes the Murmur3 hash of Type T using callbacks to fetch items at a given index and convert to ulong at a given index.
        /// Callbacks are used for performance reasons, since they don't require an allocation of a byte buffer wrapper and avoid the
        /// indirection tax of going through a byte buffer.
        /// </summary>
        public static void GetHash128(
            ReadOnlySpan<byte> pointer,
            out ulong outHash1,
            out ulong outHash2)
        {
            ulong hash1 = 0;
            ulong hash2 = 0;
            int pos = 0;
            ulong remaining = (ulong)pointer.Length;

            // Body. Process blocks of 16 bytes all at once.
            while (remaining >= READSIZE)
            {
                ulong k1 = GetUInt64(pos, pointer);
                pos += 8;
                ulong k2 = GetUInt64(pos, pointer);
                pos += 8;
                remaining -= READSIZE;

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
                        k2 ^= (ulong)pointer[pos + 14] << 48;
                        goto case 14;
                    case 14:
                        k2 ^= (ulong)pointer[pos + 13] << 40;
                        goto case 13;
                    case 13:
                        k2 ^= (ulong)pointer[pos + 12] << 32;
                        goto case 12;
                    case 12:
                        k2 ^= (ulong)pointer[pos + 11] << 24;
                        goto case 11;
                    case 11:
                        k2 ^= (ulong)pointer[pos + 10] << 16;
                        goto case 10;
                    case 10:
                        k2 ^= (ulong)pointer[pos + 9] << 8;
                        goto case 9;
                    case 9:
                        k2 ^= (ulong)pointer[pos + 8];
                        k2 = unchecked(k2 * C2);
                        k2 = RotateLeft(k2, 33);
                        k2 = unchecked(k2 * C1);
                        hash2 ^= k2;
                        goto case 8;
                    case 8:
                        k1 ^= (ulong)pointer[pos + 7] << 56;
                        goto case 7;
                    case 7:
                        k1 ^= (ulong)pointer[pos + 6] << 48;
                        goto case 6;
                    case 6:
                        k1 ^= (ulong)pointer[pos + 5] << 40;
                        goto case 5;
                    case 5:
                        k1 ^= (ulong)pointer[pos + 4] << 32;
                        goto case 4;
                    case 4:
                        k1 ^= (ulong)pointer[pos + 3] << 24;
                        goto case 3;
                    case 3:
                        k1 ^= (ulong)pointer[pos + 2] << 16;
                        goto case 2;
                    case 2:
                        k1 ^= (ulong)pointer[pos + 1] << 8;
                        goto case 1;
                    case 1:
                        k1 ^= (ulong)pointer[pos];
                        k1 = unchecked(k1 * C1);
                        k1 = RotateLeft(k1, 31);
                        k1 = unchecked(k1 * C2);
                        hash1 ^= k1;
                        break;
                }
            }

            // Finalization. 
            hash1 ^= (ulong)pointer.Length;
            hash2 ^= (ulong)pointer.Length;

            hash1 = unchecked(hash1 + hash2);
            hash2 = unchecked(hash2 + hash1);

            hash1 = FinalMix(hash1);
            hash2 = FinalMix(hash2);

            hash1 = unchecked(hash1 + hash2);
            hash2 = unchecked(hash2 + hash1);

            outHash1 = hash1;
            outHash2 = hash2;
        }

        #endregion

        #region Private methods

        private static ulong GetUInt64(int position, ReadOnlySpan<byte> span)
        {
            return MemoryMarshal.Cast<byte, ulong>(span.Slice(position))[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FinalMix(ulong k)
        {
            k ^= k >> 33;
            k = unchecked(k * 0xff51afd7ed558ccd);
            k ^= k >> 33;
            k = unchecked(k * 0xc4ceb9fe1a85ec53);
            k ^= k >> 33;
            return k;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong original, int bits)
        {
            return (original << bits) | (original >> (64 - bits));
        }

        #endregion
    }
}