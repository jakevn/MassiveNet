using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class TextFieldInput : MonoBehaviour {

        private static readonly List<TextFieldInput> Fields = new List<TextFieldInput>();
        private static readonly Dictionary<string, Action> Listeners = new Dictionary<string, Action>();

        private static TextFieldInput currentlySelected;

        public static bool ListenForSubmit(string fieldName, Action listener) {
            foreach (var field in Fields) {
                if (field.name != fieldName) continue;
                if (Listeners.ContainsKey(fieldName)) return false;
                Listeners.Add(fieldName, listener);
                return true;
            }
            return false;
        }

        public static void StopListenForSubmit(Action listener) {
            string key = (from kv in Listeners where kv.Value == listener select kv.Key).FirstOrDefault();
            if (key != null) Listeners.Remove(key);
        }

        public static bool TryGetText(string fieldName, out string text) {
            foreach (var field in Fields) {
                if (field.gameObject.name != fieldName) continue;
                text = field.Text();
                return true;
            }
            text = null;
            return false;
        }

        public bool PasswordField;
        public int MaxLength = 64;
        public int LineLength = 20;
        public TextFieldInput TabTarget;

        private TextMesh textMesh;
        private string initialValue;

        private string backingText = "";

        private void OnEnable() {
            if (textMesh == null) {
                textMesh = GetComponentInChildren<TextMesh>();
                if (textMesh == null) {
                    Debug.LogError("No TextMesh found in children.");
                    return;
                }
                initialValue = textMesh.text;
            }
            if (!Fields.Contains(this)) Fields.Add(this);

            Button.ListenForClick(gameObject.name, Clicked);
        }

        private void OnDisable() {
            if (Fields.Contains(this)) Fields.Remove(this);
            if (initialValue != null) {
                textMesh.text = initialValue;
                backingText = "";
            }
            Button.StopListenForClick(gameObject.name, Clicked);
            Deselected();
        }

        private void Clicked() {
            if (currentlySelected == this) return;
            Selected();
        }

        private void MissedClick() {
            Deselected();
        }

        public void Selected() {
            currentlySelected = this;
            if (textMesh.text == initialValue) textMesh.text = "";
            textMesh.text += '|';
            InputHandler.Instance.ListenToKeyDown(Tab, KeyCode.Tab);
            InputHandler.Instance.ListenToKeyDown(Return, KeyCode.Return);
            Button.ListenForMissedClick(gameObject.name, MissedClick);
            InputHandler.Instance.ListenToChars(OnInput);
            InputHandler.Instance.ListenToKey(OnBackspace, KeyCode.Backspace);
        }

        private void Deselected() {
            if (currentlySelected == this) currentlySelected = null;
            if (textMesh.text[textMesh.text.Length - 1] == '|') textMesh.text = textMesh.text.Remove(textMesh.text.Length - 1);
            if (backingText.Length == 0) textMesh.text = initialValue;
            InputHandler.Instance.StopListenToKeyDown(Tab, KeyCode.Tab);
            InputHandler.Instance.StopListenToKeyDown(Return, KeyCode.Return);
            Button.StopListenForMissedClick(gameObject.name, MissedClick);
            InputHandler.Instance.StopListenToChars(OnInput);
            InputHandler.Instance.StopListenToKey(OnBackspace, KeyCode.Backspace);
        }

        private void OnDestroy() {
            OnDisable();
        }

        private float lastBackspace;
        private const float BackspaceDelay = 0.1f;

        void OnBackspace() {
            if (Time.time - lastBackspace < BackspaceDelay) return;
            if (backingText.Length == 0) return;
            lastBackspace = Time.time;
            backingText = backingText.Remove(backingText.Length - 1);
            if (textMesh.text[textMesh.text.Length - 2] == '\n') textMesh.text = textMesh.text.Remove(textMesh.text.Length - 2, 1);
            textMesh.text = textMesh.text.Remove(textMesh.text.Length - 2, 1);
        }

        void OnInput(char c) {
            if (backingText.Length >= MaxLength || c == '\n' || c == '\t') return;
            backingText += c;
            c = PasswordField ? '*' : c;

            if (backingText.Length != 0 && backingText.Length % LineLength == 0) {
                textMesh.text = textMesh.text.Insert(textMesh.text.Length - 1, "\n" + c);
            } else {
                textMesh.text = textMesh.text.Insert(textMesh.text.Length - 1, c.ToString());
            }
        }

        private void Tab() {
            if (TabTarget == null) return;
            Deselected();
            TabTarget.Selected();
        }

        private void Return() {
            if (currentlySelected != this) return;
            Submit();
        }

        public void Submit() {
            if (!Listeners.ContainsKey(gameObject.name)) return;
            Listeners[gameObject.name]();
        }

        public string Text() {
            return backingText;
        }

    }

}
