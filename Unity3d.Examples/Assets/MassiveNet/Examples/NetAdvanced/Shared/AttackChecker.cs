// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class AttackChecker : MonoBehaviour {

        public delegate void TakeDamage(NetView attackerView, int damage);

        public event TakeDamage OnTakeDamage;

        public delegate int CalculateDamage();

        public CalculateDamage OnCalculateDamage;

        public delegate Bounds CalculateBounds();

        public CalculateBounds OnCalculateBounds;

        private NetView view;
        public List<string> TargetTags = new List<string>();

        private Vector3 boundsSize = new Vector3(6, 6, 6);

        private void Start() {
            view = GetComponent<NetView>();
        }

        public void SetBoundsSize(Vector3 size) {
            boundsSize = size;
        }

        private readonly List<Transform> targetCache = new List<Transform>();

        public void CalcHits() {
            foreach (string targetTag in TargetTags) {
                var arr = GameObject.FindGameObjectsWithTag(targetTag);
                foreach (GameObject go in arr) {
                    targetCache.Add(go.transform);
                }
            }
            Bounds bounds = TriggerCalculateBounds();
            foreach (Transform target in targetCache) {
                if (Hit(target, bounds)) RegisterHit(target);
            }
            targetCache.Clear();
        }

        private void RegisterHit(Transform hitTarget) {
            NetView hitView = hitTarget.GetComponent<NetView>();
            if (hitView == null) return;
            if (!view.AmServer) view.SendReliable("ReceiveHit", RpcTarget.Server, hitView.Id);
        }

        [NetRPC]
        private void ReceiveHit(int id) {
            NetView hitView;
            if (!view.ViewManager.TryGetView(id, out hitView)) return;
            var attackChecker = hitView.GetComponent<AttackChecker>();
            if (attackChecker == null) return;
            attackChecker.TriggerTakeHit(view, TriggerCalculateDamage());
        }

        private int TriggerCalculateDamage() {
            if (OnCalculateDamage != null) return OnCalculateDamage();
            else return 25;
        }

        private void TriggerTakeHit(NetView from, int damage) {
            if (OnTakeDamage != null) OnTakeDamage(from, damage);
        }

        private Bounds TriggerCalculateBounds() {
            return OnCalculateBounds != null ? OnCalculateBounds() : new Bounds(transform.position, boundsSize);
        }

        public bool Hit(Transform target, Bounds bounds) {
            return (bounds.Contains(target.position));
        }

    }

}
