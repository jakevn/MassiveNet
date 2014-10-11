// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class ChatClient : MonoBehaviour {

        private readonly List<ChatLine> _chatLines = new List<ChatLine>();
        private readonly List<string> _chatLog = new List<string>();

        void Awake() {
            _chatLines.AddRange(GetComponentsInChildren<ChatLine>());
        }

        public void AddToChatLog(string line, Color32 color) {
            _chatLog.Add(line);
            if (_chatLog.Count > 64) _chatLog.RemoveAt(0);
            bool setLine = false;
            foreach (ChatLine cl in _chatLines) {
                cl.Bump();
                if (cl.enabled || setLine) continue;
                cl.SetLine(line, color);
                setLine = true;
            }
        }

    }

}