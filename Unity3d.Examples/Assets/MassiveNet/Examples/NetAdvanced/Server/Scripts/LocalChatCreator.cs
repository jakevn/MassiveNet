using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LocalChatCreator : MonoBehaviour {

        private const int MaxChatLength = 128;
        private const float MininumBetweenInput = 0.333f;
        private const float FloodProtectionReset = 4;
        private const int FloodInputTrip = 5;
        private const float FloodMuteDuration = 30f;

        private PlayerCreator player;
        private NetView view;
        private char[] formattedName;

        private float lastInput;
        private float lastFloodTrip = -30;
        private int floodInput;

        private bool FloodMuted {
            get { return Time.time - lastFloodTrip < FloodMuteDuration; }
        }

        private bool CanSend() {
            if (Time.time - lastInput < MininumBetweenInput) return false;
            if (Time.time - lastInput > FloodProtectionReset) floodInput = 0;
            lastInput = Time.time;
            floodInput++;
            if (floodInput <= FloodInputTrip) return !FloodMuted;
            lastFloodTrip = Time.time;
            view.SendReliable("ReceiveFloodMuted", RpcTarget.Controllers, FloodMuteDuration);
            return !FloodMuted;
        }

        private void Start() {
            player = GetComponent<PlayerCreator>();
            view = GetComponent<NetView>();
            formattedName = player.PlayerName.ToCharArray();
        }

        [NetRPC]
        private void ReceiveLocalInput(char[] input) {
            if (!CanSend() || !InputValid(input)) return;
            view.SendReliable("ReceiveLocalMessage", RpcTarget.All, formattedName, input);
        }

        [NetRPC]
        private void ReceiveSayInput(char[] input) {
            Debug.Log(input.Length);
            if (!CanSend() || !InputValid(input)) return;
            view.SendReliable("ReceiveSayMessage", RpcTarget.All, formattedName, input);
        }

        [NetRPC]
        private void ReceiveWhisperInput(string targetName, char[] input) {
            if (!CanSend() || !InputValid(input)) return;
            if (!InputValidator.LowercaseOnly(targetName)) return;
            if (player.PlayerName == targetName) return;
            if (!PlayerCreator.Players.ContainsKey(targetName)) {
                view.SendReliable("ReceiveWhisperFailed", RpcTarget.Controllers);
                return;
            }
            NetView playerView = PlayerCreator.Players[targetName].View;
            playerView.SendReliable("ReceiveWhisperMessage", RpcTarget.Controllers, player.PlayerName, input);
            view.SendReliable("ReceiveDeliveredWhisper", RpcTarget.Controllers, targetName, input);
        }

        private static bool InputValid(char[] input) {
            if (input.Length > MaxChatLength || input.Length == 0 || input[0] == ' ') return false;
            bool spacePrevious = false;
            for (int i = 0; i < input.Length; i++) {
                if (input[i] == ' ') {
                    if (spacePrevious) return false;
                    spacePrevious = true;
                    continue;
                }
                spacePrevious = false;
            }
            return true;
        }
    }
}