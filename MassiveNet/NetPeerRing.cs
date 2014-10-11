using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace MassiveNet
{
    /*                                              || Design ||
     * 
     *  1a. First peer connection occurs, set up ring:
     *      1b. The accepting connection performs startup
     *      1c. The accepting connection has the start position of 64
     *      1d. The connecting connection is auto-assigned position - 1, which is 63
     *      1e. Since there are only two peers, tail and head are identical
     *      
     *  2a. Second peer connection occurs (now three in ring), complete ring:
     *      2b. If the new peer connects to 64, it is forwarded to 63
     *      2c. 63 assigns the position 62 to the new connection
     *      2d. 63 tells 64 about new tail (62) and passes the address
     *      2e. Since 64 has no head, 64 connects to 62
     *      
     *  3a. Ring established, 30 starts ring communication by sending new tail info to both head and tail
     *      3b. 63 and 64 receive the message that 62 now has a tail (64)
     *      3c. Since 63 is next to talk, 63 will talk after receiving the same message from both its head and tail
     *      3d. 63 doesn't have anything new to say, so it sends a heartbeat to both its head (64) and tail (62)
     *      3e. Since 64 is next to talk, 64 will talk after receiving the same message from both its head and tail
     *      ... etc ...
     *      
     */

    internal class NetPeerRing : MonoBehaviour {
        private NetSocket socket;

        private byte position;
        private ulong ringStatus;

        private byte nextTalker;
        private uint lastListenTime;

        private NetConnection tail;
        private NetConnection head;

        private readonly List<IPEndPoint> peers = new List<IPEndPoint>();
        private readonly List<byte> peerPositions = new List<byte>(); 

        private readonly List<NetConnection> unassignedConnections = new List<NetConnection>();

        private bool headDropped;
        private bool tailDropped;

        private bool headAdded;
        private bool tailAdded;

        private bool lastTail;
        private bool lastHead;

        private void Start() {
            socket = GetComponent<NetSocket>();
            socket.RegisterRpcListener(this);
            socket.Events.OnPeerConnected += PeerConnected;
            socket.Events.OnPeerDisconnected += PeerDisconnected;
        }

        private void StartRing() {
            if (head != null || tail == null) throw new Exception("Invalid state for ring start.");
            position = 64;
            ringStatus = 0UL | (1UL << position - 1);
        }

        private void AddPending(NetConnection connection) {
            unassignedConnections.Add(connection);
        }

        private void PeerConnected(NetConnection connection) {
            if (peers.Contains(connection.Endpoint)) return;
            if (connection.IsServer && head == null) AddHead(connection);
            else if (tail == null) AddTail(connection);
        }

        private void PeerDisconnected(NetConnection connection) {
            if (tail != connection && head != connection) return;
            if (connection.IsServer) RemoveHead(connection);
            else RemoveTail(connection);
        }

        private void AddHead(NetConnection connection) {
            headAdded = true;
            head = connection;
        }

        private void RemoveHead(NetConnection connection) {
            tailDropped = true;
            tail = null;
        }

        private void AddTail(NetConnection connection) {
            if (ringStatus == 0UL && head == null) StartRing();
            tailAdded = true;
            tail = connection;
        }

        private void RemoveTail(NetConnection connection) {
            tailDropped = true;
            tail = null;
        }

        private bool Heard(NetConnection connection) {
            if (connection != head && connection != tail) return false;
            if (connection == head) HeardHead();
            if (connection == tail) HeardTail();
            return true;
        }

        private void HeardHead() {
            lastHead = true;
            if (lastHead && lastTail) Talk();
        }

        private void HeardTail() {
            lastTail = true;
            if (lastHead && lastTail) Talk();
        }

        private void Talk() {
            if (tailAdded && tailDropped) tailAdded = tailDropped = false;
            if (headAdded && headDropped) headAdded = headDropped = false;
            if (tailAdded) TalkTailAdded();
            else if (tailDropped) TalkTailDropped(tail.Endpoint);
            else TalkHeartbeat();
            lastTail = lastHead = false;
        }

        private void TalkTailAdded() {
            socket.Send("ListenTailAdded", head, tail.Endpoint);
            tailAdded = false;
        }

        private void TalkTailDropped(IPEndPoint endpoint) {
            socket.Send("ListenTailAdded", head, tail.Endpoint);
            peers.Remove(endpoint);
        }

        [NetRPC]
        private void ListenTailAdded(IPEndPoint endpoint, NetConnection connection) {
            if (!Heard(connection)) return;
            peers.Add(endpoint);
        }

        [NetRPC]
        private void ListenTailDropped(IPEndPoint endpoint, NetConnection connection) {
            if (!Heard(connection)) return;
            peers.Remove(endpoint);
        }

        private void TalkHeartbeat() {
            socket.Send("ListenHeartbeat", head);
        }

        [NetRPC]
        private void ListenHeartbeat(NetConnection connection) {
            if (!Heard(connection)) return;
        }
    }
}
