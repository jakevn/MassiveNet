// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Net;

namespace MassiveNet
{
    /// <summary>
    /// Contains NetSocket events, their backing delegates, and methods which null-check and call events.
    /// </summary>
    public class NetSocketEvents
    {

        public delegate void OnSocketStartEvent();
        /// <summary> Fired after socket startup is complete. </summary>
        public event OnSocketStartEvent OnSocketStart;

        internal void SocketStart() {
            if (OnSocketStart != null) OnSocketStart();
        }


        public delegate void OnFailedToConnectEvent(IPEndPoint endpoint);
        /// <summary> Fired when failed to connect to an endpoint; Provides the EndPoint. </summary>
        public event OnFailedToConnectEvent OnFailedToConnect;

        internal void FailedToConnect(IPEndPoint endpoint) {
            if (OnFailedToConnect != null) OnFailedToConnect(endpoint);
        }


        public delegate bool OnClientApprovalEvent(IPEndPoint endpoint, NetStream data);
        /// <summary> An incoming client connection has an endpoint and a stream of data.
        /// If true is returned, the client is approved and a connection is created. False
        /// will send a deny to the client. </summary>
        public event OnClientApprovalEvent OnClientApproval;

        internal bool ClientApproval(IPEndPoint endpoint, NetStream data) {
            if (OnClientApproval == null) return true;
            else return OnClientApproval(endpoint, data);
        }


        public delegate bool OnPeerApprovalEvent(IPEndPoint endpoint, NetStream data);
        /// <summary> This is fired after approval but before a connection is created. 
        /// Using the supplied endpoint and stream, return true if the connection should be
        /// considered a peer, false if it is not a peer. </summary>
        public event OnPeerApprovalEvent OnPeerApproval;

        internal bool PeerApproval(IPEndPoint endpoint, NetStream data) {
            return (OnPeerApproval != null && OnPeerApproval(endpoint, data));
        }


        public delegate void OnClientConnectedEvent(NetConnection connection);
        /// <summary> Fired after an incoming connection is created. Provides the connection. </summary>
        public event OnClientConnectedEvent OnClientConnected;

        internal void ClientConnected(NetConnection connection) {
            if (OnClientConnected != null) OnClientConnected(connection);
        }


        public delegate void OnClientDisconnectedEvent(NetConnection connection);
        /// <summary> Fired after a client connection is removed from the connections list. Provides
        /// the connection. </summary>
        public event OnClientDisconnectedEvent OnClientDisconnected;

        internal void ClientDisconnected(NetConnection connection) {
            if (OnClientDisconnected != null) OnClientDisconnected(connection);
        }


        public delegate void OnConnectedToServerEvent(NetConnection connection);
        /// <summary> Fired after an outgoing connection is created. Provides the connection.  </summary>
        public event OnConnectedToServerEvent OnConnectedToServer;

        internal void ConnectedToServer(NetConnection connection) {
            if (OnConnectedToServer != null) OnConnectedToServer(connection);
        }


        public delegate void OnDisconnectedFromServerEvent(NetConnection connection);
        /// <summary> Fired after a server connection is removed from the connections list. Provides
        /// the connection. </summary>
        public event OnDisconnectedFromServerEvent OnDisconnectedFromServer;

        internal void DisconnectedFromServer(NetConnection connection) {
            if (OnDisconnectedFromServer != null) OnDisconnectedFromServer(connection);
        }


        public delegate void OnWritePeerApprovalEvent(IPEndPoint connectingTo, NetStream stream);
        /// <summary> Triggered after ConnectToPeer, any data that is used by the receiving end to determine
        /// if we are a legit peer should be written to stream. </summary>
        public event OnWritePeerApprovalEvent OnWritePeerApproval;

        internal void WritePeerApproval(IPEndPoint connectingTo, NetStream stream) {
            if (OnWritePeerApproval != null) OnWritePeerApproval(connectingTo, stream);
        }


        public delegate void OnWriteApprovalEvent(IPEndPoint connectingTo, NetStream stream);
        /// <summary> Triggered after Connect, any data that is used by the receiving end to determine
        /// if we are a legit client should be written to stream. </summary>
        public event OnWriteApprovalEvent OnWriteApproval;

        internal void WriteApproval(IPEndPoint connectingTo, NetStream stream) {
            if (OnWriteApproval != null) OnWriteApproval(connectingTo, stream);
        }


        public delegate void OnPeerConnectedEvent(NetConnection connection);
        /// <summary> Fired after a peer connection is created. Provides the connection.
        /// This will fire after OnClientConnected or OnConnectedToServer. </summary>
        public event OnPeerConnectedEvent OnPeerConnected;

        internal void PeerConnected(NetConnection connection) {
            if (OnPeerConnected != null) OnPeerConnected(connection);
        }


        public delegate void OnPeerDisconnectedEvent(NetConnection connection);
        /// <summary> Fired after a peer connection is removed from the connections list. Provides
        /// the connection. This will fire after OnClientDisconnected or OnDisconnectedFromServer. </summary>
        public event OnPeerDisconnectedEvent OnPeerDisconnected;

        internal void PeerDisconnected(NetConnection connection) {
            if (OnPeerDisconnected != null) OnPeerDisconnected(connection);
        }


        internal delegate void OnMessageReceivedEvent(NetMessage message, NetConnection connection);
        /// <summary> Fired after a message is received. Provies the message and connection it came from. </summary>
        internal event OnMessageReceivedEvent OnMessageReceived;

        internal void MessageReceived(NetMessage message, NetConnection connection) {
            if (OnMessageReceived != null) OnMessageReceived(message, connection);
        }

        internal delegate void OnAddRequirementsEvent(NetConnection connection);

        internal event OnAddRequirementsEvent OnAddRequirements;

        internal void AddRequirements(NetConnection connection) {
            if (OnAddRequirements != null) OnAddRequirements(connection);
        }
    }
}
