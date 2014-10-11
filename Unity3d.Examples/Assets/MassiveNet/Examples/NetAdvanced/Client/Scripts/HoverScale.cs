// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class HoverScale : MonoBehaviour {

        private MouseHover hover;

        public float ScaleMultiplier = 1.1f;

        private bool hovering;
        private Vector3 startScale;
        private Vector3 hoverScale;

        private void Awake() {
            hover = GetComponentInChildren<MouseHover>() ?? GetComponent<MouseHover>();
            if (hover == null) {
                Debug.LogError("HoverScale needs MouseHover component on same GameObject or in children.");
                return;
            }
            hover.OnHoverStart += HoverStart;
            hover.OnHoverEnd += HoverEnd;
            startScale = transform.localScale;
            hoverScale = transform.localScale * ScaleMultiplier;
        }

        private void HoverStart() {
            hovering = true;
            if (!IsInvoking("UpdateScale")) Invoke("UpdateScale", 0.025f);
        }

        private void UpdateScale() {
            transform.localScale = Vector3.Lerp(transform.localScale, hovering ? hoverScale : startScale, 0.2f);
            if (hovering || (!hovering && transform.localScale != startScale)) Invoke("UpdateScale", 0.025f);
        }

        private void HoverEnd() {
            hovering = false;
        }

    }

}
