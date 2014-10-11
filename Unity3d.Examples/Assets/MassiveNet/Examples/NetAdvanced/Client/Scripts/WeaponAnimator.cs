// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class WeaponAnimator : MonoBehaviour {

        public Transform WeaponMount;

        private GameObject effectObj;

        public Quaternion SwingGoal;
        private Quaternion startAngle;

        private AttackChecker attackChecker;

        public delegate void WeaponFullSwing();

        public event WeaponFullSwing OnWeaponFullSwing;

        private void Awake() {
            if (WeaponMount == null) return;
            startAngle = WeaponMount.localRotation;

            InputHandler.Instance.ListenToKeyDown(StartSwing, KeyBind.Code(Bind.Attack));
        }

        private void CalcHits() {
            if (attackChecker == null) attackChecker = GetComponentInParent<AttackChecker>();
            if (attackChecker != null) attackChecker.CalcHits();
        }

        private void TriggerWeaponEffects() {
            if (effectObj == null) {
                if (WeaponMount.childCount == 0) return;
                var trans = WeaponMount.GetChild(0);
                if (trans == null) return;
                var effectTrans = trans.FindChild("Effect");
                if (effectTrans != null) effectObj = effectTrans.gameObject;
            }
            if (effectObj == null) return;
            effectObj.SetActive(true);
            if (!IsInvoking("DisableWeaponEffect")) Invoke("DisableWeaponEffect", 0.03f);
        }

        private void DisableWeaponEffect() {
            if (effectObj != null) effectObj.SetActive(false);
        }

        private void OnDisable() {
            attackChecker = null;
            effectObj = null;
        }

        private void StartSwing() {
            if (!swing && !backSwing) swing = true;
        }

        private bool swing;
        private bool backSwing;

        private void Update() {
            if (swing || backSwing) Swing();
        }

        private void TriggerOnFullSwing() {
            if (OnWeaponFullSwing != null) OnWeaponFullSwing();
            CalcHits();
            TriggerWeaponEffects();
        }

        public void Swing() {
            if (swing && ToGoal(WeaponMount, SwingGoal, 500f)) {
                swing = false;
                backSwing = true;
                TriggerOnFullSwing();
            }
            if (backSwing && ToGoal(WeaponMount, startAngle, 160f)) backSwing = false;
        }

        private static bool ToGoal(Transform trans, Quaternion goal, float rate) {
            trans.localRotation = Quaternion.RotateTowards(trans.localRotation, goal, rate * Time.deltaTime);
            return (trans.localRotation == goal);
        }

    }

}