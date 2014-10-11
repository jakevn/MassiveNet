// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {
    public class AggroMagnet : MonoBehaviour {

        private static readonly List<AggroMagnet> Magnets = new List<AggroMagnet>();

        private static readonly List<Vector3> Offsets = new List<Vector3> {
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(-1, 0, 0),
            new Vector3(-1, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, 1),
            new Vector3(1, 0, -1),
        };

        private static int nextTalker;

        private static void AddMagnet(AggroMagnet magnet) {
            if (!Magnets.Contains(magnet)) Magnets.Add(magnet);
            if (Magnets.Count == 1) TalkNext();
        }

        private static void DoneTalking() {
            if (Magnets.Count == 0) return;
            if (Magnets.Count > nextTalker + 1) nextTalker++;
            else nextTalker = 0;
            TalkNext();
        }

        private static void TalkNext() {
            Magnets[nextTalker].PrepareToTalk(2f / Magnets.Count);
        }

        private static void RemoveMagnet(AggroMagnet magnet) {
            AggroChaser.RemoveMagnetAggro(magnet.transform);
            if (!Magnets.Contains(magnet)) return;
            int index = Magnets.IndexOf(magnet);
            Magnets.Remove(magnet);
            if (nextTalker == index) DoneTalking();
        }

        private bool[] offsetTaken = new[] {false, false, false, false, false, false, false, false};

        public bool TryGetOffset(out Vector3 offset) {
            for (int i = 0; i < offsetTaken.Count(); i++) {
                if (offsetTaken[i]) continue;
                offsetTaken[i] = true;
                offset = Offsets[i];
                return true;
            }
            offset = Vector3.zero;
            return false;
        }

        public void ReleaseOffset(Vector3 offset) {
            if (!Offsets.Contains(offset)) return;
            int index = Offsets.IndexOf(offset);
            offsetTaken[index] = false;
        }

        private void PrepareToTalk(float time) {
            Invoke("Talk", time);
        }

        private void Talk() {
            AggroChaser.AggroPing(transform);
            DoneTalking();
        }

        private void OnEnable() {
            AddMagnet(this);
        }

        private void OnDisable() {
            RemoveMagnet(this);
            if (IsInvoking("Talk")) CancelInvoke("Talk");
        }

        private void OnDestroy() {
            OnDisable();
        }

        public void ChangeGlobalAggro(float amount) {
            AggroChaser.ChangeGlobalAggro(transform, amount);
        }

        public void RemoveAllAggro() {
            AggroChaser.RemoveMagnetAggro(transform);
        }

        public void IncreaseAggroFor(Transform possibleChaser, int amount) {
            AggroChaser.MaybeChangeAggro(transform, possibleChaser, amount);
        }

    }
}