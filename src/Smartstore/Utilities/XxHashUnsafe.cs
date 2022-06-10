namespace Smartstore.Utilities
{
    /// <summary>
    /// xxHash is an extremely fast non-cryptographic Hash algorithm, working at speeds close to RAM limits.
    /// http://code.google.com/p/xxhash/
    /// </summary>
    public static class XxHashUnsafe
    {
        private const uint Prime1 = 2654435761U;
        private const uint Prime2 = 2246822519U;
        private const uint Prime3 = 3266489917U;
        private const uint Prime4 = 668265263U;
        private const int Prime5 = 0x165667b1;
        private const uint Seed = 0xc58f1a7b;

        /// <summary>
        /// Computes the xxHash of the input string. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="data">the input string</param>
        /// <returns>xxHash</returns>
        public static unsafe uint ComputeHash(string data)
        {
            fixed (char* input = data)
            {
                return Hash((byte*)input, (uint)data.Length * sizeof(char), Seed);
            }
        }

        /// <summary>
        /// Computes the xxHash of the input byte array. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="data">the input byte array</param>
        /// <returns>xxHash</returns>
        public static unsafe uint ComputeHash(byte[] data)
        {
            fixed (byte* input = &data[0])
            {
                return Hash(input, (uint)data.Length, Seed);
            }
        }

        /// <summary>
        /// Computes the xxHash of the input byte array. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="data">the input byte array</param>
        /// <param name="offset">start offset</param>
        /// <param name="len">length</param>
        /// <param name="seed">initial seed</param>
        /// <returns>xxHash</returns>
        public static unsafe uint ComputeHash(byte[] data, int offset, uint len, uint seed)
        {
            fixed (byte* input = &data[offset])
            {
                return Hash(input, len, seed);
            }
        }

        private unsafe static uint Hash(byte* data, uint len, uint seed)
        {
            if (len < 16)
                return HashSmall(data, len, seed);
            var v1 = seed + Prime1;
            var v2 = v1 * Prime2 + len;
            var v3 = v2 * Prime3;
            var v4 = v3 * Prime4;
            var p = (uint*)data;
            var limit = (uint*)(data + len - 16);
            while (p < limit)
            {
                v1 += Rotl32(v1, 13); v1 *= Prime1; v1 += *p; p++;
                v2 += Rotl32(v2, 11); v2 *= Prime1; v2 += *p; p++;
                v3 += Rotl32(v3, 17); v3 *= Prime1; v3 += *p; p++;
                v4 += Rotl32(v4, 19); v4 *= Prime1; v4 += *p; p++;
            }
            p = limit;
            v1 += Rotl32(v1, 17); v2 += Rotl32(v2, 19); v3 += Rotl32(v3, 13); v4 += Rotl32(v4, 11);
            v1 *= Prime1; v2 *= Prime1; v3 *= Prime1; v4 *= Prime1;
            v1 += *p; p++; v2 += *p; p++; v3 += *p; p++; v4 += *p;
            v1 *= Prime2; v2 *= Prime2; v3 *= Prime2; v4 *= Prime2;
            v1 += Rotl32(v1, 11); v2 += Rotl32(v2, 17); v3 += Rotl32(v3, 19); v4 += Rotl32(v4, 13);
            v1 *= Prime3; v2 *= Prime3; v3 *= Prime3; v4 *= Prime3;
            var crc = v1 + Rotl32(v2, 3) + Rotl32(v3, 6) + Rotl32(v4, 9);
            crc ^= crc >> 11;
            crc += (Prime4 + len) * Prime1;
            crc ^= crc >> 15;
            crc *= Prime2;
            crc ^= crc >> 13;
            return crc;
        }

        private unsafe static uint HashSmall(byte* data, uint len, uint seed)
        {
            var p = data;
            var bEnd = data + len;
            var limit = bEnd - 4;
            var idx = seed + Prime1;
            uint crc = Prime5;
            while (p < limit)
            {
                crc += (*(uint*)p) + idx;
                idx++;
                crc += Rotl32(crc, 17) * Prime4;
                crc *= Prime1;
                p += 4;
            }
            while (p < bEnd)
            {
                crc += (*p) + idx;
                idx++;
                crc *= Prime1;
                p++;
            }
            crc += len;
            crc ^= crc >> 15;
            crc *= Prime2;
            crc ^= crc >> 13;
            crc *= Prime3;
            crc ^= crc >> 16;
            return crc;
        }

        private static uint Rotl32(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }
    }
}
