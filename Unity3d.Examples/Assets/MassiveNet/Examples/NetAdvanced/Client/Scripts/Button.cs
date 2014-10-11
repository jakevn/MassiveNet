// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class Button : MonoBehaviour {

        private static readonly List<GameObject> Buttons = new List<GameObject>();
        private static readonly Dictionary<string, List<Action>> Listeners = new Dictionary<string, List<Action>>();

        private static readonly Dictionary<string, List<Action>> MissedListeners = new Dictionary<string, List<Action>>();

        public static bool TrySetText(string buttonName, string newText) {
            foreach (var b in Buttons) {
                if (b.name != buttonName) continue;
                var mesh = b.GetComponentInChildren<TextMesh>();
                if (mesh == null) continue;
                mesh.text = newText;
                return true;
            }
            return false;
        }

        public static bool TryGetText(string buttonName, out string text) {
            foreach (var b in Buttons) {
                if (b.name != buttonName) continue;
                var mesh = b.GetComponentInChildren<TextMesh>();
                if (mesh == null) continue;
                text = mesh.text;
                return true;
            }
            text = null;
            return false;
        }

        public static bool TrySetActive(string buttonName, bool value) {
            foreach (var b in Buttons) {
                if (b.name != buttonName) continue;
                b.SetActive(value);
                return true;
            }
            return false;
        }

        public static void ListenForClick(string buttonName, Action listener) {
            if (Listeners.ContainsKey(buttonName)) Listeners[buttonName].Add(listener);
            else Listeners.Add(buttonName, new List<Action>{listener});
        }

        public static void StopListenForClick(string buttonName, Action listener) {
            if (Listeners.ContainsKey(buttonName) && Listeners[buttonName].Contains(listener)) Listeners[buttonName].Remove(listener);
        }

        public static void ListenForMissedClick(string buttonName, Action listener) {
            if (MissedListeners.ContainsKey(buttonName)) MissedListeners[buttonName].Add(listener);
            else MissedListeners.Add(buttonName, new List<Action> { listener });
        }

        public static void StopListenForMissedClick(string buttonName, Action listener) {
            if (MissedListeners.ContainsKey(buttonName) && MissedListeners[buttonName].Contains(listener)) MissedListeners[buttonName].Remove(listener);
        }

        private void OnEnable() {
            if (!Buttons.Contains(gameObject)) Buttons.Add(gameObject);
            InputHandler.Instance.ListenToKeyDown(MouseClick, KeyCode.Mouse0);
        }

        private void OnDisable() {
            //if (Buttons.Contains(gameObject)) Buttons.Remove(gameObject);
            InputHandler.Instance.StopListenToKeyDown(MouseClick, KeyCode.Mouse0);
        }

        private void MouseClick() {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit, 100.0f)) return;
            if (hit.collider.transform.parent == transform && Listeners.ContainsKey(gameObject.name)) {
                var listenerList = Listeners[gameObject.name];
                for (int i = listenerList.Count - 1; i >= 0; i--) {
                    listenerList[i]();
                }
            } else if (MissedListeners.ContainsKey(gameObject.name)) {
                var missedList = MissedListeners[gameObject.name];
                for (int i = missedList.Count - 1; i >= 0; i--) {
                    missedList[i]();
                }
            }
        }

    }

}
