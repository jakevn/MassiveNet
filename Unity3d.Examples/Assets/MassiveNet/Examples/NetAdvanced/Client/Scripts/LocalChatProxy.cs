// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LocalChatProxy : MonoBehaviour {

        public Color32 SayColor;
        public Color32 LocalColor;
        public Color32 WhisperColor;

        private ChatClient chatClient;

        private void Start() {
            chatClient = FindObjectOfType<ChatClient>();
        }

        [NetRPC]
        private void ReceiveLocalMessage(char[] senderName, char[] input) {
            string a = new String(senderName, 0, senderName.Length) + ": " + new String(input, 0, input.Length);
            chatClient.AddToChatLog(a, LocalColor);
        }

        [NetRPC]
        private void ReceiveSayMessage(char[] senderName, char[] input) {
            string a = new String(senderName, 0, senderName.Length) + ": " + new String(input, 0, input.Length);
            chatClient.AddToChatLog(a, SayColor);
        }

    }

}