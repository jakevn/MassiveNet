// Copyright 2014 - Inhumane Software - legal@inhumanesoftware.com

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Maintains state and configuration for scope calculation, as well as events for when this particular
    /// scope goes in/out of scope for the connection associated with this socket.
    /// </summary>
    public class NetScope {

        internal NetScopeManager ScopeManager;

        /// <summary> How close a connection must be to the NetView for the connection to be set in-scope for the NetView. </summary>
        public int InScopeDist {
            get {
                return inScopeDist;
            }
            set {
                if (value > OutScopeDist) throw new Exception("Failed to set InScopeDist: InScopeDist cannot be greater than OutScopeDist.");
                inScopeDist = value;
            }
        }
        private int inScopeDist = 150;

        /// <summary> How far away a connection must be to a NetView for the connection to be set out-of-scope for the NetView. </summary>
        public int OutScopeDist {
            get {
                return outScopeDist;
            }
            set {
                if (value < InScopeDist) throw new Exception("Failed to set OutScopeDist: OutScopeDist cannot be less than InScopeDist.");
                outScopeDist = value;
            }
        }
        private int outScopeDist = 200;

        /// <summary> If within this distance, scope level will be set to one (the most frequent sync rate).
        /// Anything beyond this range will be set to either scope level two or three. </summary>
        public int LevelOne {
            get {
                return levelOne;
            }
            set {
                if (value > levelTwo) throw new Exception("Failed to set LevelOne: Cannot be greater than LevelTwo");
                levelOne = value;
            }
        }
        private int levelOne = 40;

        /// <summary> If within this distance, scope level will be set to two (the middle frequency sync rate).
        ///  Anything beyond this range will be set to scope level three.</summary>
        public int LevelTwo {
            get {
                return levelTwo;
            }
            set {
                if (value < levelOne) throw new Exception("Failed to set LevelTwo: Cannot be less than LevelOne");
                levelTwo = value;
            }
        }
        private int levelTwo = 100;

        public delegate void OutEvent();

        /// <summary> Signals that this Scope has gone out of scope for us. </summary>
        public event OutEvent OnOut;

        public delegate void InEvent();

        /// <summary> Signals that this Scope has come back into scope for us. </summary>
        public event InEvent OnIn;

        /// <summary> ViewId/Scope network level of detail (LOD) lookup for each in-scope view. 1=Every, 2=Every other, 3=Every fourth sync. </summary>
        private readonly Dictionary<int, int> levels = new Dictionary<int, int>();

        /// <summary> When true, this scope overrides values used for scope calculation. That is, when another scope is being checked against
        /// this scope, if normally the other scope's values would be used, this scope's values will be used instead. </summary>
        internal bool TakePrecedence = false;

        /// <summary> Gets the position that should be used for distance-based scope calculation. </summary>
        public Vector3 Position {
            get { return trans != null ? trans.position : position; }
            internal set { position = value; }
        }

        private Vector3 position = Vector3.zero;

        /// <summary> Gets/sets the transform that is used for distance-based scope calculation. </summary>
        public Transform Trans {
            get { return trans; }
            set { trans = value; }
        }

        internal bool CalcDisabled = false;
        /// <summary> On next scope calculation, will set to out-of-scope for all. Will not
        /// participate in future scope calculations unless re-enabled. </summary>
        public void DisableScopeCalculation() {
            CalcDisabled = true;
        }

        /// <summary> Resumes scope calculation for this scope. </summary>
        public void EnableScopeCalculation() {
            CalcDisabled = false;
        }

        /// <summary> Fires OnOut when this view has been set as out of scope. </summary>
        internal void FireOutEvent() {
            if (OnOut != null) OnOut();
        }

        /// <summary> Fires OnIn when this view has been set as in scope. </summary>
        internal void FireInEvent() {
            if (OnIn != null) OnIn();
        }

        /// <summary> When set, this transform will be used for scope calculation instead of the base transform. </summary>
        private Transform trans;

        /// <summary> Returns true if has a non-zero position. </summary>
        public bool HasPosition {
            get { return Position != Vector3.zero; }
        }

        /// <summary> Returns true if the supplied ViewID is in-scope. </summary>
        public bool In(int viewId) {
            return levels.ContainsKey(viewId);
        }

        /// <summary> Returns true if the supplied ViewID is in-scope at the provided scope level. </summary>
        public bool In(int viewId, int scopeLevel) {
            if (!levels.ContainsKey(viewId)) return false;
            int lod = levels[viewId];
            return (lod == scopeLevel || lod == 1 || (lod == 2 && scopeLevel == 4));
        }

        /// <summary> Returns the 0-3 value scope level for the supplied viewId. 0 = not in-scope. </summary>
        public int Level(int viewId) {
            return levels.ContainsKey(viewId) ? levels[viewId] : 0;
        }

        /// <summary> Sets a NetView as out-of-scope. </summary>
        public void SetOut(int viewId) {
            if (levels.ContainsKey(viewId)) levels.Remove(viewId);
        }

        /// <summary> Sets a NetView as in-scope. </summary>
        public void SetIn(int viewId, int scopeLevel) {
            if (levels.ContainsKey(viewId)) levels[viewId] = scopeLevel;
            else levels.Add(viewId, scopeLevel);
        }

        /// <summary> Sets a NetView as in-scope with the default scope level of 1. </summary>
        public void SetIn(int viewId) {
            SetIn(viewId, 1);
        }
    }
}