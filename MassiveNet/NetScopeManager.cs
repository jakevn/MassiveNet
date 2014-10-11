// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections.Generic;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Contains various implementations for scope calculation.
    /// </summary>
    public class NetScopeManager : MonoBehaviour {

        // Position and SliceSize determine current slice and slice size:
        private int Position { get; set; }
        private const int SliceSize = 32;

        internal NetViewManager ViewManager;
        internal NetSocket Socket;

        private void Awake() {
            Socket = GetComponent<NetSocket>();
            ViewManager = GetComponent<NetViewManager>();
            ViewManager.OnNetViewCreated += FullScopeCalculation;
        }

        private void Update() {
            IncrementalScopeUpdate();
        }

        /// <summary> Updates scope for a limited slice of Views. Every connection with a scope is
        /// updated for the current slice. Slice position is maintained between invocations. Typically
        /// this is called every frame and is designed to spread the overhead of scope calculation across
        /// all frames for consistent performance. </summary>
        private void IncrementalScopeUpdate() {
            for (int i = 0; i < Socket.Connections.Count; i++) {
                NetConnection connection = Socket.Connections[i];
                if (connection.IsServer || !connection.HasScope) continue;
                UpdateScopeRange(connection, ViewManager.Views);
            }
            Position += SliceSize;
        }

        /// <summary> Calculates the scope for the supplied connection against the scope of every view.
        /// This is useful, for example, when a connection is first created and an immediate population
        /// of views is desired. This can be detrimental when there is so much instantiation data that
        /// sending it all at once would cause an immediate send window overflow. </summary>
        internal void FullScopeCalculation(NetConnection connection) {
            for (int i = 0; i < ViewManager.Views.Count; i++) {
                var view = ViewManager.Views[i];
                if (view.Server != Socket.Self) continue;
                if (!connection.InGroup(view.Group) || !view.CanInstantiateFor(connection)) continue;
                if (!UpdateScope(connection.Scope, view) && !view.IsController(connection)) continue;
                view.SendInstantiateData(connection);
            }
        }

        /// <summary> Calculates scope for every connection against the provided view's scope.
        /// This is useful, for example, when creating a new view so that it can be immediately
        /// instantiated since a delay may be undesireable. </summary>
        internal void FullScopeCalculation(NetView view) {
            if (view.Server != Socket.Self) return;
            for (int i = 0; i < Socket.Connections.Count; i++) {
                NetConnection connection = Socket.Connections[i];
                if (connection.IsServer || !connection.HasScope) continue;
                if (view.IsController(connection)) {
                    view.SendInstantiateData(connection);
                } else if (connection.InGroup(view.Group) && view.CanInstantiateFor(connection) && UpdateScope(connection.Scope, view)) {
                    view.SendInstantiateData(connection);
                }
            }
        }

        /// <summary> Working with a given connection, a List of all views, SliceSize and Position,
        /// the connection has its scope updated for the given position/slice of the provided list
        /// of views. If views.Count == 512, Position == 64, and slice size == 32, connection will
        /// have scope updated for views occupying index 64 through 96 of the views List only. </summary>
        internal void UpdateScopeRange(NetConnection connection, List<NetView> views) {
            if (Position >= views.Count) Position = 0;

            int count = SliceSize;
            NetScope scope = connection.Scope;
            for (int i = Position; i < views.Count; i++) {
                var view = views[i];
                if (view != connection.View && view.Server == Socket.Self && connection.InGroup(view.Group) && view.CanInstantiateFor(connection) && UpdateScope(scope, view)) {
                    if (scope.In(view.Id)) view.SendInstantiateData(connection);
                    else ViewManager.SendOutOfScope(view.Id, connection);
                }
                count--;
                if (count == 0) break;
            }
        }

        /// <summary> The provided scope is updated if the provided view has gone in or out of scope.
        /// False is returned if there is no change. </summary>
        internal bool UpdateScope(NetScope source, NetView view) {
            bool scopeChanged = false;

            float distance = Vector3.Distance(source.Position, view.Scope.Position);
            // If the source scope is configured to override, use its scope distances instead:
            var rulesScope = source.TakePrecedence ? source : view.Scope;
            if (view.Scope.CalcDisabled || distance > rulesScope.OutScopeDist) {
                if (!source.In(view.Id)) return false;
                scopeChanged = true;
                source.SetOut(view.Id);
            } else if (distance < rulesScope.InScopeDist) {
                if (!source.In(view.Id)) scopeChanged = true;

                if (distance < rulesScope.LevelOne) source.SetIn(view.Id, 1);
                else if (distance < rulesScope.LevelTwo) source.SetIn(view.Id, 2);
                else source.SetIn(view.Id, 3);
            }
            return scopeChanged;
        }
    }
}