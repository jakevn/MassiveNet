using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class CharacterCreator : MonoBehaviour {

        private NetSocket socket;
        private UiManager uiManager;

        private const string CreatorWindowName = "CreateScreen";
        private const string TitleWindowName = "TitleScreen";

        private const string CreateButtonName = "CreateButton";
        private const string PlayerNameInput = "PlayerNameInput";

        private NetConnection characterServer;

        private void Awake() {
            socket = GetComponent<NetSocket>();
            uiManager = GetComponent<UiManager>();
            socket.RegisterRpcListener(this);
        }

        void OnEnable() {
            Button.ListenForClick(CreateButtonName, SubmitCreate);
            TextFieldInput.ListenForSubmit(PlayerNameInput, SubmitCreate);
        }

        void OnDisable() {
            Button.StopListenForClick(CreateButtonName, SubmitCreate);
            TextFieldInput.StopListenForSubmit(SubmitCreate);
        }

        private void SubmitCreate() {
            string playerName;
            if (!TextFieldInput.TryGetText(PlayerNameInput, out playerName)) return;
            if (!InputValidator.IsValidPlayerName(playerName)) {
                Debug.LogError("Character name must be letters only and be " + InputValidator.MinPlayerNameLength + " to " + InputValidator.MaxPlayerNameLength + " characters long.");
                return;
            }
            
            playerName = InputValidator.FmtAllLowercase(playerName);
            socket.Send("CreateCharacter", characterServer, playerName);
            uiManager.CloseAllWindows();
        }

        [NetRPC]
        public void OpenCreator(NetConnection connection) {
            characterServer = connection;
            uiManager.OpenWindowsExclusively(CreatorWindowName, TitleWindowName);
        }

        [NetRPC]
        private void NameTakenResponse() {
            Debug.Log("Name taken.");
        }
    }
}