// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Security.Cryptography;

namespace MassiveNet {
    public class NetMath {
        internal static ushort Trim(ushort sequence) {
            sequence >>= 1;
            return sequence;
        }

        internal static ushort Pad(ushort sequence) {
            sequence <<= 1;
            sequence |= ((1 << 1) - 1);
            return sequence;
        }

        internal static int SeqDistance(ushort from, ushort to) {
            from <<= 1;
            to <<= 1;
            return ((short) (from - to)) >> 1;
        }

        private static readonly RNGCryptoServiceProvider Rng = new RNGCryptoServiceProvider();
        private static readonly byte[] FourByteArr = new byte[4];
        private static readonly byte[] EightByteArr = new byte[8];
        private static readonly object LockObj = new object();


        public static uint RandomUint() {
            lock (LockObj) {
                Rng.GetBytes(FourByteArr);
                return BitConverter.ToUInt32(FourByteArr, 0);
            }
        }

        public static ulong RandomUlong() {
            lock (LockObj) {
                Rng.GetBytes(EightByteArr);
                return BitConverter.ToUInt64(EightByteArr, 0);
            }
        }
    }
}