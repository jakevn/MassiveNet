// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// NetSerializer handles (de)serialization of custom types, registration of custom type (de)serialization methods,
    /// MassiveNet type serialization methods, and "object" type serialization routing methods.
    /// </summary>
    public class NetSerializer {
        private static readonly Dictionary<Type, Action<NetStream, object>> Serializers =
            new Dictionary<Type, Action<NetStream, object>>();

        private static readonly Dictionary<Type, Func<NetStream, object>> Deserializers =
            new Dictionary<Type, Func<NetStream, object>>();

        public static void Add<T>(Action<NetStream, object> serializer, Func<NetStream, object> deserializer) {
            if (Serializers.ContainsKey(typeof(T))) return;
            Serializers.Add(typeof(T), serializer);
            Deserializers.Add(typeof(T), deserializer);
        }

        public static void Remove(Type type) {
            if (Serializers.ContainsKey(type)) Serializers.Remove(type);
            if (Deserializers.ContainsKey(type)) Deserializers.Remove(type);
        }

        public static void RemoveAll() {
            Serializers.Clear();
            Deserializers.Clear();
        }

        public static bool HasType(Type type) {
            return Serializers.ContainsKey(type);
        }

        internal static void Write(NetStream stream, Type type, object param) {
            Serializers[type](stream, param);
        }

        internal static object Read(NetStream stream, Type type) {
            return Deserializers[type](stream);
        }

        internal static bool TryWriteParam(NetStream stream, object param) {
            try {
                WriteParam(stream, param);
                return true;
            } catch {
                return false;
            }
        }

        internal static void WriteParam(NetStream stream, object param) {

            if (param == null) {
                // TODO: Explicit nullable params should be properly supported.
                stream.WriteBool(false);
                return;
            }

            // Get the object type so we can compare it:
            Type type = param.GetType();

            // Built-in types:
            if (type == typeof(bool)) stream.WriteBool((bool)param);
            else if (type == typeof(byte)) stream.WriteByte((byte)param);
            else if (type == typeof(short)) stream.WriteShort((short)param);
            else if (type == typeof(ushort)) stream.WriteUShort((ushort)param);
            else if (type == typeof(int)) stream.WriteInt((int)param);
            else if (type == typeof(uint)) stream.WriteUInt((uint)param);
            else if (type == typeof(float)) stream.WriteFloat((float)param);
            else if (type == typeof(long)) stream.WriteLong((long)param);
            else if (type == typeof(ulong)) stream.WriteULong((ulong)param);
            else if (type == typeof(double)) stream.WriteDouble((double)param);
            else if (type == typeof(string)) stream.WriteString((string)param);
            else if (type == typeof(Vector2)) stream.WriteVector2((Vector2)param);
            else if (type == typeof(Vector3)) stream.WriteVector3((Vector3)param);
            else if (type == typeof(Quaternion)) stream.WriteQuaternion((Quaternion)param);

            else if (type == typeof(bool[])) {
                bool[] arr = (bool[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteBool(arr[i]);
            } else if (type == typeof(byte[])) {
                byte[] arr = (byte[])param;
                stream.WriteUShort((ushort)arr.Length);
                stream.WriteByteArray(arr, arr.Length);
            } else if (type == typeof(short[])) {
                short[] arr = (short[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteShort(arr[i]);
            } else if (type == typeof(ushort[])) {
                ushort[] arr = (ushort[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteUShort(arr[i]);
            } else if (type == typeof(int[])) {
                int[] arr = (int[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteInt(arr[i]);
            } else if (type == typeof(uint[])) {
                uint[] arr = (uint[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteUInt(arr[i]);
            } else if (type == typeof(float[])) {
                float[] arr = (float[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteFloat(arr[i]);
            } else if (type == typeof(string[])) {
                string[] arr = (string[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteString(arr[i]);
            } else if (type == typeof(char[])) {
                char[] arr = (char[])param;
                stream.WriteUShort((ushort)arr.Length);
                for (int i = 0; i < arr.Length; i++) stream.WriteChar(arr[i]);
            } else if (type == typeof(NetMessage)) {
                WriteNetMessage(stream, (NetMessage)param);
            } else if (type == typeof(NetMessage[])) {
                NetMessage[] arr = (NetMessage[])param;
                stream.WriteByte((byte)arr.Length);
                for (int i = 0; i < arr.Length; i++) WriteNetMessage(stream, arr[i]);
            } else if (type == typeof(IPAddress)) {
                IPAddress address = (IPAddress)param;
                stream.WriteByteArray(address.GetAddressBytes());
            } else if (type == typeof(IPEndPoint)) {
                IPEndPoint ep = (IPEndPoint)param;
                stream.WriteBool(true); // non-null
                WriteParam(stream, ep.Address);
                stream.WriteUShort((ushort)ep.Port);
            } else if (type == typeof(NetZone)) {
                var zone = (NetZone)param;
                stream.WriteUInt(zone.Id);
                bool serializeEndpoint = zone.ServerEndpoint != null;
                stream.WriteBool(serializeEndpoint);
                if (serializeEndpoint) WriteParam(stream, zone.ServerEndpoint);
                stream.WriteVector3(zone.Position);
                stream.WriteInt(zone.ViewIdMin);
                stream.WriteInt(zone.ViewIdMax);
            } else if (type == typeof(NetStream)) {
                NetStream netStream = (NetStream)param;
                netStream.CopyTo(stream);
            } else if (HasType(type)) {
                Write(stream, type, param);
            } else {
                NetLog.Error("Failed to serialize, no serializer found: " + type);
                throw new Exception(
    "Serializer not implemented for type! You must add your own type check and serialization logic to serialize this type: " +
    type);
            }
        }

        private static object ReadParam(NetStream stream, Type type) {
            if (type == typeof(bool)) return stream.ReadBool();
            if (type == typeof(byte)) return stream.ReadByte();
            if (type == typeof(short)) return stream.ReadShort();
            if (type == typeof(ushort)) return stream.ReadUShort();
            if (type == typeof(int)) return stream.ReadInt();
            if (type == typeof(uint)) return stream.ReadUInt();
            if (type == typeof(float)) return stream.ReadFloat();
            if (type == typeof(long)) return stream.ReadLong();
            if (type == typeof(ulong)) return stream.ReadULong();
            if (type == typeof(double)) return stream.ReadDouble();
            if (type == typeof(string)) return stream.ReadString();
            if (type == typeof(Vector2)) return stream.ReadVector2();
            if (type == typeof(Vector3)) return stream.ReadVector3();
            if (type == typeof(Quaternion)) return stream.ReadQuaternion();

            if (type == typeof(bool[])) {
                ushort length = stream.ReadUShort();
                bool[] arr = new bool[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadBool();
                return arr;
            }
            if (type == typeof(byte[])) {
                ushort length = stream.ReadUShort();
                byte[] arr = new byte[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadByte();
                return arr;
            }
            if (type == typeof(ushort[])) {
                ushort length = stream.ReadUShort();
                ushort[] arr = new ushort[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadUShort();
                return arr;
            }
            if (type == typeof(int[])) {
                ushort length = stream.ReadUShort();
                int[] arr = new int[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadInt();
                return arr;
            }
            if (type == typeof(uint[])) {
                ushort length = stream.ReadUShort();
                uint[] arr = new uint[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadUInt();
                return arr;
            }
            if (type == typeof(float[])) {
                ushort length = stream.ReadUShort();
                float[] arr = new float[length];
                for (int i = 0; i < length; i++) arr[i] = stream.ReadFloat();
                return arr;
            }
            if (type == typeof(string[])) {
                ushort length = stream.ReadUShort();
                string[] arr = new string[length];
                for (int i = 0; i < arr.Length; i++) arr[i] = stream.ReadString();
                return arr;
            }
            if (type == typeof(char[])) {
                ushort length = stream.ReadUShort();
                char[] arr = new char[length];
                for (int i = 0; i < arr.Length; i++) arr[i] = stream.ReadChar();
                return arr;
            }
            if (type == typeof(NetMessage)) return ReadNetMessage(stream);
            if (type == typeof(NetMessage[])) {
                byte length = stream.ReadByte();
                NetMessage[] arr = new NetMessage[length];
                for (int i = 0; i < length; i++) arr[i] = ReadNetMessage(stream);
                return arr;
            }
            if (type == typeof(NetConnection)) return stream.Connection;
            if (type == typeof(IPAddress)) {
                byte[] array = new byte[4];
                stream.ReadByteArray(array);
                return new IPAddress(array);
            }
            if (type == typeof(IPEndPoint)) {
                if (!stream.ReadBool()) return null;
                return new IPEndPoint((IPAddress)ReadParam(stream, typeof(IPAddress)), stream.ReadUShort());
            }
            if (type == typeof(NetStream)) return stream.ReadNetStream();
            if (type == typeof(NetZone)) {
                return new NetZone {
                    Id = stream.ReadUInt(),
                    ServerEndpoint =
                        stream.ReadBool()
                            ? (IPEndPoint)ReadParam(stream, typeof(IPEndPoint))
                            : null,
                    Position = stream.ReadVector3(),
                    ViewIdMin = stream.ReadInt(),
                    ViewIdMax = stream.ReadInt()
                };
            }
            if (HasType(type)) return Read(stream, type);

            // We don't know how to deserialize this type
            throw new Exception("Deserializer not implemented for type: " + type);
        }

        internal static void WriteNetMessage(NetStream stream, NetMessage message) {
            stream.WriteUShort(message.MessageId, 11);
            stream.WriteBool(message.ViewId != 0);
            if (message.ViewId != 0) stream.WriteUInt(message.ViewId, 20);
            foreach (object param in message.Parameters) WriteParam(stream, param);
        }

        internal static bool TryWriteMessage(NetStream stream, NetMessage message) {
            int pos = stream.Pos;
            if (!stream.CanWrite(32)) return false;
            stream.WriteUShort(message.MessageId, 11);
            stream.WriteBool(message.ViewId != 0);
            if (message.ViewId != 0) stream.WriteUInt(message.ViewId, 20);
            foreach (object param in message.Parameters) {
                if (TryWriteParam(stream, param)) continue;
                stream.Pos = pos;
                return false;
            }
            return true;
        }

        internal static bool CanReadMessage(NetStream stream) {
            return stream.CanRead(12);
        }

        internal static NetMessage ReadNetMessage(NetStream stream) {
            List<Type> paramTypes;
            ushort messageId = stream.ReadUShort(11);
            uint viewId = 0;
            if (stream.ReadBool()) viewId = stream.ReadUInt(20);

            if (messageId == (int)Cmd.RequestResponse) return CreateResponseMessage(stream, messageId, viewId);

            if (messageId > 1800) {
                if (!stream.Socket.Command.Exists(messageId)) {
                    NetLog.Error("Cannot deserialize message, Command ID not found: " + messageId);
                    return null;
                }
                paramTypes = stream.Socket.Command.ParamTypes(messageId);
            } else {
                if (!stream.Socket.Rpc.Exists(messageId)) {
                    NetLog.Error("Cannot deserialize message, RPC ID not found: " + messageId);
                    return null;
                }
                paramTypes = stream.Socket.Rpc.ParamTypes(messageId);
            }

            NetMessage netMessage = NetMessage.Create(messageId, viewId, paramTypes.Count, false);

            if (stream.Socket.Rpc.TakesRequests(messageId)) return CreateRequestMessage(stream, netMessage, paramTypes);

            for (int i = 0; i < paramTypes.Count; i++) netMessage.Parameters[i] = ReadParam(stream, paramTypes[i]);

            return netMessage;
        }

        private static NetMessage CreateResponseMessage(NetStream stream, ushort messageId, uint viewId) {
            ushort requestId = stream.ReadUShort();
            if (!stream.Socket.Request.Exists(viewId, requestId)) return null;
            bool isSuccessful = stream.ReadBool();
            object result = null;
            if (isSuccessful) {
                Type resultType = stream.Socket.Request.Type(viewId, requestId);
                result = ReadParam(stream, resultType);
            }
            object[] requestParams = { requestId, isSuccessful, result };
            return NetMessage.Create(messageId, viewId, requestParams, true);
        }

        private static NetMessage CreateRequestMessage(NetStream stream, NetMessage netMessage, List<Type> paramTypes) {
            int requestIndex = -1;
            for (int i = 0; i < paramTypes.Count; i++) {
                if (paramTypes[i].IsGenericType && paramTypes[i].GetGenericTypeDefinition() == typeof(NetRequest<>)) {
                    requestIndex = i;
                    netMessage.Parameters[i] = null;
                    continue;
                }
                netMessage.Parameters[i] = ReadParam(stream, paramTypes[i]);
            }

            if (!stream.CanRead(16)) return netMessage;

            ushort requestId = stream.ReadUShort();
            if (requestIndex != -1) {
                netMessage.Parameters[requestIndex] = Activator.CreateInstance(paramTypes[requestIndex],
                    netMessage.ViewId, requestId, stream.Connection);
            } else {
                object[] newParams = new object[paramTypes.Count + 1];
                netMessage.Parameters.CopyTo(newParams, 0);
                newParams[newParams.Length - 1] = requestId;
                netMessage.Parameters = newParams;
            }
            return netMessage;
        }
    }
}