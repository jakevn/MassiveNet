// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class LocalAvoidance : MonoBehaviour {

        public static LocalAvoidance Instance { get { return instance ?? Create(); } }
        private static LocalAvoidance instance;

        private static LocalAvoidance Create() {
            if (applicationIsQuitting) return null;
            instance = new GameObject("LocalAvoidance").AddComponent<LocalAvoidance>();
            return instance;
        }

        private static bool applicationIsQuitting;


        void OnDestroy() {
            applicationIsQuitting = true;
        }

        private readonly List<Func<LAState>> goalFuncs = new List<Func<LAState>>();

        private readonly List<LAState> states = new List<LAState>();

        public struct LAState {
            public Action<Vector3> ResultCallback;
            public Vector3 CurrPos;
            public float Speed;
            public Vector3 GoalPos;
            public int GoalId;
        }

        private readonly List<LAState> sharedGoal = new List<LAState>();
        private readonly List<Vector3> results = new List<Vector3>();

        void Update() {
            for (int i = goalFuncs.Count - 1; i >= 0; i--) {
                states.Add(goalFuncs[i].Invoke());
            }
            while (states.Count > 0) {
                var fstate = states[0];
                states.RemoveAt(0);
                sharedGoal.Add(fstate);
                for (int i = states.Count - 1; i >= 0; i--) {
                    var g = states[i];
                    if (g.GoalId != fstate.GoalId) continue;
                    states.RemoveAt(i);
                    sharedGoal.Add(g);
                }
                for (int i = sharedGoal.Count - 1; i >= 0; i--) {
                    var state = sharedGoal[i];
                    float dist = Vector3.Distance(state.CurrPos, state.GoalPos);
                    Vector3 result;
                    if (dist < 1.5) {
                        result = state.CurrPos;
                    } else {
                        result = Vector3.MoveTowards(state.CurrPos, state.GoalPos, Time.deltaTime * state.Speed);
                        foreach (Vector3 res in results) {
                            //if (Vector3.Distance(result, res) < 0.5) result = result + (Vector3.right*state.Speed*Time.deltaTime);
                            if (Vector3.Distance(result, res) > 1.5) continue;
                            result = Vector3.MoveTowards(state.CurrPos, state.GoalPos + GoalOffset(results.Count, dist), Time.deltaTime * state.Speed);
                            break;
                        }
                    }
                    state.ResultCallback(result);
                    results.Add(result);
                }
                results.Clear();
                sharedGoal.Clear();
            }
            states.Clear();
        }

        private Vector3 GoalOffset(int index, float dist) {
            float dampener = 0.7f + ((index / 8) * 0.07f);
            if (index > 8) index = index % 8 + 1;
            float offset = Mathf.Clamp(dist * dampener, 1.3f, 16f);
            switch (index) {
                case 1:
                    return new Vector3(offset, 0, 0);
                case 2:
                    return new Vector3(offset, 0, offset);
                case 3:
                    return new Vector3(0, 0, offset);
                case 4:
                    return new Vector3(-offset, 0, 0);
                case 5:
                    return new Vector3(-offset, 0, -offset);
                case 6:
                    return new Vector3(0, 0, -offset);
                case 7:
                    return new Vector3(offset, 0, -offset);
                case 8:
                    return new Vector3(-offset, 0, offset);
                default:
                    return Vector3.zero;
            }
        }

        public void RegisterGoalFunc(Func<LAState> goalFunc) {
            if (!goalFuncs.Contains(goalFunc)) goalFuncs.Add(goalFunc);
        }

        public void DeregisterGoalFunc(Func<LAState> goalFunc) {
            if (goalFuncs.Contains(goalFunc)) goalFuncs.Remove(goalFunc);
        }
    }
}