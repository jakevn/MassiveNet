// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class PickupSpawner : MonoBehaviour {

        public static PickupSpawner Instance { get { return instance ?? CreateInstance(); } }
        private static PickupSpawner instance;
        private static readonly object LockObj = new object();

        private static PickupSpawner CreateInstance() {
            lock (LockObj) {
                if (applicationIsQuitting) return null;
                return instance ?? (instance = new GameObject("_PickupSpawner").AddComponent<PickupSpawner>());
            }
        }

        private static bool applicationIsQuitting;

        void OnDestroy() {
            applicationIsQuitting = true;
        }


        private readonly List<PickupCreator> setPickups = new List<PickupCreator>();
        private readonly List<PickupCreator> unsetPickups = new List<PickupCreator>(); 
        private NetViewManager viewManager;
        private Vector3 root;

        public int MaxPickups = 16;
        public float RespawnRate = 30;

        void Awake() {
            viewManager = GetComponent<NetViewManager>();
            lock (LockObj) {
                if (instance == null) instance = this;
            }
        }

        public void StartSpawning(Vector3 origin) {
            root = origin;
            if (!IsInvoking("Respawn")) InvokeRepeating("Respawn", 0, RespawnRate);
        }

        private void Respawn() {
            if (setPickups.Count >= MaxPickups) return;
            for (int i = 1; i > 0; i--) {
                Vector3 offset = new Vector3(Random.Range(-200, 200), 0, Random.Range(-200, 200));
                SpawnPickup(Inventory.DbCloneRandom<MeleeWeapon>(), root + offset);
            }
        }

        public void SpawnPickup(IInvItem item, Vector3 position) {
            PickupCreator pickup;
            if (unsetPickups.Count == 0) {
                var pickupObject = viewManager.CreateView("Pickup");
                pickup = pickupObject.GetComponent<PickupCreator>();
            } else {
                pickup = unsetPickups[0];
                unsetPickups.RemoveAt(0);
            }
            pickup.Set(item, position);
            setPickups.Add(pickup);
        }

        public void PickupUnset(PickupCreator pickup) {
            if (setPickups.Contains(pickup)) setPickups.Remove(pickup);
            if (!unsetPickups.Contains(pickup)) unsetPickups.Add(pickup);
        }

        public bool TryGetByViewId(int viewId, out PickupCreator foundPickup) {
            foreach (var pickup in setPickups) {
                if (pickup.GetViewId() != viewId) continue;
                foundPickup = pickup;
                return true;
            }
            foundPickup = null;
            return false;
        }
    }
}