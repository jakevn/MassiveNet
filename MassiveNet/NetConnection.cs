// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// NetConnection maintains high-level connection functionality such as group membership, scope, and view ownership.
    /// </summary>
    public class NetConnection {
        /// <summary> The socket that created this connection. </summary>
        public readonly NetSocket Socket;
        /// <summary> The endpoint that this connection represents. </summary>
        public readonly IPEndPoint Endpoint;

        internal readonly uint Id;
        internal readonly NetChannelUnreliable Unreliable;
        internal readonly NetChannelReliable Reliable;

        internal uint LastReceiveTime;
        internal uint LastSendTime;
        internal readonly uint Created;

        internal NetConnection(bool isServer, bool isPeer, NetSocket socket, IPEndPoint endpoint, uint id = 0) {
            IsServer = isServer;
            IsPeer = isPeer;
            Socket = socket;
            Endpoint = endpoint;
            Id = id;
            Unreliable = new NetChannelUnreliable(this);
            Reliable = new NetChannelReliable(this);
            LastReceiveTime = LastSendTime = Created = NetTime.Milliseconds();
            AddToGroup(0);
        }

        /// <summary> Returns false if this is an incoming connection. </summary>
        public bool IsServer { get; internal set; }

        /// <summary> PeerApproval is true if NetSocket.Events.OnPeerApproval returns true for this connection. </summary>
        public bool IsPeer { get; internal set; }

        /// <summary> Returns the string form of this connection's IP address, e.g.: "192.168.1.1" </summary>
        public IPAddress Address {
            get { return Endpoint.Address; }
        }

        /// <summary> Returns the int form of this conenction's port, e.g.: 17010  </summary>
        public int Port {
            get { return Endpoint.Port; }
        }

        /// <summary> The approximate ping (in milliseconds) for the connection. </summary>
        internal uint Ping { get; set; }

        /// <summary> When a reliable header is processed, ping is updated with 1/10 sample. </summary>
        internal void UpdatePing(uint receiveTime, uint sendTime, uint ackTime) {
            uint rtt = receiveTime - sendTime;
            // The adjusted time is the RTT modified by ack delay provided in header:
            uint adjusted = (uint)(rtt - Mathf.Clamp(ackTime, 0, rtt));
            // If we don't yet have a ping, assign initial value:
            if (Ping == 0) Ping = ackTime;
            else Ping = (uint)((Ping * 0.9f) + (adjusted * 0.1f));
        }

        /// <summary> Performs end-frame tasks such as flushing stream & checking timeouts. </summary>
        internal void EndOfFrame(uint currentTime) {
            if (ShouldDisconnect(currentTime)) {
                NetLog.Info("Disconnecting connection due to overflow or timeout: " + Endpoint);
                Disconnect();
                return;
            }
            if (!Reliable.FlushStream() && Reliable.ShouldForceAck(currentTime)) Reliable.ForceAck();
            if (!Unreliable.FlushStream() && ShouldSendHeartbeat(currentTime)) Unreliable.SendHeartbeat();
            Reliable.CheckTimeouts(currentTime);
        }

        /// <summary> Returns true if it has been more than 1 second since last sent message. </summary>
        internal bool ShouldSendHeartbeat(uint currentTime) {
            return (currentTime - LastSendTime > 1000);
        }

        /// <summary> Returns true if the reliable send window is full or connection timeout. </summary>
        internal bool ShouldDisconnect(uint currentTime) {
            return (Reliable.SendWindowFull || currentTime - LastReceiveTime > 6000);
        }

        /// <summary> Disconnects this NetConnection and performs cleanup. </summary>
        public void Disconnect() {
            Socket.Disconnect(this);
        }

        /// <summary> Updates receive time and forwards stream to proper channel for deserialization. </summary>
        internal void ReceiveStream(NetStream stream) {
            stream.Connection = this;
            LastReceiveTime = NetTime.Milliseconds();
            bool reliable = stream.ReadBool();
            if (!reliable) Unreliable.DeserializeStream(stream);
            else Reliable.RouteIncomingStream(stream);
        }

        /// <summary> Sends a message to this connection. </summary>
        internal void Send(NetMessage message) {
            if (this == Socket.Self) NetLog.Warning("Trying to send message to self.");
            if (!message.Reliable) Unreliable.SerializeMessage(message);
            else Reliable.SerializeReliableMessage(message);
        }

        /// <summary> Convenient way of storying and retrieving a connection's primary View, if any. </summary>
        public NetView View { get; set; }

        /// <summary> Returns true if this connection has a View assigned as a primary. </summary>
        public bool HasView {
            get { return View != null; }
        }

        /// <summary> If connection has a View assigned, returns the scope for the View. </summary>
        internal NetScope Scope { get { return HasView ? View.Scope : InternalScope; } }

        /// <summary> When a connection does not have a View yet needs a calculated scope, this can be used. </summary>
        internal NetScope InternalScope;

        internal bool HasScope { get { return Scope != null; } }

        /// <summary> The list of ViewIDs for NetViews that this connection is authorized to communicate with. </summary>
        internal List<int> Authorizations = new List<int>();

        /// <summary> Returns true if this connection has control over the NetView associated with the provided ViewID. </summary>
        public bool Authorized(int viewId) {
            return Authorizations.Contains(viewId);
        }

        /// <summary> Adds the supplied ViewID to the controller list for this connection. </summary>
        public void AddAuthorization(int viewId) {
            if (!Authorizations.Contains(viewId)) Authorizations.Add(viewId);
        }

        /// <summary> Removes the supplied ViewID from the controller list for this connection. </summary>
        public void RemoveAuthorization(int viewId) {
            if (Authorizations.Contains(viewId)) Authorizations.Remove(viewId);
        }

        /// <summary> Clears the authorization list for this connection. </summary>
        public void ResetAuthorizations() {
            Authorizations.Clear();
        }

        /// <summary> The list of groups this connection is a member of. </summary>
        private readonly List<int> groups = new List<int>();

        /// <summary> Returns true if this connection is a member of the supplied group. </summary>
        public bool InGroup(int groupId) {
            return groups.Contains(groupId);
        }

        /// <summary> Adds this connection to the group associated with the supplied GroupID. </summary>
        public void AddToGroup(int groupId) {
            if (!groups.Contains(groupId)) groups.Add(groupId);
        }

        /// <summary> Removes this connection from the group associated with the supplied GroupID. </summary>
        public void RemoveFromGroup(int groupId) {
            if (groups.Contains(groupId)) groups.Remove(groupId);
        }
    }
}