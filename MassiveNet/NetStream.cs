// This file contains substantial code from an MIT Licensed codebase (udpkit). The license for udpkit is as follows:

//The MIT License (MIT)

//Copyright (c) 2012-2014 Fredrik Holmstrom (fredrik.johan.holmstrom@gmail.com)

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MassiveNet {

    [StructLayout(LayoutKind.Explicit)]
    internal struct FpBytes {
        [FieldOffset(0)]
        public Single Float;
        [FieldOffset(0)]
        public Double Double;
        [FieldOffset(0)]
        public Byte B0;
        [FieldOffset(1)]
        public Byte B1;
        [FieldOffset(2)]
        public Byte B2;
        [FieldOffset(3)]
        public Byte B3;
        [FieldOffset(4)]
        public Byte B4;
        [FieldOffset(5)]
        public Byte B5;
        [FieldOffset(6)]
        public Byte B6;
        [FieldOffset(7)]
        public Byte B7;

        public static implicit operator FpBytes(Single value) {
            var bytes = default(FpBytes);
            bytes.Float = value;
            return bytes;
        }

        public static implicit operator FpBytes(Double value) {
            var bytes = default(FpBytes);
            bytes.Double = value;
            return bytes;
        }
    }

    public class NetStream {

        internal NetConnection Connection;
        internal NetSocket Socket;
        internal int Pos;
        internal int Length;
        internal byte[] Data;

        internal bool WriteLength = false;

        /// <summary>
        /// When true, all floats written will use half precision. This value must be the same for client and server. 
        /// </summary>
        public static bool HalfFloats = true;

        private NetStream() { }

        /// <summary> The current size of the stream (in bits). </summary>
        public int Size {
            get { return Length; }
            internal set { Length = Mathf.Clamp(value, 0, Data.Length << 3); }
        }

        /// <summary> There current read/write position of the stream (in bits). </summary>
        public int Position {
            get { return Pos; }
            set { Pos = Mathf.Clamp(value, 0, Length); }
        }

        internal byte[] ByteBuffer {
            get { return Data; }
        }

        /// <summary> Creates a new NetStream, recycling from the pool when possible. </summary>
        internal static NetStream Create() {
            return CreateFromPool();
        }

        /// <summary> Creates a new NetStream, recycling from the pool when possible. </summary>
        public static NetStream New() {
            var strm = CreateFromPool();
            strm.WriteLength = true;
            return strm;
        }

        private NetStream(byte[] arr) {
            Pos = 0;
            Data = arr;
            Length = arr.Length << 3;
        }

        internal bool CanWrite(int bits) {
            return Pos + bits <= Length;
        }

        internal bool CanRead(int bits) {
            return Pos + bits <= Length;
        }

        internal void Reset() {
            Reset(Data.Length);
        }

        internal void Reset(int size) {
            Pos = 0;
            Length = size << 3;
            Array.Clear(Data, 0, Data.Length);
        }

        public bool WriteBool(bool value) {
            InternalWriteByte(value ? (byte)1 : (byte)0, 1);
            return value;
        }

        public bool ReadBool() {
            return InternalReadByte(1) == 1;
        }

        internal void WriteByte(byte value, int bits) {
            InternalWriteByte(value, bits);
        }

        internal byte ReadByte(int bits) {
            return InternalReadByte(bits);
        }

        public void WriteByte(byte value) {
            WriteByte(value, 8);
        }

        public byte ReadByte() {
            return ReadByte(8);
        }

        internal void WriteSByte(sbyte value, int bits) {
            InternalWriteByte((byte)value, bits);
        }

        internal sbyte ReadSByte(int bits) {
            return (sbyte)InternalReadByte(bits);
        }

        public void WriteSByte(sbyte value) {
            WriteSByte(value, 8);
        }

        public sbyte ReadSByte() {
            return ReadSByte(8);
        }

        internal void WriteUShort(ushort value, int bits) {
            if (bits <= 8) InternalWriteByte((byte)(value & 0xFF), bits);
            InternalWriteByte((byte)(value & 0xFF), 8);
            InternalWriteByte((byte)(value >> 8), bits - 8);
        }

        internal ushort ReadUShort(int bits) {
            if (bits <= 8) return InternalReadByte(bits);
            return (ushort)(InternalReadByte(8) | (InternalReadByte(bits - 8) << 8));
        }

        public void WriteUShort(ushort value) {
            WriteUShort(value, 16);
        }

        public ushort ReadUShort() {
            return ReadUShort(16);
        }

        internal void WriteShort(short value, int bits) {
            WriteUShort((ushort)value, bits);
        }

        internal short ReadShort(int bits) {
            return (short)ReadUShort(bits);
        }

        public void WriteShort(short value) {
            WriteShort(value, 16);
        }

        public short ReadShort() {
            return ReadShort(16);
        }

        internal void WriteChar(char value, int bits) {
            WriteUShort(value, bits);
        }

        internal char ReadChar(int bits) {
            return (char)ReadUShort(bits);
        }

        public void WriteChar(char value) {
            WriteChar(value, 16);
        }

        public char ReadChar() {
            return ReadChar(16);
        }

        internal void WriteUInt(uint value, int bits) {
            byte a = (byte)(value >> 0),
                 b = (byte)(value >> 8),
                 c = (byte)(value >> 16),
                 d = (byte)(value >> 24);

            switch ((bits + 7) / 8) {
                case 1:
                    InternalWriteByte(a, bits);
                    break;
                case 2:
                    InternalWriteByte(a, 8);
                    InternalWriteByte(b, bits - 8);
                    break;
                case 3:
                    InternalWriteByte(a, 8);
                    InternalWriteByte(b, 8);
                    InternalWriteByte(c, bits - 16);
                    break;
                case 4:
                    InternalWriteByte(a, 8);
                    InternalWriteByte(b, 8);
                    InternalWriteByte(c, 8);
                    InternalWriteByte(d, bits - 24);
                    break;
            }
        }

        internal uint ReadUInt(int bits) {

            int a = 0, b = 0, c = 0, d = 0;

            switch ((bits + 7) / 8) {
                case 1:
                    a = InternalReadByte(bits);
                    break;
                case 2:
                    a = InternalReadByte(8);
                    b = InternalReadByte(bits - 8);
                    break;
                case 3:
                    a = InternalReadByte(8);
                    b = InternalReadByte(8);
                    c = InternalReadByte(bits - 16);
                    break;
                case 4:
                    a = InternalReadByte(8);
                    b = InternalReadByte(8);
                    c = InternalReadByte(8);
                    d = InternalReadByte(bits - 24);
                    break;
            }

            return (uint)(a | (b << 8) | (c << 16) | (d << 24));
        }

        public void WriteUInt(uint value) {
            WriteUInt(value, 32);
        }

        public uint ReadUInt() {
            return ReadUInt(32);
        }

        internal void WriteInt(int value, int bits) {
            WriteUInt((uint)value, bits);
        }

        internal int ReadInt(int bits) {
            return (int)ReadUInt(bits);
        }

        public void WriteInt(int value) {
            WriteInt(value, 32);
        }

        public int ReadInt() {
            return ReadInt(32);
        }

        internal void WriteULong(ulong value, int bits) {
            if (bits <= 32) WriteUInt((uint)(value & 0xFFFFFFFF), bits);
            else {
                WriteUInt((uint)(value), 32);
                WriteUInt((uint)(value >> 32), bits - 32);
            }
        }

        internal ulong ReadULong(int bits) {
            if (bits <= 32) return ReadUInt(bits);
            ulong a = ReadUInt(32);
            ulong b = ReadUInt(bits - 32);
            return a | (b << 32);
        }

        public void WriteULong(ulong value) {
            WriteULong(value, 64);
        }

        public ulong ReadULong() {
            return ReadULong(64);
        }

        internal void WriteLong(long value, int bits) {
            WriteULong((ulong)value, bits);
        }

        internal long ReadLong(int bits) {
            return (long)ReadULong(bits);
        }

        public void WriteLong(long value) {
            WriteLong(value, 64);
        }

        public long ReadLong() {
            return ReadLong(64);
        }

        public void WriteHalf(float value) {
            WriteUShort(HalfConverter.FloatToHalf(value), 16);
        }

        public float ReadHalf() {
            return HalfConverter.HalfToFloat(ReadUShort(16));
        }

        public void WriteFloat(float value) {
            FpBytes bytes = value;
            InternalWriteByte(bytes.B0, 8);
            InternalWriteByte(bytes.B1, 8);
            InternalWriteByte(bytes.B2, 8);
            InternalWriteByte(bytes.B3, 8);
        }

        public float ReadFloat() {
            var bytes = default(FpBytes);
            bytes.B0 = InternalReadByte(8);
            bytes.B1 = InternalReadByte(8);
            bytes.B2 = InternalReadByte(8);
            bytes.B3 = InternalReadByte(8);
            return bytes.Float;
        }

        public void WriteDouble(double value) {
            FpBytes bytes = value;
            InternalWriteByte(bytes.B0, 8);
            InternalWriteByte(bytes.B1, 8);
            InternalWriteByte(bytes.B2, 8);
            InternalWriteByte(bytes.B3, 8);
            InternalWriteByte(bytes.B4, 8);
            InternalWriteByte(bytes.B5, 8);
            InternalWriteByte(bytes.B6, 8);
            InternalWriteByte(bytes.B7, 8);
        }

        public double ReadDouble() {
            var bytes = default(FpBytes);
            bytes.B0 = InternalReadByte(8);
            bytes.B1 = InternalReadByte(8);
            bytes.B2 = InternalReadByte(8);
            bytes.B3 = InternalReadByte(8);
            bytes.B4 = InternalReadByte(8);
            bytes.B5 = InternalReadByte(8);
            bytes.B6 = InternalReadByte(8);
            bytes.B7 = InternalReadByte(8);
            return bytes.Double;
        }

        public void WriteVector3(Vector3 vector) {
            if (HalfFloats) {
                WriteHalf(vector.x);
                WriteHalf(vector.y);
                WriteHalf(vector.z);
            } else {
                WriteFloat(vector.x);
                WriteFloat(vector.y);
                WriteFloat(vector.z);
            }
        }

        public Vector3 ReadVector3() {
            if (HalfFloats) return new Vector3(ReadHalf(), ReadHalf(), ReadHalf());
            else return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }

        public void WriteVector2(Vector2 vector) {
            if (HalfFloats) {
                WriteHalf(vector.x);
                WriteHalf(vector.y);
            } else {
                WriteFloat(vector.x);
                WriteFloat(vector.y);
            }
        }

        public Vector2 ReadVector2() {
            if (HalfFloats) return new Vector2(ReadHalf(), ReadHalf());
            else return new Vector2(ReadFloat(), ReadFloat());
        }

        public void WriteQuaternion(Quaternion quaternion) {
            if (HalfFloats) {
                WriteHalf(quaternion.x);
                WriteHalf(quaternion.y);
                WriteHalf(quaternion.z);
                WriteHalf(quaternion.w);
            } else {
                WriteFloat(quaternion.x);
                WriteFloat(quaternion.y);
                WriteFloat(quaternion.z);
                WriteFloat(quaternion.w);
            }
        }

        public Quaternion ReadQuaternion() {
            if (HalfFloats) return new Quaternion(ReadHalf(), ReadHalf(), ReadHalf(), ReadHalf());
            else return new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
        }

        public void WriteByteArray(byte[] from) {
            WriteByteArray(from, 0, from.Length);
        }

        public void WriteByteArray(byte[] from, int count) {
            WriteByteArray(from, 0, count);
        }

        public void WriteByteArray(byte[] from, int offset, int count) {
            int p = Pos >> 3;
            int bitsUsed = Pos % 8;
            int bitsFree = 8 - bitsUsed;

            if (bitsUsed == 0) Buffer.BlockCopy(from, offset, Data, p, count);
            else {
                for (int i = 0; i < count; ++i) {
                    byte value = from[offset + i];

                    Data[p] &= (byte)(0xFF >> bitsFree);
                    Data[p] |= (byte)(value << bitsUsed);

                    p += 1;

                    Data[p] &= (byte)(0xFF << bitsUsed);
                    Data[p] |= (byte)(value >> bitsFree);
                }
            }

            Pos += (count * 8);
        }

        public void ReadByteArray(byte[] to) {
            ReadByteArray(to, 0, to.Length);
        }

        public void ReadByteArray(byte[] to, int count) {
            ReadByteArray(to, 0, count);
        }

        public void ReadByteArray(byte[] to, int offset, int count) {
            int p = Pos >> 3;
            int bitsUsed = Pos % 8;

            if (bitsUsed == 0) Buffer.BlockCopy(Data, p, to, offset, count);
            else {
                int bitsNotUsed = 8 - bitsUsed;

                for (int i = 0; i < count; ++i) {
                    int first = Data[p] >> bitsUsed;

                    p += 1;

                    int second = Data[p] & (255 >> bitsNotUsed);
                    to[offset + i] = (byte)(first | (second << bitsNotUsed));
                }
            }

            Pos += (count * 8);
        }

        internal void WriteString(string value, Encoding encoding) {
            WriteString(value, encoding, int.MaxValue);
        }

        internal void WriteString(string value, Encoding encoding, int length) {
            if (string.IsNullOrEmpty(value)) WriteUShort(0);
            else {
                if (length < value.Length) value = value.Substring(0, length);

                WriteUShort((ushort)encoding.GetByteCount(value));
                WriteByteArray(encoding.GetBytes(value));
            }
        }

        public void WriteString(string value) {
            WriteString(value, Encoding.UTF8);
        }

        internal string ReadString(Encoding encoding) {
            int byteCount = ReadUShort();

            if (byteCount == 0) return "";

            var bytes = new byte[byteCount];
            ReadByteArray(bytes);

            return encoding.GetString(bytes);
        }

        public string ReadString() {
            return ReadString(Encoding.UTF8);
        }

        private void InternalWriteByte(byte value, int bits) {
            if (bits <= 0) return;

            value = (byte)(value & (0xFF >> (8 - bits)));

            int p = Pos >> 3;
            int bitsUsed = Pos & 0x7;
            int bitsFree = 8 - bitsUsed;
            int bitsLeft = bitsFree - bits;

            if (bitsLeft >= 0) {
                int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));
                Data[p] = (byte)((Data[p] & mask) | (value << bitsUsed));
            } else {
                Data[p] = (byte)((Data[p] & (0xFF >> bitsFree)) | (value << bitsUsed));
                Data[p + 1] = (byte)((Data[p + 1] & (0xFF << (bits - bitsFree))) | (value >> bitsFree));
            }

            Pos += bits;
        }

        private byte InternalReadByte(int bits) {
            if (bits <= 0) return 0;

            byte value;
            int p = Pos >> 3;
            int bitsUsed = Pos % 8;

            if (bitsUsed == 0 && bits == 8) value = Data[p];
            else {
                int first = Data[p] >> bitsUsed;
                int remainingBits = bits - (8 - bitsUsed);

                if (remainingBits < 1) value = (byte)(first & (0xFF >> (8 - bits)));
                else {
                    int second = Data[p + 1] & (0xFF >> (8 - remainingBits));
                    value = (byte)(first | (second << (bits - remainingBits)));
                }
            }

            Pos += bits;
            return value;
        }

        internal void CopyTo(NetStream stream) {
            if (Position == 0) {
                if (WriteLength) stream.WriteUShort((ushort)Position, 14);
                return;
            }
            int count = Position >> 3;
            int bits = Position % 8;
            if (WriteLength) stream.WriteUShort((ushort)Position, 14);
            stream.WriteByteArray(ByteBuffer, count);
            if (bits != 0) stream.WriteByte(ByteBuffer[count], bits);
        }

        public NetStream Copy() {
            var stream = New();
            if (WriteLength) {
                WriteLength = false;
                CopyTo(stream);
                WriteLength = true;
            } else {
                CopyTo(stream);
            }
            return stream;
        }

        internal NetStream ReadNetStream() {
            NetStream newStream = New();
            newStream.Size = ReadUShort(14);
            int count = newStream.Size >> 3;
            int bits = newStream.Size % 8;
            ReadByteArray(newStream.ByteBuffer, count);
            if (bits != 0) newStream.ByteBuffer[count] = ReadByte(bits);
            return newStream;
        }

        private const int MaxSize = 65536;
        private static readonly Queue<NetStream> Pool = new Queue<NetStream>();

        /// <summary> Releases a stream back to the pool for reuse. This should be called once a stream is no longer needed. </summary>
        public void Release() {
            if (Pool.Count > MaxSize) return;
            Reset();
            Connection = null;
            Socket = null;
            WriteLength = false;
            if (!Pool.Contains(this)) Pool.Enqueue(this);
        }

        private static NetStream CreateFromPool() {
            return Pool.Count == 0 ? new NetStream(new byte[1400]) : Pool.Dequeue();
        }
    }
}