// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Functionality and state for sending and receiving reliable, ordered messages.
    /// Handles acking, stream processing, send window, ordered receive buffer, etc.
    /// </summary>
    internal class NetChannelReliable {

        internal struct NetHeader {

            public ushort ObjSequence;
            public ushort AckSequence;
            public ulong AckHistory;
            public ushort AckTime;
            public uint SendTime;

            internal static NetHeader Create(NetChannelReliable chan, uint time) {
                var header = new NetHeader {
                    AckHistory = chan.AckHistory,
                    AckSequence = chan.NewestRemoteSequence,
                    ObjSequence = chan.LocalSequence,
                    SendTime = time
                };
                if (chan.LastReceiveTime > time) {
                    header.AckTime = (ushort)Mathf.Clamp(time - chan.LastReceiveTime, 0, 6000);
                }
                return header;
            }

            internal static NetHeader FromStream(NetStream stream) {
                return new NetHeader(NetMath.Trim(stream.ReadUShort()), NetMath.Trim(stream.ReadUShort()),
                    stream.ReadULong(), stream.ReadUShort());
            }

            internal void ToStream(NetStream stream) {
                stream.WriteUShort(NetMath.Pad(ObjSequence));
                stream.WriteUShort(NetMath.Pad(AckSequence));
                stream.WriteULong(AckHistory);
                stream.WriteUShort(AckTime);
            }

            internal NetHeader(ushort objSeq, ushort ackSeq, ulong ackHistory, ushort ackTime) {
                ObjSequence = objSeq;
                AckSequence = ackSeq;
                AckHistory = ackHistory;
                AckTime = ackTime;
                SendTime = 0;
            }
        }

        internal NetConnection Connection;

        // - Sent but unacked -
        private readonly List<ushort> sendWindow = new List<ushort>();
        private readonly List<uint> sendWindowTime = new List<uint>();
        private readonly Dictionary<ushort, NetStream> reliableWindow = new Dictionary<ushort, NetStream>();


        // - Received, but out-of-order -
        private readonly List<NetStream> recvBuffer = new List<NetStream>();
        private readonly List<int> recvBufferSeqDist = new List<int>();


        private ushort NewestRemoteSequence;
        private ushort LocalSequence;
        private ulong AckHistory;

        private uint LastReliableSent;
        private uint LastReceiveTime;

        private ushort LastAcceptedRemoteSequence;
        private uint ReceivedSinceLastSend;


        // - Stats -
        internal uint Sent = 0;
        internal uint Delivered = 0;
        internal uint Received = 0;
        internal uint Resends = 0;

        private NetStream sendStream;

        internal NetChannelReliable(NetConnection connection) {
            Connection = connection;
        }

        internal bool SendWindowFull {
            get { return sendWindow.Count > 450; }
        }

        internal bool ShouldForceAck(uint currentTime) {
            return ReceivedSinceLastSend > 16 || (ReceivedSinceLastSend > 0 && currentTime - LastReliableSent > 33);
        }

        internal void ForceAck() {
            if (sendStream != null) return;
            sendStream = NetStream.Create();
            WriteHeader();
            FlushStream();
        }

        internal bool FlushStream() {
            if (sendStream == null) return false;
            Connection.Socket.SendStream(Connection, sendStream);
            LastReliableSent = NetTime.Milliseconds();
            Connection.LastSendTime = LastReliableSent;
            sendStream = null;
            return true;
        }

        private void InitializeStream() {
            sendStream = NetStream.Create();
            AdvanceLocalSequence();
            var header = WriteHeader();

            reliableWindow.Add(header.ObjSequence, sendStream);
            sendWindow.Add(header.ObjSequence);
            sendWindowTime.Add(header.SendTime);
        }

        private bool retryingSerialization;

        /// <summary> 
        /// Serializes a NetMessage to the reliable stream.
        /// If there is no current stream, one is prepared.
        /// If the current stream cannot fit the message, it is sent and a new stream is prepared.
        /// </summary>
        internal void SerializeReliableMessage(NetMessage message) {
            if (sendWindow.Count >= 512) return;

            if (sendStream == null) InitializeStream();

            if (!NetSerializer.TryWriteMessage(sendStream, message)) {
                if (retryingSerialization) {
                    NetLog.Warning("SerializeReliableMessage failed.");
                    retryingSerialization = false;
                    return;
                }

                retryingSerialization = true;
                FlushStream();
                SerializeReliableMessage(message);
            }

            if (retryingSerialization) retryingSerialization = false;
        }

        private void AdvanceLocalSequence() {
            LocalSequence++;
            LocalSequence &= 32767;
        }

        /// <summary> Prepares the outgoing reliable stream: Writes the reliable bit & reliable header,
        /// sets stream parameters, and updates send stats. </summary>
        private NetHeader WriteHeader() {
            sendStream.Connection = Connection;
            sendStream.Socket = Connection.Socket;

            sendStream.WriteBool(true);
            var header = NetHeader.Create(this, NetTime.Milliseconds());
            header.ToStream(sendStream);

            ReceivedSinceLastSend = 0;
            Sent++;

            return header;
        }

        /// <summary> Handles a stream based on its header/size. Determines if it should be buffered if out-of-order,
        /// acked and released if size is equal to header size (ack only), or delivered immediately. </summary>
        internal void RouteIncomingStream(NetStream strm) {
            var header = NetHeader.FromStream(strm);
            int seqDist = NetMath.SeqDistance(header.ObjSequence, LastAcceptedRemoteSequence);

            // If the stream is only the size of a header, it's likely a forced ack:
            if (strm.Length <= 120) {
                AckDelivered(header);
                strm.Release();
            }
            else if (!RemoteSequenceValid(seqDist)) strm.Release();
            else if (seqDist != 1) BufferOutOfOrder(seqDist, strm, header);
            else {
                AckReceived(header);
                AckDelivered(header);
                DeliverStream(strm);
            }
        }

        /// <summary> Deserializes incoming reliable stream into NetMessages, forwards them to the NetSocket, releases the stream,
        /// increments the remote sequence, and retries the out-of-order buffer when needed. </summary>
        private void DeliverStream(NetStream strm) {
            // Deserialize stream into individual messages and pass them to the socket:
            while (NetSerializer.CanReadMessage(strm)) {
                var message = NetSerializer.ReadNetMessage(strm);
                if (message == null) {
                    NetLog.Error("Failed to parse reliable message from: " + Connection.Endpoint + " Pos: " + strm.Pos + " Size: " + strm.Size);
                    break;
                }
                Connection.Socket.ReceiveMessage(message, Connection);
            }
            // Return stream to pool and update receive buffer distances:
            strm.Release();
            LastAcceptedRemoteSequence++;
            if (recvBuffer.Count > 0) DecrementReceiveBuffer();
        }

        /// <summary> Returns true if the remote sequence distance is valid. </summary>
        private bool RemoteSequenceValid(int seqDist) {
            return seqDist > 0 && seqDist <= 512;
        }

        /// <summary> Adds an out-of-order datagram to the buffer to await future delivery. </summary>
        private void BufferOutOfOrder(int seqDist, NetStream strm, NetHeader header) {
            if (recvBuffer.Count >= 512) {
                Connection.Disconnect();
                return;
            }
            if (recvBufferSeqDist.Contains(seqDist)) {
                strm.Release();
                return;
            }
            // Ack history window is only 64 bits, so only ack if within window:
            if (seqDist < 64) {
                AckReceived(header);
                AckDelivered(header);
            }
            else strm.Pos = 1; // Reset to 1 so header can be reprocessed when seqDist < 64

            recvBuffer.Add(strm);
            recvBufferSeqDist.Add(seqDist);
        }

        /// <summary>
        /// When an in-order datagram arrives and there are out-of-order datagrams
        /// in the buffer, this method updates their sequence distance. If a datagram is now
        /// ready to be delivered (seqDist = 1), or ready to be acknowledged (seqDist = 63),
        /// this method will handle it.
        /// </summary>
        private void DecrementReceiveBuffer() {
            for (int i = recvBuffer.Count - 1; i >= 0; i--) recvBufferSeqDist[i]--;
            if (recvBufferSeqDist.Contains(63)) RetryOutOfOrder(recvBufferSeqDist.IndexOf(63));
            if (recvBufferSeqDist.Contains(1)) RetryOutOfOrder(recvBufferSeqDist.IndexOf(1));
        }

        /// <summary> Removes a datagram from the buffer and retries delivery. </summary>
        private void RetryOutOfOrder(int index) {
            NetStream strm = recvBuffer[index];
            int dist = recvBufferSeqDist[index];
            recvBuffer.RemoveAt(index);
            recvBufferSeqDist.RemoveAt(index);
            if (dist == 63) RouteIncomingStream(strm);
            else if (dist == 1) DeliverStream(strm);
        }

        /// <summary> Acknowledges and updates the remote sequence. </summary>
        private void AckReceived(NetHeader header) {
            int newestDist = NetMath.SeqDistance(header.ObjSequence, NewestRemoteSequence);
            // If the sequence is newest, shift the buffer and apply ack bit:
            if (newestDist > 0) {
                AckHistory = (AckHistory << newestDist) | 1UL;
                NewestRemoteSequence = header.ObjSequence;
            }
            // Else, shift the ack bit and apply to buffer:
            else AckHistory |= 1UL << -newestDist;

            LastReceiveTime = NetTime.Milliseconds();

            ReceivedSinceLastSend++;
            Received++;
        }

        /// <summary> Detects acknowledged and lost messages. </summary>
        private void AckDelivered(NetHeader header) {
            for (int i = 0; i < sendWindow.Count; i++) {
                int seqDist = NetMath.SeqDistance(sendWindow[i], header.AckSequence);
                // This AckSequence is older than the sendWindow's, not useful:
                if (seqDist > 0) break;
                // AckHistory has rolled over without acking this message; Ordered reliable is broken:
                if (seqDist <= -64) Connection.Disconnect();
                // If the seqDistance corresponds to a true bit in the AckHistory, message delivered/acked:
                else if (IsAcked(header.AckHistory, seqDist)) {
                    MessageDelivered(i, header);
                    i--; // Since the sendWindow count will decrease, the index needs to be adjusted.
                }
                // The seqDist is still within the send window, but if too much time has passed, assume lost:
                else if (NetTime.Milliseconds() - sendWindowTime[i] > 333) MessageLost(i);
            }
        }

        private uint lastCheckTime;

        /// <summary> Checks all sent and unacked messages for timeout. Messages exceeding timeout are considered lost. </summary>
        internal void CheckTimeouts(uint currentTime) {

            if (currentTime - lastCheckTime < 333) return;

            lastCheckTime = currentTime;

            for (int i = 0; i < sendWindow.Count; i++) {
                if (NetTime.Milliseconds() - sendWindowTime[i] > 333) MessageLost(i);
            }
        }

        /// <summary> Uses the seqDistance as a flag on ackHistory for delivery. True if present. </summary>
        private static bool IsAcked(ulong ackHistory, int seqDist) {
            return (ackHistory & (1UL << -seqDist)) != 0UL;
        }

        /// <summary> Resends a lost message, updates its sent time, and increments the Resends stat. </summary>
        private void MessageLost(int index) {
            Resends++;
            NetStream strm = reliableWindow[sendWindow[index]];
            sendWindowTime[index] = NetTime.Milliseconds();
            Connection.Socket.SendStream(Connection, strm);
        }

        /// <summary> Removes acked messages from the send window, releases the stream, updates connection ping, and 
        /// increments the Delivered stat. </summary>
        private void MessageDelivered(int index, NetHeader header) {
            Delivered++;
            NetStream strm = reliableWindow[sendWindow[index]];
            if (header.AckTime > 0) Connection.UpdatePing(NetTime.Milliseconds(), sendWindowTime[index], header.AckTime);
            reliableWindow.Remove(sendWindow[index]);
            sendWindow.RemoveAt(index);
            sendWindowTime.RemoveAt(index);
            strm.Release();
        }
    }
}