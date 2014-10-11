using System;
using System.Collections.Generic;

namespace MassiveNet {
    internal class CommandDispatcher {
        /// <summary> Command RPC IDs and associated param types. </summary>
        private readonly Dictionary<int, List<Type>> paramTypes = new Dictionary<int, List<Type>>();

        private readonly Dictionary<int, Action<NetMessage, NetConnection>> targets =
            new Dictionary<int, Action<NetMessage, NetConnection>>();

        private readonly Dictionary<int, Action<NetStream, NetConnection>> streamTargets =
            new Dictionary<int, Action<NetStream, NetConnection>>();

        private static readonly List<Type> streamTypes = new List<Type> {typeof (NetStream), typeof (NetConnection)};

        internal void Dispatch(NetMessage message, NetConnection connection) {
            if (targets.ContainsKey(message.MessageId)) targets[message.MessageId](message, connection);
            else if (streamTargets.ContainsKey(message.MessageId))
                streamTargets[message.MessageId]((NetStream) message.Parameters[0], connection);
        }

        /// <summary> Creates and sends a reliable command to a single connection. </summary>
        internal void Send(int id, NetConnection connection, params object[] parameters) {
            connection.Send(NetMessage.Create((ushort) id, 0, parameters, true));
        }

        internal void Send(int id, NetConnection connection, NetStream stream) {
            NetMessage message = NetMessage.Create((ushort) id, 0, 1, true);
            message.Parameters[0] = stream;
            connection.Send(message);
        }

        internal void Register(ushort commandId, Action<NetMessage, NetConnection> target, List<Type> types) {
            if (paramTypes.ContainsKey(commandId)) {
                NetLog.Error("Command Id already in use. Cannot add command.");
                return;
            }
            paramTypes.Add(commandId, types);
            targets.Add(commandId, target);
        }

        internal void Register(ushort commandId, Action<NetStream, NetConnection> target) {
            if (paramTypes.ContainsKey(commandId)) {
                NetLog.Error("Command Id already in use. Cannot add command.");
                return;
            }
            streamTargets.Add(commandId, target);
            paramTypes.Add(commandId, streamTypes);
        }

        internal void Add(ushort id, List<Type> types) {
            if (paramTypes.ContainsKey(id)) throw new Exception("Command ID already in use: " + id);
            paramTypes.Add(id, types);
        }

        /// <summary> Returns true if the supplied command ID is valid. </summary>
        internal bool Exists(int commandId) {
            return paramTypes.ContainsKey(commandId);
        }

        /// <summary> Returns a list of parameters types for the given command ID. </summary>
        internal List<Type> ParamTypes(int commandId) {
            return paramTypes[commandId];
        }

        internal void RegisterParams(ushort commandId, List<Type> paramTypes) {
            if (this.paramTypes.ContainsKey(commandId)) {
                NetLog.Error("Command Id already in use. Cannot add command types.");
                return;
            }
            if (commandId < 1800 || commandId > 2047)
                throw new Exception("Cannot register Command - Range: 1800-2047 - Provided: " + commandId);
            this.paramTypes.Add(commandId, paramTypes);
        }
    }
}