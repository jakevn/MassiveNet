// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Net;
using UnityEngine;

namespace MassiveNet {
    /// <summary>
    /// Contains configuration parameters for Zones.
    /// </summary>
    public class NetZone {
        public uint Id { get; internal set; }

        public bool Available { get; internal set; }

        public bool Assigned { get; internal set; }

        public IPEndPoint ServerEndpoint { get; internal set; }

        public string PublicEndpoint { get; internal set; }

        public NetConnection Server { get; internal set; }

        public Vector3 Position { get; internal set; }

        public int ViewIdMin { get; internal set; }

        public int ViewIdMax { get; internal set; }

        /// <summary>
        /// A NetConnection will be forced to handover to this Zone when within this range.
        /// </summary>
        public int HandoverMinDistance = 100;

        /// <summary>
        /// The maximum distance from center a NetConnection can be before forcing handoff to another zone or disconnect.
        /// </summary>
        public int HandoverMaxDistance = 400;

        /// <summary>
        /// The radius of the zone. Under ideal conditions, this zone will only control NetConnections within this range.
        /// </summary>
        public int ZoneSize = 300;

        /// <summary>
        /// How close a NetConnection must be to this Zone center before this Zone takes control.
        /// </summary>
        public int HandoverDistance = 200;

        internal void RemoveServer() {
            Server = null;
            Assigned = false;
        }

        internal bool InRange(Vector3 position) {
            return Vector3.Distance(Position, position) < ZoneSize;
        }

        internal bool InRangeMax(Vector3 position) {
            return Vector3.Distance(Position, position) < HandoverMaxDistance;
        }

        internal float Distance(Vector3 position) {
            return Vector3.Distance(Position, position);
        }

        /// <summary>
        /// Creates a Zone using the default size and handoff ranges.
        /// </summary>
        internal NetZone(Vector3 position, int viewIdMin, int viewIdMax) {
            Id = NetMath.RandomUint();
            ViewIdMin = viewIdMin;
            ViewIdMax = viewIdMax;
            Position = position;
        }

        /// <summary>
        /// Creates a Zone using custom size and handoff ranges.
        /// </summary>
        internal NetZone(Vector3 position, int viewIdMin, int viewIdMax, int handoff, int handoffMin, int handoffMax, int zoneSize) {
            Id = NetMath.RandomUint();
            ViewIdMin = viewIdMin;
            ViewIdMax = viewIdMax;
            Position = position;
            HandoverDistance = handoff;
            HandoverMinDistance = handoffMin;
            HandoverMaxDistance = handoffMax;
            ZoneSize = zoneSize;
        }

        /// <summary> Constructor used for deserialization. </summary>
        internal NetZone() {}
    }
}