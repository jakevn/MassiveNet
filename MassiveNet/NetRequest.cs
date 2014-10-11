// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Represents a request sent over the network, a coroutine for waiting on said request, and
    /// the result (if successful).
    /// </summary>
    public sealed class NetRequest<T> : Request<T> {
        public override T Result {
            get { return result; }
            set {
                result = value;
                resultSet = true;
                ResponseSet();
            }
        }

        public override bool IsSuccessful { get; set; }
        public override Coroutine WaitUntilDone { get; set; }

        public override IEnumerator RequestCoroutine() {
            while (resultSet == false && !resultFail && timeout > 0) {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (resultSet) IsSuccessful = true;
            else socket.Request.Remove(viewId, requestId);
        }

        private NetSocket socket;
        private NetConnection requestor;
        private uint viewId;
        private ushort requestId;
        private float timeout;
        private bool resultSet;
        private bool resultFail;
        private T result;

        public void SetResponse(ushort id, bool successful, T response) {
            Result = response;
        }

        internal void FailureResponse() {
            resultFail = true;
        }

        private void ResponseSet() {
            if (requestor == null) return;
            object[] responseParams = {requestId, true, result};
            requestor.Send(NetMessage.Create((ushort) Cmd.RequestResponse, viewId, responseParams, true));
        }

        public NetRequest(NetSocket socket, uint viewId, ushort requestId, float timeout = 3f) {
            this.socket = socket;
            this.viewId = viewId;
            this.timeout = timeout;
            this.requestId = requestId;
            WaitUntilDone = socket.StartCoroutine(RequestCoroutine());
        }

        public NetRequest(uint viewId, ushort requestId, NetConnection requestor) {
            this.requestor = requestor;
            this.viewId = viewId;
            this.requestId = requestId;
        }
    }
}