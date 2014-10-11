// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
namespace MassiveNet {

    /// <summary> Handles (de)serialization of sent and received unreliable messages. </summary>
    internal class NetChannelUnreliable {

        internal NetConnection Connection;

        private readonly NetStream unreliableStream = NetStream.Create();

        internal NetChannelUnreliable(NetConnection connection) {
            Connection = connection;
            WriteUnreliableHeader();
        }

        private bool FlushStream(bool forceSend) {
            if (unreliableStream.Position == 1 && !forceSend) return false;
            Connection.Socket.SendStream(Connection, unreliableStream);
            Connection.LastSendTime = NetTime.Milliseconds();
            unreliableStream.Reset();
            WriteUnreliableHeader();
            return true;
        }

        internal void DeserializeStream(NetStream stream) {
            while (NetSerializer.CanReadMessage(stream)) {
                var message = NetSerializer.ReadNetMessage(stream);
                if (message == null) {
                    NetLog.Warning("Failed to parse unreliable message from: " + Connection.Endpoint);
                    break;
                }
                Connection.Socket.ReceiveMessage(message, Connection);
            }
            stream.Release();
        }

        internal bool FlushStream() {
            return FlushStream(false);
        }

        internal void SendHeartbeat() {
            FlushStream(true);
        }

        /// <summary> Writes a zero-bit to signify the lack of a reliable header. </summary>
        private void WriteUnreliableHeader() {
            unreliableStream.WriteBool(false);
        }


        private bool retry;
        internal void SerializeMessage(NetMessage message) {
            if (!NetSerializer.TryWriteMessage(unreliableStream, message)) {
                if (retry) {
                    NetLog.Warning("SerializeUnreliableMessage failed.");
                    retry = false;
                    return;
                }
                // Stream likely full, flush stream and retry serialization:
                retry = true;
                FlushStream();
                SerializeMessage(message);
            }
            if (retry) retry = false;
        }
    }
}