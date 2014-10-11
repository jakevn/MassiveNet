// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
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
            for (int i = 0; i < renderers.Length; i++) {
                renderers[i].enabled = on;
            }

            for (int i = 0; i < colliders.Length; i++) {
                colliders[i].enabled = on;
            }

            for (int i = 0; i < audioSources.Length; i++) {
                audioSources[i].enabled = on;
            }

            for (int i = 0; i < monoBehaviours.Length; i++) {
                monoBehaviours[i].enabled = on;
            }
        }

    }
}
