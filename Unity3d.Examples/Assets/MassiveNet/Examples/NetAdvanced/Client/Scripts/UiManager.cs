// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class UiManager : MonoBehaviour {

        public List<GameObject> UiWindows = new List<GameObject>();

        public void OpenWindowExclusively(string windowName) {
            foreach (GameObject screen in UiWindows) {
                screen.SetActive(screen.name == windowName);
            }
        }

        public void OpenWindowsExclusively(string windowName, string windowName2) {
            foreach (GameObject screen in UiWindows) {
                screen.SetActive(screen.name == windowName || screen.name == windowName2);
            }
        }

        public void OpenWindowsExclusively(string windowName, string windowName2, string windowName3) {
            foreach (GameObject screen in UiWindows) {
                screen.SetActive(screen.name == windowName
                                || screen.name == windowName2
                                || screen.name == windowName3);
            }
        }

        public void CloseAllWindows() {
            foreach (GameObject screen in UiWindows) {
                screen.SetActive(false);
            }
        }

        public void OpenWindow(string windowName) {
            foreach (GameObject screen in UiWindows) {
                if (screen.name != windowName) continue;
                screen.SetActive(true);
                return;
            }
        }

        public void CloseWindow(string windowName) {
            foreach (GameObject screen in UiWindows) {
                if (screen.name != windowName) continue;
                screen.SetActive(false);
                return;
            }
        }

    }

}
