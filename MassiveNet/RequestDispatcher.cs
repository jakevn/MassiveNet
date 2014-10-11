using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace MassiveNet {
    internal class RequestDispatcher {
        internal NetSocket Socket;

        private RequestDispatcher() {}

        internal RequestDispatcher(NetSocket socket) {
            Socket = socket;
        }

        /// <summary> Request ID -> NetRequest instance lookup. When receiving a response to a NetRequest, it is directed using this. </summary>
        private readonly Dictionary<uint, object> requests = new Dictionary<uint, object>();

        public Request<T> Send<T>(string methodName, NetConnection target, params object[] parameters) {
            return Send<T>(0, methodName, target, parameters);
        }

        /// <summary> Generates the key to be used for the request dictionary lookup. </summary>
        private uint CreateKey(uint viewId, ushort requestId) {
            return (viewId << 16) ^ requestId;
        }

        /// <summary> Returns true if there is an active request matching the supplied ViewID and RequestID. </summary>
        internal bool Exists(uint viewId, ushort requestId) {
            return requests.ContainsKey(CreateKey(viewId, requestId));
        }

        private ushort requestIdIndex = 1;

        internal Request<T> Send<T>(uint viewId, string methodName, NetConnection target, object[] parameters) {

            if (!Socket.Rpc.HasId(methodName)) throw new Exception("Cannot create request. Id does not exist for RPC: " + methodName);

            if (requestIdIndex == ushort.MaxValue) requestIdIndex = 1;

            ushort requestId = requestIdIndex++;
            uint key = CreateKey(viewId, requestId);
            if (requests.ContainsKey(key)) {
                throw new Exception(
                    "RequestID already exists. Requests either not completing or being sent too quickly.");
            }

            object[] newParams = new object[parameters.Length + 1];
            parameters.CopyTo(newParams, 0);
            newParams[newParams.Length - 1] = requestId;

            NetMessage netMessage = NetMessage.Create(Socket.Rpc.NameToId(methodName), viewId, newParams, true);

            NetRequest<T> request = new NetRequest<T>(Socket, viewId, requestId);

            requests.Add(key, request);
            target.Send(netMessage);

            return request;
        }

        internal bool Dispatch(NetMessage message, NetConnection connection, MethodInfo method, object instance) {

            List<Type> paramTypes = RpcInfoCache.ParamTypes(method.Name);

            if (paramTypes.Count >= message.Parameters.Length) return false;

            if (method.ReturnType == typeof (IEnumerator) || method.ReturnType == typeof (void)) return false;

            ushort requestId = (ushort) message.Parameters[message.Parameters.Length - 1];
            object[] culledParams = new object[paramTypes.Count];

            for (int i = 0; i < paramTypes.Count; i++) culledParams[i] = message.Parameters[i];

            message.Parameters = culledParams;

            object result = method.Invoke(instance, message.Parameters);

            connection.Send(NetMessage.Create((ushort) Cmd.RequestResponse, message.ViewId,
                new object[] {requestId, true, result}, true));

            return true;
        }

        /// <summary>
        /// Sets request result and removes request from queue.
        /// </summary>
        internal void SetResponse(NetMessage message, NetConnection connection) {

            if (message.Parameters.Length < 2 || !(message.Parameters[0] is ushort)) {
                NetLog.Error("Received malformed request response. Discarding.");
                return;
            }

            ushort requestId = (ushort) message.Parameters[0];
            uint key = CreateKey(message.ViewId, requestId);

            if (!requests.ContainsKey(key)) {
                //NetLog.Trace("Active request doesn't exist for response. Discarding.");
                return;
            }

            object requestObj = requests[key];
            requests.Remove(key);

            if ((bool) message.Parameters[1]) {
                MethodInfo setResponse = requestObj.GetType().GetMethod("SetResponse");
                setResponse.Invoke(requestObj, message.Parameters);
            }
            else {
                MethodInfo failureResponse = requestObj.GetType().GetMethod("FailureResponse", BindingFlags.NonPublic);
                failureResponse.Invoke(requestObj, null);
            }
        }

        /// <summary> Returns the type for the request. The type is needed for deserialization purposes. </summary>
        internal Type Type(uint viewId, ushort requestId) {

            uint key = CreateKey(viewId, requestId);

            if (!requests.ContainsKey(key)) throw new Exception("RequestID invalid/expired: " + requestId);
            return requests[key].GetType().GetGenericArguments()[0];
        }

        /// <summary> Removes the request from the active requests list. </summary>
        internal void Remove(uint viewId, ushort requestId) {

            uint key = CreateKey(viewId, requestId);

            if (requests.ContainsKey(key)) requests.Remove(key);
        }
    }
}