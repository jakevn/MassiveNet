// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class AiManager : MonoBehaviour {

        public int MaxAiCount = 512;
        public float RespawnRate = 16;
        private NetViewManager viewManager;

        private readonly List<AiCreator> alive = new List<AiCreator>();
        private readonly List<AiCreator> dead = new List<AiCreator>();
        private readonly List<AiCreator> unspawned = new List<AiCreator>();

        private Vector3 rootOrigin;

        private void Awake() {
            viewManager = GetComponent<NetViewManager>();
        }

        void Respawn() {
            SpawnAi(Mathf.Clamp(unspawned.Count / 4, 0, MaxAiCount / 16));
        }

        public void StartSpawning(Vector3 origin) {
            InvokeRepeating("Respawn", RespawnRate, RespawnRate);
            rootOrigin = origin;
            SpawnAi(MaxAiCount);
        }

        public void SpawnAi(int count) {
            if (unspawned.Count < count) {
                int current = alive.Count + dead.Count + unspawned.Count;
                int newSpawnCount = Mathf.Clamp(count - unspawned.Count, 0, MaxAiCount - current);
                CreateNewAi(newSpawnCount);
            }
            count = Mathf.Clamp(count, 0, unspawned.Count);
            for (int i = count; i > 0; i--) SpawnAi(rootOrigin);
        }

        private void SpawnAi(Vector3 pos) {
            var ai = unspawned[0];
            unspawned.RemoveAt(0);
            alive.Add(ai);
            ai.Spawn(pos);
        }

        private void CreateNewAi(int count) {
            for (int i = count; i > 0; i--) {
                var aiView = viewManager.CreateView("AI");
                var ai = aiView.GetComponent<AiCreator>();
                ai.SetTargetRoot(rootOrigin);
                ai.AiManager = this;
                unspawned.Add(ai);
            }
        }

        public void AiDied(AiCreator ai) {
            alive.Remove(ai);
            dead.Add(ai);
        }

        public void AiDespawned(AiCreator ai) {
            if (alive.Contains(ai)) alive.Remove(ai);
            if (dead.Contains(ai)) dead.Remove(ai);
            unspawned.Add(ai);
        }
    }

}
