// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class SelectableButton : MonoBehaviour {

        private static readonly Dictionary<GameObject, List<SelectableButton>> selectGroup = new Dictionary<GameObject, List<SelectableButton>>();

        private static void DeselectAllExcept(GameObject parent, SelectableButton selected) {
            if (!selectGroup.ContainsKey(parent)) return;
            foreach (var button in selectGroup[parent]) {
                if (button.Selected && button != selected) button.Deselect();
            }
        }

        private static bool TryGetParent(string name, out GameObject parent) {
            foreach (var kv in selectGroup) {
                if (kv.Key.name != name) continue;
                parent = kv.Key;
                return true;
            }
            parent = null;
            return false;
        }

        public static bool TryGetSelected(string parentName, out SelectableButton selected) {
            GameObject parent;
            selected = null;
            if (!TryGetParent(parentName, out parent)) return false;
            if (!selectGroup.ContainsKey(parent)) return false;
            foreach (var button in selectGroup[parent]) {
                if (!button.Selected) continue;
                selected = button;
                return true;
            }
            return false;
        }

        public Material SelectedMaterial;

        public float ScaleMultiplier = 1.1f;

        public bool DeselectOnMissClick = false;

        private bool selected;
        private Vector3 startScale;
        private Vector3 hoverScale;

        private string buttonName;
        private Material startMaterial;
        private MeshRenderer meshRend;

        private void Awake() {
            startScale = transform.localScale;
            hoverScale = transform.localScale * ScaleMultiplier;
            buttonName = gameObject.name;
            meshRend = GetComponentInChildren<MeshRenderer>();
            if (meshRend != null) startMaterial = meshRend.material;
            if (!selectGroup.ContainsKey(transform.parent.gameObject)) selectGroup.Add(transform.parent.gameObject, new List<SelectableButton>());
            selectGroup[transform.parent.gameObject].Add(this);
            Button.ListenForClick(buttonName, Clicked);
        }

        public bool Selected { get { return selected; } }

        bool ChangeMaterial { get { return meshRend != null && startMaterial != null && SelectedMaterial != null; } }

        void Clicked() {
            Button.ListenForMissedClick(buttonName, MissedClick);
            Select();
        }

        void MissedClick() {
            if (!DeselectOnMissClick) return;
            Button.StopListenForMissedClick(buttonName, MissedClick);
            Deselect();
        }

        public void Select() {
            DeselectAllExcept(transform.parent.gameObject, this);
            selected = true;
            if (!IsInvoking("UpdateScale")) Invoke("UpdateScale", 0.025f);
            if (ChangeMaterial) meshRend.material = SelectedMaterial;
        }

        private void UpdateScale() {
            transform.localScale = Vector3.Lerp(transform.localScale, selected ? hoverScale : startScale, 0.2f);
            if (selected || (!selected && transform.localScale != startScale)) Invoke("UpdateScale", 0.025f);
        }

        public void Deselect() {
            selected = false;
            if (ChangeMaterial) meshRend.material = startMaterial;
        }

    }
}
