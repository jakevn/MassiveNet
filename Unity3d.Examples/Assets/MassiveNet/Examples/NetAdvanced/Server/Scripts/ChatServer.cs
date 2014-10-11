// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

public class ChatServer : MonoBehaviour {

    private NetSocket socket;

    private void Start() {
        socket = FindObjectOfType<NetSocket>();
        socket.RegisterRpcListener(this);
    }

    public void BroadcastChat(string text) {

    }
}
