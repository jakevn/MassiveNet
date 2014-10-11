using System;
using System.Collections.Generic;
using MassiveNet;
using UnityEngine;
using Random = System.Random;

public class StreamDatabaseServer : MonoBehaviour {

    private NetSocket socket;

    private readonly List<ulong> ulongKeys = new List<ulong>();
    private readonly List<string> stringKeys = new List<string>();
    private readonly List<NetStream> streams = new List<NetStream>();
    //private readonly List<ulong> revision = new List<ulong>();
    //private readonly List<bool> locked = new List<bool>();

    private const string EmptyString = "";
    private const uint EmptyUlong = 0;

    private readonly byte[] randBytes = new byte[8];
    private readonly Random random = new Random();

    // TODO: Released streams break everything. Copy streams.

    void Awake() {
        socket = GetComponent<NetSocket>();
        socket.RegisterRpcListener(this);
    }

    private ulong RandomUlong() {
        for (int i = 0; i < 64; i++) {
            random.NextBytes(randBytes);
            ulong result = BitConverter.ToUInt64(randBytes, 0);
            if (ulongKeys.Contains(result)) continue;
            return result;
        }
        throw new UnityException("Failed to generate unused ulong within 64 tries.");
    }

    public ulong Add(NetStream streamValue) {
        NetStream stream = streamValue.Copy();
        ulong key = RandomUlong();
        ulongKeys.Add(key);
        stringKeys.Add(EmptyString);
        streams.Add(stream);
        return key;
    }

    public bool TryAdd(string stringKey, NetStream streamValue, out ulong key) {
        NetStream stream = streamValue.Copy();
        if (string.IsNullOrEmpty(stringKey)) {
            Debug.LogError("TryAdd failed! String key is null or empty.");
            key = ulong.MinValue;
            return false;
        }
        if (stringKeys.Contains(stringKey)) {
            Debug.LogError("TryAdd failed! Database already contains provided string key: " + stringKey);
            key = ulong.MinValue;
            return false;
        }
        key = RandomUlong();
        ulongKeys.Add(key);
        stringKeys.Add(stringKey);
        streams.Add(stream);
        return true;
    }

    [NetRPC]
    private void TryAddRequest(string stringKey, NetStream stream, NetRequest<ulong> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        ulong result;
        if (TryAdd(stringKey, stream, out result)) request.Result = result;
    }

    [NetRPC]
    private void AddRequest(NetStream stream, NetRequest<ulong> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        request.Result = Add(stream);
    }

    public bool TryGet(ulong ulongKey, out NetStream stream) {
        int index;
        if (!TryGetIndex(ulongKey, out index)) {
            stream = null;
            return false;
        }
        var foundStream = streams[index].Copy();
        foundStream.Position = 0;
        stream = foundStream;
        return true;
    }

    public bool TryGet(string stringKey, out NetStream stream) {
        int index;
        if (!TryGetIndex(stringKey, out index)) {
            stream = null;
            return false;
        }
        var foundStream = streams[index].Copy();
        foundStream.Position = 0;
        stream = foundStream;
        return true;
    }

    [NetRPC]
    private void GetRequest(ulong ulongKey, NetRequest<NetStream> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        NetStream stream;
        if (TryGet(ulongKey, out stream)) {
            stream.Position = stream.Size;
            request.Result = stream;
        }
    }

    [NetRPC]
    private void GetStringRequest(string stringKey, NetRequest<NetStream> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        NetStream stream;
        if (TryGet(stringKey, out stream)) {
            stream.Position = stream.Size;
            request.Result = stream;
        }
    }

    public bool TryGetKey(ulong ulongKey, out string stringKey) {
        int index;
        if (!TryGetIndex(ulongKey, out index)) {
            stringKey = null;
            return false;
        }
        stringKey = stringKeys[index];
        return true;
    }

    public bool TryGetKey(string stringKey, out ulong ulongKey) {
        int index;
        if (!TryGetIndex(stringKey, out index)) {
            ulongKey = default(uint);
            return false;
        }
        ulongKey = ulongKeys[index];
        return true;
    }

    [NetRPC]
    private void GetStringKeyRequest(ulong ulongKey, NetRequest<string> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        string result;
        if (TryGetKey(ulongKey, out result)) request.Result = result;
    }

    [NetRPC]
    private void GetKeyRequest(string stringKey, NetRequest<ulong> request, NetConnection connection) {
        if (!connection.IsPeer && !connection.IsServer) return;
        ulong result;
        if (TryGetKey(stringKey, out result)) request.Result = result;
    }

    private bool TryGetIndex(ulong ulongKey, out int index) {
        if (ulongKey == EmptyUlong || !ulongKeys.Contains(ulongKey)) {
            index = -1;
            return false;
        }
        index = ulongKeys.IndexOf(ulongKey);
        return true;
    }


    public bool HasKey(string stringKey) {
        int index;
        return TryGetIndex(stringKey, out index);
    }

    public bool HasKey(ulong ulongKey) {
        int index;
        return TryGetIndex(ulongKey, out index);
    }

    private bool TryGetIndex(string stringKey, out int index) {
        if (string.IsNullOrEmpty(stringKey) || !stringKeys.Contains(stringKey)) {
            index = -1;
            return false;
        }
        index = stringKeys.IndexOf(stringKey);
        return true;
    }

    public bool TryUpdate(string stringKey, NetStream stream) {
        int index;
        if (stream == null || !TryGetIndex(stringKey, out index)) return false;
        NetStream oldStream = streams[index];
        streams[index] = stream.Copy();
        if (stream != oldStream) oldStream.Release();
        return true;
    }

    public bool TryUpdate(ulong ulongKey, NetStream stream) {
        int index;
        if (stream == null || !TryGetIndex(ulongKey, out index)) return false;
        NetStream oldStream = streams[index];
        streams[index] = stream.Copy();
        if (stream != oldStream) oldStream.Release();
        return true;
    }

    public bool TryDelete(ulong ulongKey) {
        int index;
        if (!TryGetIndex(ulongKey, out index)) return false;
        NetStream stream = streams[index];
        stream.Release();
        streams.RemoveAt(index);
        stringKeys.RemoveAt(index);
        ulongKeys.RemoveAt(index);
        return true;
    }

    public bool TryDelete(string stringKey) {
        int index;
        if (!TryGetIndex(stringKey, out index)) return false;
        NetStream stream = streams[index];
        stream.Release();
        streams.RemoveAt(index);
        stringKeys.RemoveAt(index);
        ulongKeys.RemoveAt(index);
        return true;
    }
}
