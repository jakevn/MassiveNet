// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MassiveNet {

    /// <summary>
    /// NetRPC is an attribute used to identify methods which should be cached and targeted for incoming RPCs.
    /// </summary>
    public class NetRPCAttribute : Attribute {}

    /// <summary>
    /// Tracks numeric ids for RPC method names and invokes incoming RPC messages.
    /// </summary>
    internal class RpcDispatcher {
        private RpcDispatcher() {}

        internal NetSocket Socket;

        internal RpcDispatcher(NetSocket socket) {
            Socket = socket;
        }

        /// <summary> Invokes RPC with parameters from the NetMessage. </summary>
        internal void Invoke(object instance, string methodName, NetMessage message, NetConnection sender) {

            RpcMethodInfo rpcInfo = RpcInfoCache.Get(methodName);
            MethodInfo method = rpcInfo.MethodInfoLookup[instance.GetType()];

            if (rpcInfo.TakesRequests && Socket.Request.Dispatch(message, sender, method, instance)) return;

            if (method.ReturnType == typeof (IEnumerator)) {
                var coroutine = (IEnumerator) method.Invoke(instance, message.Parameters);
                var behaviour = (MonoBehaviour) instance;
                if (coroutine != null) behaviour.StartCoroutine(coroutine);
            }
            else method.Invoke(instance, message.Parameters);
        }

        /// <summary> The protocol authority (server) generates network friendly IDs for RPCs, which we store here: </summary>
        private readonly Dictionary<ushort, string> idToName = new Dictionary<ushort, string>();

        internal int IdCount {
            get { return idToName.Count; }
        }

        /// <summary> Remote RPC identifiers. The string name of the RPC is converted to a numeric ID for header use: </summary>
        private readonly Dictionary<string, ushort> nameToId = new Dictionary<string, ushort>();

        internal int NameCount {
            get { return nameToId.Count; }
        }

        /// <summary> Returns true if the RPC method has a return value or has a NetRequest parameter. </summary>
        internal bool TakesRequests(ushort rpcId) {
            return HasName(rpcId) && RpcInfoCache.TakesRequests(IdToName(rpcId));
        }

        /// <summary> Returns true if the provided rpcID is associated with a method. </summary>
        internal bool Exists(ushort rpcId) {
            return HasName(rpcId) && RpcInfoCache.Exists(IdToName(rpcId));
        }

        internal bool HasId(string name) {
            return nameToId.ContainsKey(name);
        }

        internal bool HasName(ushort id) {
            return idToName.ContainsKey(id);
        }

        internal List<Type> ParamTypes(ushort id) {
            return RpcInfoCache.ParamTypes(IdToName(id));
        }

        internal string IdToName(ushort id) {
            return idToName[id];
        }

        internal ushort NameToId(string name) {
            return nameToId[name];
        }

        /// <summary> Processes a request to assign an RPC method name to a numeric ID. </summary>
        internal void ReceiveAssignmentRequest(NetMessage netMessage, NetConnection connection) {
            // If we aren't responsible for RPC ID assignment, ignore request.
            if (!Socket.ProtocolAuthority) return;

            var rpcName = (string) netMessage.Parameters[0];
            if (!HasId(rpcName)) {
                if (NameCount > 1800) return;
                AssignRemoteRpc(rpcName);
                // Send new assignment to all connections.
                foreach (NetConnection conn in Socket.Connections) {
                    if (conn == connection || conn.IsServer) continue;
                    Socket.Command.Send((int) Cmd.RemoteAssignment, conn, nameToId[rpcName], rpcName);
                }
            }
            Socket.Command.Send((int) Cmd.AssignmentResponse, connection, nameToId[rpcName], rpcName);
        }

        /// <summary> Assigns a network-friendly numeric ID for a remote method. </summary>
        private void AssignRemoteRpc(string rpcName) {
            ushort id = (ushort) (nameToId.Count + 1);
            nameToId.Add(rpcName, id);
            idToName.Add(id, rpcName);
        }

        /// <summary> Processes a response to an RPC assignment request. The assigned id and method name are added to the LocalRpcs dictionary. </summary>
        internal void ReceiveAssignmentResponse(NetMessage netMessage, NetConnection connection) {

            if (!connection.IsServer && !connection.IsPeer) return;

            var id = (ushort) netMessage.Parameters[0];
            var methodName = (string) netMessage.Parameters[1];

            if (RpcInfoCache.Exists(methodName) && !idToName.ContainsKey(id)) {
                idToName.Add(id, methodName);
                if (!nameToId.ContainsKey(methodName)) nameToId.Add(methodName, id);
            }
            else NetLog.Error("Cannot assign local RPC. ID: " + id + " MethodName: " + methodName);

            if (idToName.Count == RpcInfoCache.Count) Socket.SendRequirementsMet(connection);
        }

        internal int WaitingForRpcs;

        /// <summary> Adds the assigned id and remote method name to the RemoteRpcIds dictionary. </summary>
        internal void ReceiveRemoteAssignment(NetMessage netMessage, NetConnection connection) {

            if (!connection.IsServer && !connection.IsPeer) return;

            var rpcId = (ushort)netMessage.Parameters[0];
            var rpcName = (string) netMessage.Parameters[1];

            if (!HasId(rpcName) && !HasName(rpcId)) {
                nameToId.Add(rpcName, rpcId);
                idToName.Add(rpcId, rpcName);
                foreach (NetConnection conn in Socket.Connections) {
                    if (conn == connection || conn.IsServer) continue;
                    Socket.Command.Send((int)Cmd.RemoteAssignment, conn, nameToId[rpcName], rpcName);
                }
            }

            WaitingForRpcs--;
            //NetLog.Trace("Waiting for RPCs: " + WaitingForRpcs);
            if (WaitingForRpcs != 0) return;

            if (IdCount >= RpcInfoCache.Count) Socket.SendRequirementsMet(connection);
            else RequestAssignments(connection);
        }

        /// <summary> Sends a message for each RPC method names and IDs to the supplied connection. </summary>
        internal void SendLocalAssignments(NetConnection connection) {

            foreach (KeyValuePair<ushort, string> kvp in idToName) {
                Socket.Command.Send((int) Cmd.RemoteAssignment, connection, kvp.Key, kvp.Value);
            }
        }

        /// <summary> Called by the server to generate IDs for local RPCs. </summary>
        internal void AssignLocalRpcs() {
            int i = 0;
            foreach (KeyValuePair<string, RpcMethodInfo> kvp in RpcInfoCache.RpcMethods()) {
                for (i++; i < 1800; i++) {
                    if (HasName((ushort) i)) continue;
                    idToName.Add((ushort) i, kvp.Value.Name);
                    nameToId.Add(kvp.Value.Name, (ushort) i);
                    break;
                }
            }
        }

        /// <summary> Sends a request to the server for each local RPC method name that needs an ID assignment. </summary>
        internal void RequestAssignments(NetConnection connection) {

            foreach (string methodName in RpcInfoCache.RpcMethods().Keys) {
                if (HasId(methodName)) {
                    if (!HasName(NameToId(methodName))) idToName.Add(NameToId(methodName), methodName);
                    continue;
                }
                Socket.Command.Send((int) Cmd.AssignmentRequest, connection, methodName);
            }

            if (IdCount >= RpcInfoCache.Count) Socket.SendRequirementsMet(connection);
        }

    }
}