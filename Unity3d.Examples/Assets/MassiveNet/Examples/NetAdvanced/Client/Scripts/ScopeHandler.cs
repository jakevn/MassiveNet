// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class ScopeHandler : MonoBehaviour {
        private NetView view;

        private Renderer[] renderers;
        private Collider[] colliders;
        private AudioSource[] audioSources;
        private MonoBehaviour[] monoBehaviours;

        void Start() {
            view = GetComponent<NetView>();
            view.Scope.OnOut += DisableAll;
            view.Scope.OnIn += EnableAll;

            CacheAllComponents();
        }

        public void CacheAllComponents() {
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            audioSources = GetComponentsInChildren<AudioSource>();
            monoBehaviours = GetComponents<MonoBehaviour>();
        }

        public void DisableAll() {
            ChangeScope(false);
        }

        public void EnableAll() {
            ChangeScope(true);
        }

        private void ChangeScope(bool on) {
            foreach (Renderer r in renderers) {
                r.enabled = on;
            }

            foreach (Collider c in colliders) {
                c.enabled = on;
            }

            foreach (AudioSource a in audioSources) {
                a.enabled = on;
            }

            foreach (MonoBehaviour m in monoBehaviours) {
                m.enabled = on;
            }
        }

    }

}
