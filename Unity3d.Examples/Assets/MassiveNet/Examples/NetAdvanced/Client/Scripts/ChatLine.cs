// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class ChatLine : MonoBehaviour {

        private TextMesh textMesh;

        public Vector3 LineOnePos;

        public Vector3 BumpAmount;

        private float SetTime { get; set; }

        private int bumpCount;

        private void Awake() {
            textMesh = GetComponent<TextMesh>();
        }

        private void Update() {
            if (Time.time - SetTime > 15) ClearLine();
        }

        public void SetLine(string line, Color32 color) {
            transform.localPosition = LineOnePos;
            bumpCount = 0;
            SetTime = Time.time;
            textMesh.text = line;
            textMesh.color = color;
            enabled = true;
        }

        public void Bump() {
            transform.localPosition += BumpAmount;
            bumpCount++;
            if (bumpCount > 12) ClearLine();
        }

        public void ClearLine() {
            textMesh.text = "";
            enabled = false;
        }

    }

}