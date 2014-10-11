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
