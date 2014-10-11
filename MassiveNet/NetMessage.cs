// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System;
using System.Collections.Generic;

namespace MassiveNet {
    /// <summary> Stores message options and data. </summary>
    internal class NetMessage {
        /// <summary> Identifies the targeted RPC. </summary>
        internal ushort MessageId;

        /// <summary> Identifies the targeted Command or View. </summary>
        internal uint ViewId;

        /// <summary> Message parameters. </summary>
        internal object[] Parameters;

        /// <summary> Returns true if Options has Reliable set. </summary>
        internal bool Reliable { get; set; }

        private NetMessage(ushort messageId, uint viewId, object[] parameters, bool reliable) {
            ViewId = viewId;
            MessageId = messageId;
            Parameters = parameters;
            Reliable = reliable;
        }

        internal static NetMessage Create(ushort messageId, uint viewId, object[] parameters, bool reliable) {
            return CreateFromPool(messageId, viewId, parameters, reliable);
        }

        internal static NetMessage Create(ushort messageId, uint viewId, int parametersCount, bool reliable) {
            object[] parameters = CreateObjFromPool(parametersCount);
            return Create(messageId, viewId, parameters, reliable);
        }

        internal void Release() {
            if (Pool.Count > MaxSize) return;
            if (Parameters != null) ReturnObjArray(Parameters);
            Parameters = null;
            if (!Pool.Contains(this)) Pool.Enqueue(this);
        }

        private const int MaxSize = 32768;
        private static readonly Queue<NetMessage> Pool = new Queue<NetMessage>();

        private const int ObjMaxSize = 2048;
        private static readonly Dictionary<int, Queue<object[]>> ObjPools = new Dictionary<int, Queue<object[]>>();

        private static void ReturnObjArray(object[] arr) {
            int len = arr.Length;
            Array.Clear(arr, 0, len);
            if (!ObjPools.ContainsKey(len)) ObjPools.Add(len, new Queue<object[]>());
            if (ObjPools[len].Count < ObjMaxSize) ObjPools[len].Enqueue(arr);
        }

        private static object[] CreateObjFromPool(int count) {
            if (ObjPools.ContainsKey(count) && ObjPools[count].Count > 0) return ObjPools[count].Dequeue();
            return new object[count];
        }

        private static NetMessage CreateFromPool(ushort messageId, uint viewId, object[] parameters, bool reliable) {
            if (Pool.Count == 0) return new NetMessage(messageId, viewId, parameters, reliable);
            NetMessage msg = Pool.Dequeue();
            msg.Parameters = parameters;
            msg.MessageId = messageId;
            msg.ViewId = viewId;
            msg.Reliable = reliable;
            return msg;
        }
    }
}