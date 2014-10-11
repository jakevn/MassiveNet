// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class InputHandler : MonoBehaviour {

        public static InputHandler Instance {
            get { return instance ?? CreateInstance(); }
        }

        private static InputHandler instance;
        private static readonly object LockObj = new object();

        private static InputHandler CreateInstance() {
            lock (LockObj) {
                return instance ?? (instance = new GameObject("_InputHandler").AddComponent<InputHandler>());
            }
        }

        private readonly List<Action<char>> charListeners = new List<Action<char>>();

        private readonly List<KeyCode> keyDownKeys = new List<KeyCode>();
        private readonly List<List<Action>> keyDownListen = new List<List<Action>>();

        private readonly List<KeyCode> keyKeys = new List<KeyCode>();
        private readonly List<List<Action>> keyListen = new List<List<Action>>(); 

        private object exclusiveListener;

        private void Awake() {
            useGUILayout = false;
            lock (LockObj) {
                if (instance == null) instance = this;
            }
        }

        /// <summary> Finds every listener for the first KeyCode argument and switches
        /// it to listen to the second KeyCode argument. Useful for rebinding keys during runtime. </summary>
        public void SwapKeyCodes(KeyCode from, KeyCode to) {
            if (keyDownKeys.Contains(from)) {
                int index = keyDownKeys.IndexOf(from);
                var listeners = keyDownListen[index];
                if (!keyDownKeys.Contains(to)) {
                    keyDownKeys.Add(to);
                    keyDownListen.Add(new List<Action>());
                }
                keyDownListen[keyDownKeys.IndexOf(to)].AddRange(listeners);
                listeners.Clear();
            }

            if (keyKeys.Contains(from)) {
                int index = keyKeys.IndexOf(from);
                var listeners = keyListen[index];
                if (!keyKeys.Contains(to)) {
                    keyKeys.Add(to);
                    keyListen.Add(new List<Action>());
                }
                keyListen[keyKeys.IndexOf(to)].AddRange(listeners);
                listeners.Clear();
            }
        }
        public void ListenToKeyDown(Action keyDownListener, KeyCode key) {
            if (keyDownKeys.Contains(key)) {
                int index = keyDownKeys.IndexOf(key);
                if (!keyDownListen[index].Contains(keyDownListener)) keyDownListen[index].Add(keyDownListener);
            } else {
                keyDownKeys.Add(key);
                keyDownListen.Add(new List<Action> { keyDownListener });
            }
        }

        public void StopListenToKeyDown(Action keyDownListener, KeyCode key) {
            if (!keyDownKeys.Contains(key) || !keyDownListen[keyDownKeys.IndexOf(key)].Contains(keyDownListener)) return;
            keyDownListen[keyDownKeys.IndexOf(key)].Remove(keyDownListener);
        }

        public void ListenToKey(Action keyListener, KeyCode key) {
            if (keyKeys.Contains(key)) {
                int index = keyKeys.IndexOf(key);
                if (!keyListen[index].Contains(keyListener)) keyListen[index].Add(keyListener);
            } else {
                keyKeys.Add(key);
                keyListen.Add(new List<Action> { keyListener });
            }
        }

        public void StopListenToKey(Action keyListener, KeyCode key) {
            if (!keyKeys.Contains(key) || !keyListen[keyKeys.IndexOf(key)].Contains(keyListener)) return;
            keyListen[keyKeys.IndexOf(key)].Remove(keyListener);
        }

        public void GetExclusiveLock(object exclusiveListenerInstance) {
            exclusiveListener = exclusiveListenerInstance;
        }

        public void CancelExclusiveLock() {
            exclusiveListener = null;
        }


        public void ListenToChars(Action<char> charListener) {
            if (!charListeners.Contains(charListener)) charListeners.Add(charListener);
        }

        public void StopListenToChars(Action<char> removeCharListener) {
            if (charListeners.Contains(removeCharListener)) charListeners.Remove(removeCharListener);
        }

        private void Update() {
            if (Input.anyKeyDown) {
                for (int i = keyDownKeys.Count - 1; i >= 0; i--) {
                    if (!Input.GetKeyDown(keyDownKeys[i])) continue;
                    InformListeners(keyDownListen[i]);
                }
            }

            if (!Input.anyKey) return;
            for (int i = keyKeys.Count - 1; i >= 0; i--) {
                if (!Input.GetKey(keyKeys[i])) continue;
                InformListeners(keyListen[i]);
            }
        }

        private void InformListeners(List<Action> listeners) {
            for (int i = listeners.Count - 1; i >= 0 && i < listeners.Count; i--) {
                if (exclusiveListener != null && listeners[i].Target != exclusiveListener) continue;
                listeners[i]();
            }
        }

        private void OnGUI() {
            if (Event.current.type != EventType.KeyDown) return;
            if (Event.current.character != 0 && Event.current.keyCode == KeyCode.None) CapturedChar(Event.current.character);
        }

        private void CapturedChar(char c) {
            if (charListeners.Count == 0) return;
            for (int i = charListeners.Count - 1; i >= 0 && i < charListeners.Count; i--) {
                if (exclusiveListener != null && charListeners[i].Target != exclusiveListener) continue;
                charListeners[i](c);
            }
        }

    }

}