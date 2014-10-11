// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Linq;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class CharacterSelect : MonoBehaviour {

        private NetSocket socket;
        private UiManager uiManager;

        private const string CharSelectWindowName = "SelectScreen";
        private const string TitleWindowName = "TitleScreen";

        private const string PlayButtonName = "PlayButton";
        private const string NewCharButtonName = "NewButton";

        private NetConnection characterServer;

        private void Awake() {
            socket = GetComponent<NetSocket>();
            uiManager = GetComponent<UiManager>();
            socket.RegisterRpcListener(this);
        }

        private void OnEnable() {
            Button.ListenForClick(PlayButtonName, SubmitPlay);
            Button.ListenForClick(NewCharButtonName, Create);
        }

        private void OnDisable() {
            Button.StopListenForClick(PlayButtonName, SubmitPlay);
            Button.StopListenForClick(NewCharButtonName, Create);
        }

        private void Create() {
            GetComponent<CharacterCreator>().OpenCreator(characterServer);
        }

        private void SubmitPlay() {
            string playerName;
            SelectableButton selected;
            if (!SelectableButton.TryGetSelected(CharSelectWindowName, out selected)) return;
            if (!Button.TryGetText(selected.gameObject.name, out playerName)) return;
            socket.Send("SpawnCharacter", characterServer, InputValidator.FmtAllLowercase(playerName));
            uiManager.CloseAllWindows();
        }

        [NetRPC]
        private void OpenCharacterList(string[] names, bool dummy, NetConnection connection) {
            characterServer = connection;
            uiManager.OpenWindowsExclusively(TitleWindowName, CharSelectWindowName);
            SetCharacterButtons(names);
        }

        private void SetCharacterButtons(string[] names) {
            for (int i = 1; i < 5; i++) {
                if (i > names.Length) {
                    Button.TrySetActive("Character" + i, false);
                } else {
                    Button.TrySetActive("Character" + i, true);
                    Button.TrySetText("Character" + i, InputValidator.FmtUppercaseFirstChar(names[i - 1]));
                }
            }
            if (names.Length >= 4) Button.TrySetActive(NewCharButtonName, false);
        }

    }
}
