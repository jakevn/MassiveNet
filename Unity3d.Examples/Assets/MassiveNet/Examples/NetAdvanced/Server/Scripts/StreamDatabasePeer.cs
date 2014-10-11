// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections;
using System.Collections.Generic;
using MassiveNet;
using UnityEngine;
using Random = System.Random;

public class StreamDatabasePeer : MonoBehaviour {

    public int SuccessSets = 0;
    public int FailSets = 0;

    public int SuccessGets = 0;
    public int FailGets = 0;

    public bool ActAsServer = false;

    private NetSocket socket;

    private NetConnection server;

    private readonly List<uint> benchKeys = new List<uint>();
    private readonly List<NetStream> benchStream = new List<NetStream>();

    void Awake() {
        socket = GetComponent<NetSocket>();
        socket.RegisterRpcListener(this);
        socket.Events.OnPeerConnected += Connected;
        if (!ActAsServer) MakeBenchData();
    }

    void Connected(NetConnection connection) {
        if (ActAsServer) return;
        server = connection;
        StartCoroutine(Benchmark());
    }

    private void MakeBenchData() {
        var random = new Random();
        for (int i = 1; i < 512; i++) {
            benchKeys.Add((uint)i);
            var stream = NetStream.New();
            for (int f = 0; f < 128; f++) stream.WriteInt(random.Next());
            benchStream.Add(stream);
        }
    }

    IEnumerator Benchmark() {
        yield return new WaitForSeconds(2f);
        while (SendSetRequest()) yield return new WaitForEndOfFrame();
        while (SendGetRequest()) yield return new WaitForEndOfFrame();
        Debug.Log("Benchmark complete!");
    }

    private int currentSet = 0;
    private bool SendSetRequest() {
        StartCoroutine("YieldSetRequest", currentSet);
        currentSet++;
        return currentSet < benchKeys.Count;
    }

    IEnumerator YieldSetRequest(int index) {
        var request = socket.SendRequest<bool>("TryAddUintKeyRequest", server, benchKeys[index], benchStream[index]);
        yield return request.WaitUntilDone;
        if (request.IsSuccessful) SuccessSets++;
        else FailSets++;
    }

    private int currentGet = 0;
    private bool SendGetRequest() {
        StartCoroutine("YieldGetRequest", currentGet);
        currentGet++;
        return currentGet < benchKeys.Count;
    }

    IEnumerator YieldGetRequest(int index) {
        var request = socket.SendRequest<NetStream>("TryGetUintKeyRequest", server, benchKeys[index]);
        yield return request.WaitUntilDone;
        if (request.IsSuccessful) {
            SuccessGets++;
            request.Result.Release();
        }
        else FailGets++;
    }


}
