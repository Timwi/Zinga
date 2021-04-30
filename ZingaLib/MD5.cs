using System.Numerics;
using System.Text;
using RT.Util.ExtensionMethods;

namespace Zinga.Lib
{
    public static class MD5
    {
        public static string ComputeHex(string input) => ComputeBigInteger(input.ToUtf8()).ToString("X").TrimStart('0').PadLeft(32, '0');

        public static string ComputeUrlName(string input)
        {
            var value = ComputeBigInteger(input.ToUtf8());
            var str = new StringBuilder();
            var allowedCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            while (value > 0)
            {
                str.Append(allowedCharacters[(int) (value % allowedCharacters.Length)]);
                value /= allowedCharacters.Length;
            }
            return str.ToString();
        }

        public static BigInteger ComputeBigInteger(byte[] input)
        {
            var pad = (uint) ((448 - ((input.Length * 8) % 512) + 512) % 512);      // number of bits to be padded
            if (pad == 0)
                pad = 512;

            var sizeMsgBuff = (uint) (input.Length + (pad / 8) + 8);      // buffer size in multiple of bytes
            var sizeMsg = (ulong) input.Length * 8;
            var bMsg = new byte[sizeMsgBuff];

            for (int i = 0; i < input.Length; i++)
                bMsg[i] = input[i];

            // make first bit of padding 1
            bMsg[input.Length] |= 0x80;

            // write the size value
            for (int i = 8; i > 0; i--)
                bMsg[sizeMsgBuff - i] = (byte) (sizeMsg >> ((8 - i) * 8) & 0xff);

            var xArr = new uint[16];
            uint rotateLeft(uint uiNumber, ushort shift) => (uiNumber >> 32 - shift) | (uiNumber << shift);
            uint TransF(uint a, uint b, uint c, uint d, uint k, ushort s, uint i) => b + rotateLeft(a + ((b & c) | (~b & d)) + xArr[k] + T[i - 1], s);
            uint TransG(uint a, uint b, uint c, uint d, uint k, ushort s, uint i) => b + rotateLeft(a + ((b & d) | (c & ~d)) + xArr[k] + T[i - 1], s);
            uint TransH(uint a, uint b, uint c, uint d, uint k, ushort s, uint i) => b + rotateLeft(a + (b ^ c ^ d) + xArr[k] + T[i - 1], s);
            uint TransI(uint a, uint b, uint c, uint d, uint k, ushort s, uint i) => b + rotateLeft(a + (c ^ (b | ~d)) + xArr[k] + T[i - 1], s);

            var a = 0x67452301U;
            var b = 0xefcdab89U;
            var c = 0x98badcfeU;
            var d = 0x10325476U;

            var size = (uint) (bMsg.Length * 8) / 32 / 16;
            for (uint i = 0; i < size; i++)
            {
                var block = i << 6;
                for (uint j = 0; j < 61; j += 4)
                    xArr[j >> 2] =
                        (((uint) bMsg[block + j + 3]) << 24) |
                        (((uint) bMsg[block + j + 2]) << 16) |
                        (((uint) bMsg[block + j + 1]) << 8) |
                        bMsg[block + j];

                uint oa = a;
                uint ob = b;
                uint oc = c;
                uint od = d;

                // Round 1
                a = TransF(a, b, c, d, 0, 7, 1); d = TransF(d, a, b, c, 1, 12, 2); c = TransF(c, d, a, b, 2, 17, 3); b = TransF(b, c, d, a, 3, 22, 4);
                a = TransF(a, b, c, d, 4, 7, 5); d = TransF(d, a, b, c, 5, 12, 6); c = TransF(c, d, a, b, 6, 17, 7); b = TransF(b, c, d, a, 7, 22, 8);
                a = TransF(a, b, c, d, 8, 7, 9); d = TransF(d, a, b, c, 9, 12, 10); c = TransF(c, d, a, b, 10, 17, 11); b = TransF(b, c, d, a, 11, 22, 12);
                a = TransF(a, b, c, d, 12, 7, 13); d = TransF(d, a, b, c, 13, 12, 14); c = TransF(c, d, a, b, 14, 17, 15); b = TransF(b, c, d, a, 15, 22, 16);

                // Round 2
                a = TransG(a, b, c, d, 1, 5, 17); d = TransG(d, a, b, c, 6, 9, 18); c = TransG(c, d, a, b, 11, 14, 19); b = TransG(b, c, d, a, 0, 20, 20);
                a = TransG(a, b, c, d, 5, 5, 21); d = TransG(d, a, b, c, 10, 9, 22); c = TransG(c, d, a, b, 15, 14, 23); b = TransG(b, c, d, a, 4, 20, 24);
                a = TransG(a, b, c, d, 9, 5, 25); d = TransG(d, a, b, c, 14, 9, 26); c = TransG(c, d, a, b, 3, 14, 27); b = TransG(b, c, d, a, 8, 20, 28);
                a = TransG(a, b, c, d, 13, 5, 29); d = TransG(d, a, b, c, 2, 9, 30); c = TransG(c, d, a, b, 7, 14, 31); b = TransG(b, c, d, a, 12, 20, 32);

                // Round 3
                a = TransH(a, b, c, d, 5, 4, 33); d = TransH(d, a, b, c, 8, 11, 34); c = TransH(c, d, a, b, 11, 16, 35); b = TransH(b, c, d, a, 14, 23, 36);
                a = TransH(a, b, c, d, 1, 4, 37); d = TransH(d, a, b, c, 4, 11, 38); c = TransH(c, d, a, b, 7, 16, 39); b = TransH(b, c, d, a, 10, 23, 40);
                a = TransH(a, b, c, d, 13, 4, 41); d = TransH(d, a, b, c, 0, 11, 42); c = TransH(c, d, a, b, 3, 16, 43); b = TransH(b, c, d, a, 6, 23, 44);
                a = TransH(a, b, c, d, 9, 4, 45); d = TransH(d, a, b, c, 12, 11, 46); c = TransH(c, d, a, b, 15, 16, 47); b = TransH(b, c, d, a, 2, 23, 48);

                // Round 4
                a = TransI(a, b, c, d, 0, 6, 49); d = TransI(d, a, b, c, 7, 10, 50); c = TransI(c, d, a, b, 14, 15, 51); b = TransI(b, c, d, a, 5, 21, 52);
                a = TransI(a, b, c, d, 12, 6, 53); d = TransI(d, a, b, c, 3, 10, 54); c = TransI(c, d, a, b, 10, 15, 55); b = TransI(b, c, d, a, 1, 21, 56);
                a = TransI(a, b, c, d, 8, 6, 57); d = TransI(d, a, b, c, 15, 10, 58); c = TransI(c, d, a, b, 6, 15, 59); b = TransI(b, c, d, a, 13, 21, 60);
                a = TransI(a, b, c, d, 4, 6, 61); d = TransI(d, a, b, c, 11, 10, 62); c = TransI(c, d, a, b, 2, 15, 63); b = TransI(b, c, d, a, 9, 21, 64);

                a += oa;
                b += ob;
                c += oc;
                d += od;
            }

            static uint reverseByte(uint uiNumber) =>
                ((uiNumber & 0xff000000) >> 24) |
                ((uiNumber & 0x00ff0000) >> 8) |
                ((uiNumber & 0x0000ff00) << 8) |
                ((uiNumber & 0x000000ff) << 24);

            return ((((((BigInteger) reverseByte(a) << 32) | reverseByte(b)) << 32) | reverseByte(c)) << 32) | reverseByte(d);
        }

        /// <summary>Lookup table for 4294967296*sin(i)</summary>
        private static readonly uint[] T = new uint[]
        {
            0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
            0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
            0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
            0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
            0xd62f105d, 0x2441453, 0xd8a1e681, 0xe7d3fbc8,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x4881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
            0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
            0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
            0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
        };
    }
}
