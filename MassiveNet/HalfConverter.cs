// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Runtime.InteropServices;

namespace MassiveNet {
    internal static class HalfConverter {
        static readonly uint[] ToFloatMantissa = new uint[2048];
        static readonly uint[] ToFloatExponent = new uint[64];
        static readonly uint[] ToFloatOffset = new uint[64];

        static readonly ushort[] ToHalfBase = new ushort[512];
        static readonly byte[] ToHalfShift = new byte[512];

        [StructLayout(LayoutKind.Explicit)]
        struct FloatToUint {
            [FieldOffset(0)]
            public uint uintValue;

            [FieldOffset(0)]
            public float floatValue;
        }

        static HalfConverter() {
            GenerateToFloat();
            GenerateToHalf();
        }

        /// <summary> Populates tables for half to float conversion lookups. </summary>
        private static void GenerateToFloat() {
            PopulateMantissaTable();
            PopulateExponentTable();
            PopulateOffsetTable();
        }

        private static void PopulateMantissaTable() {
            ToFloatMantissa[0] = 0;
            for (int i = 1; i < 1024; i++) {
                uint m = ((uint)i) << 13;
                uint e = 0;

                while ((m & 0x00800000) == 0) {
                    e -= 0x00800000;
                    m <<= 1;
                }

                m &= ~0x00800000U;
                e += 0x38800000;
                ToFloatMantissa[i] = m | e;
            }

            for (int i = 1024; i < 2048; i++) {
                ToFloatMantissa[i] = 0x38000000 + (((uint)(i - 1024)) << 13);
            }
        }

        private static void PopulateExponentTable() {
            ToFloatExponent[0] = 0;
            for (int i = 1; i < 63; i++) {
                // Positive:
                if (i < 31) ToFloatExponent[i] = ((uint)i) << 23;
                // Negative:
                else ToFloatExponent[i] = 0x80000000 + (((uint)(i - 32)) << 23);
            }
            ToFloatExponent[31] = 0x47800000;
            ToFloatExponent[32] = 0x80000000;
            ToFloatExponent[63] = 0xC7800000;
        }

        private static void PopulateOffsetTable() {
            ToFloatOffset[0] = 0;
            for (int i = 1; i < 64; i++) ToFloatOffset[i] = 1024;
            ToFloatOffset[32] = 0;
        }

        /// <summary> Populates tables for float to half conversion. </summary>
        private static void GenerateToHalf() {
            for (int i = 0; i < 256; i++) {
                int e = i - 127;
                if (e < -24) MapToZero(i);
                else if (e < -14) MapToDenorm(i, e);
                else if (e <= 15) MapToPrecisionLoss(i, e);
                else if (e < 128) MapToInfinity(i);
                else MapForNaN(i);
            }
        }

        private static void MapToZero(int i) {
            // Too-small numbers map to zero:
            ToHalfBase[i | 0x000] = 0x0000;
            ToHalfBase[i | 0x100] = 0x8000;
            ToHalfShift[i | 0x000] = 24;
            ToHalfShift[i | 0x100] = 24;
        }

        private static void MapToDenorm(int i, int e) {
            // Very small numbers become denorms:
            ToHalfBase[i | 0x000] = (ushort)((0x0400 >> (-e - 14)));
            ToHalfBase[i | 0x100] = (ushort)((0x0400 >> (-e - 14)) | 0x8000);
            ToHalfShift[i | 0x000] = (byte)(-e - 1);
            ToHalfShift[i | 0x100] = (byte)(-e - 1);
        }

        private static void MapToPrecisionLoss(int i, int e) {
            // Normal range numbers lose precision:
            ToHalfBase[i | 0x000] = (ushort)(((e + 15) << 10));
            ToHalfBase[i | 0x100] = (ushort)(((e + 15) << 10) | 0x8000);
            ToHalfShift[i | 0x000] = 13;
            ToHalfShift[i | 0x100] = 13;
        }

        private static void MapToInfinity(int i) {
            // Too-large numbers == infinity:
            ToHalfBase[i | 0x000] = 0x7C00;
            ToHalfBase[i | 0x100] = 0xFC00;
            ToHalfShift[i | 0x000] = 24;
            ToHalfShift[i | 0x100] = 24;
        }

        private static void MapForNaN(int i) {
            // Infinity and NaN keep their value:
            ToHalfBase[i | 0x000] = 0x7C00;
            ToHalfBase[i | 0x100] = 0xFC00;
            ToHalfShift[i | 0x000] = 13;
            ToHalfShift[i | 0x100] = 13;
        }

        public static float HalfToFloat(ushort value) {
            var conv = new FloatToUint();
            conv.uintValue = ToFloatMantissa[ToFloatOffset[value >> 10] + (((uint)value) & 0x3ff)] + ToFloatExponent[value >> 10];
            return conv.floatValue;
        }

        public static ushort FloatToHalf(float value) {
            var conv = new FloatToUint();
            conv.floatValue = value;
            return (ushort)(ToHalfBase[(conv.uintValue >> 23) & 0x1ff] + ((conv.uintValue & 0x007fffff) >> ToHalfShift[(conv.uintValue >> 23) & 0x1ff]));
        }
    }
}