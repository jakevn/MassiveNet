// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LocalChatOwner : MonoBehaviour {

        private NetView view;
        private ChatClient chatClient;
        private TextMesh chatInput;

        public Color32 SayColor;
        public Color32 LocalColor;
        public Color32 WhisperColor;
        public Color32 SentWhisperColor;
        public Color32 FailureColor;

        private enum Mode {
            Say,
            Whisper,
            Closed,
            Command,
            Local,
        }

        private const Mode DefaultMode = Mode.Say;

        private Mode mode = Mode.Closed;
        private Mode lastMode = DefaultMode;

        private bool ChatOpen {
            get { return mode != Mode.Closed; }
        }

        private string whisperTarget;
        private string receivedLastWhisperFrom = null;

        private const char CommStart = '/';
        private const char CommDelimiter = ' ';

        private const char NewLine = '\n';

        private const string CommPredicateWhisper = "whisper";
        private const string CommPredicateSay = "say";
        private const string CommPredicateLocal = "local";

        private readonly char[] buffer = new char[128];
        private short bufferPos;
        private const short LineLength = 48;

        private void Start() {
            view = GetComponent<NetView>();

            TextMesh[] textMeshes = GetComponentsInChildren<TextMesh>();
            foreach (var tm in textMeshes) {
                if (tm.name != "ChatInput") continue;
                chatInput = tm;
                break;
            }
            chatClient = GetComponentInChildren<ChatClient>();

            InputHandler.Instance.ListenToKeyDown(CloseChat, KeyCode.Escape);
            InputHandler.Instance.ListenToKeyDown(OpenOrSend, KeyCode.Return);
            InputHandler.Instance.ListenToKeyDown(Backspace, KeyCode.Backspace);
            InputHandler.Instance.ListenToKey(BackspaceHold, KeyCode.Backspace);
            InputHandler.Instance.ListenToKeyDown(OpenReply, KeyCode.R);
        }

        private void CloseChat() {
            if (ChatOpen) ChangeMode(Mode.Closed);
            InputHandler.Instance.StopListenToChars(AddChar);;
        }

        private void OpenOrSend() {
            if (ChatOpen) SendInput();
            else OpenChatInput();
        }

        private float lastBackspace;
        private void BackspaceHold() {
            if (!ChatOpen || Time.time - lastBackspace < 0.15f) return;
            lastBackspace = Time.time;
            RemoveChar();
        }

        private void Backspace() {
            if (!ChatOpen) return;
            lastBackspace = Time.time;
            RemoveChar();
        }

        private void OpenReply() {
            if (!ChatOpen) ReplyToWhisper();
        }

        private void ReplyToWhisper() {
            if (receivedLastWhisperFrom == null) return;
            whisperTarget = receivedLastWhisperFrom.ToLower();
            ChangeMode(Mode.Whisper);
        }

        private void SendInput() {
            if (bufferPos > 0 && CanTalk()) {
                var newArr = new char[bufferPos];
                Array.Copy(buffer, 0, newArr, 0, bufferPos);
                switch (mode) {
                    case Mode.Whisper:
                        view.SendReliable("ReceiveWhisperInput", RpcTarget.Server, whisperTarget, newArr);
                        break;
                    case Mode.Say:
                        view.SendReliable("ReceiveSayInput", RpcTarget.Server, newArr);
                        break;
                    case Mode.Local:
                        view.SendReliable("ReceiveLocalInput", RpcTarget.Server, newArr);
                        break;
                }
            }
            ChangeMode(Mode.Closed);
        }

        private void ChangeMode(Mode newMode) {
            lastMode = mode;
            mode = newMode;
            ResetBuffer();
            InputHandler.Instance.GetExclusiveLock(this);
            switch (mode) {
                case Mode.Closed:
                    if (lastMode == Mode.Command) lastMode = DefaultMode;
                    inputPrefix = "";
                    chatInput.text = "";
                    InputHandler.Instance.CancelExclusiveLock();
                    break;
                case Mode.Whisper:
                    chatInput.color = WhisperColor;
                    inputPrefix = CommPredicateWhisper + " " + whisperTarget + ": ";
                    break;
                case Mode.Say:
                    chatInput.color = SayColor;
                    inputPrefix = "say: ";
                    break;
                case Mode.Local:
                    chatInput.color = LocalColor;
                    inputPrefix = "local: ";
                    break;
                case Mode.Command:
                    chatInput.color = SayColor;
                    inputPrefix = "";

                    if (Input.GetKeyDown(KeyCode.Backspace)) {
                        string toBuff = "";
                        if (lastMode == Mode.Say) toBuff = "/say ";
                        if (lastMode == Mode.Whisper) toBuff = "/whisper " + whisperTarget;
                        if (lastMode == Mode.Local) toBuff = "/local ";
                        toBuff.CopyTo(0, buffer, 0, toBuff.Length);
                        bufferPos = (short)toBuff.Length;
                    }

                    break;
            }
            UpdateChatInput();
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

        [NetRPC]
        private void ReceiveWhisperMessage(string senderName, char[] input) {
            receivedLastWhisperFrom = senderName;
            string a = receivedLastWhisperFrom + ": " + new String(input, 0, input.Length);
            chatClient.AddToChatLog(a, WhisperColor);
        }

        [NetRPC]
        private void ReceiveDeliveredWhisper(string senderName, char[] input) {
            string a = "to " + senderName + ": " + new String(input, 0, input.Length);
            chatClient.AddToChatLog(a, SentWhisperColor);
        }

        [NetRPC]
        private void ReceiveWhisperFailed() {
            chatClient.AddToChatLog("Could not send whisper. Cat not online.", FailureColor);
        }

        private bool floodMute = false;
        private float unmutedAt;

        [NetRPC]
        private void ReceiveFloodMuted(float duration) {
            floodMute = true;
            unmutedAt = Time.time + duration;
            SayFloodMuteMessage();
        }

        private void SayFloodMuteMessage() {
            chatClient.AddToChatLog(
                "You've been muted for spamming. Unmuted in " + (int)(unmutedAt - Time.time) + " seconds.",
                FailureColor);
        }

        private bool CanTalk() {
            if (!floodMute) return true;

            if (unmutedAt > Time.time) {
                SayFloodMuteMessage();
                return false;
            } else {
                floodMute = false;
            }
            return true;
        }

        private void OpenChatInput() {
            ChangeMode(lastMode);
            InputHandler.Instance.ListenToChars(AddChar);
        }

        private string inputPrefix = "";

        private void UpdateChatInput() {
            int startIndex = Mathf.Clamp(bufferPos - LineLength, 0, bufferPos - 1);
            int endIndex = startIndex < 1 ? (int)bufferPos : LineLength;
            chatInput.text = inputPrefix + new string(buffer, startIndex, endIndex) + (mode == Mode.Closed ? "" : "|");
        }

        private void ResetBuffer() {
            Array.Clear(buffer, 0, buffer.Length);
            bufferPos = 0;
        }

        private void ParseCommandMode() {
            if (buffer[0] != CommStart) return;
            int firstDelimIndex = 0;
            for (int i = 0; i < bufferPos; i++) {
                if (buffer[i] != CommDelimiter) continue;
                if (firstDelimIndex == 0) {
                    firstDelimIndex = i;
                    if (MatchEmptyCommand(new String(buffer, 1, firstDelimIndex - 1))) return;
                } else {
                    string command = new String(buffer, 1, firstDelimIndex - 1);
                    string input = new string(buffer, firstDelimIndex + 1, bufferPos - (firstDelimIndex + 2));
                    MatchCommand(command, input);
                    break;
                }
            }
        }

        private bool MatchCommand(string command, string arg) {
            string lowerComm = command.ToLower();
            string lowerArg = arg.ToLower();
            if (lowerComm == CommPredicateWhisper) {
                whisperTarget = lowerArg;
                ChangeMode(Mode.Whisper);
                return true;
            }
            return false;
        }

        private bool MatchEmptyCommand(string command) {
            string lowerComm = command.ToLower();
            switch (lowerComm) {
                case CommPredicateSay:
                    ChangeMode(Mode.Say);
                    return true;
                case CommPredicateLocal:
                    ChangeMode(Mode.Local);
                    return true;
            }
            return false;
        }

        private void RemoveChar() {
            if (bufferPos == 0) {
                if (mode != Mode.Command) ChangeMode(Mode.Command);
                return;
            }
            bufferPos--;
            buffer[bufferPos] = (char)0;
            UpdateChatInput();
        }

        private void AddChar(char c) {
            if (!ChatOpen) return;
            if (bufferPos == 0) {
                if (c == CommStart) ChangeMode(Mode.Command);
                if (c == ' ') return;
            }
            if (bufferPos == buffer.Length - 1) return;
            if (c == NewLine) return;
            buffer[bufferPos] = c;
            bufferPos++;
            if (mode == Mode.Command) ParseCommandMode();
            UpdateChatInput();
        }

    }

}
