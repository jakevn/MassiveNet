// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file

using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetSimple {
    public class NetController : MonoBehaviour {
        /// <summary>
        /// When true, SmoothCorrectPosition will perform additional
        /// calculations to avoid bad-looking sliding when a character stops.
        /// This should be used with network objects that unpredictably
        /// and/or frequently start/stop moving, such as players.
        /// </summary>
        public bool PreciseStop = false;

        /// <summary>
        /// The speed at which new rotation values are applied to the object.
        /// The lower the number, the more slowly and smoothly the rotation.
        /// e.g., 1 = 1 second, 5 = 1/5th a second
        /// </summary>
        public int RotationMultiplier = 3;

        /// <summary>
        /// The speed at which position corrections values are applied to the object.
        /// The lower the number, the more slowly and smoothly the correction is applied.
        /// e.g., 1 = 1 second, 5 = 1/5th a second
        /// </summary>
        public int CorrectionMultiplier = 3;

        private Vector3 lastPos;
        private Quaternion lastRot;
        private Vector3 lastVel;
        private float lastTime = 2;

        private Vector3 positionDiff;

        private bool teleport = true;

        private NetView view;

        void Awake() {
            view = GetComponent<NetView>();
            view.OnReadSync += ReadSync;
        }

        void ReadSync(NetStream syncStream) {
            Vector3 position = syncStream.ReadVector3();
            float yRot = syncStream.ReadFloat();
            Vector2 velocity = syncStream.ReadVector2();
            lastTime = 0f;
            lastPos = position;
            lastRot = Quaternion.Euler(0, yRot, 0);
            lastVel = new Vector3(velocity.x, 0, velocity.y);
            if (teleport) {
                if (Vector3.Distance(transform.position, lastPos) > 2f) {
                    transform.position = lastPos;
                    transform.rotation = lastRot;
                }
                teleport = false;
            }
            positionDiff = transform.position - position;
        }

        void Update() {
            if (lastTime > 1) {
                teleport = true;
                return;
            }
            lastTime += Time.deltaTime;
            SmoothCorrectPosition();
            transform.position = transform.position + lastVel * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, lastRot, Time.deltaTime * RotationMultiplier);
        }

        void OnEnable() { 
            // If the View was disabled for a while, it may be in a very different position
            // Trigger teleport to latest position to avoid sliding across huge distances quickly
            lastTime = 2;
        }

        /// <summary>
        /// SmoothCorrectPosition will correct a character's position over time.
        /// This is necessary because the simulation is non-deterministic and
        /// quickly becomes inaccurate.
        /// </summary>
        void SmoothCorrectPosition() {
            if (lastTime > 0.8f) return;
            if (PreciseStop) {
                if (lastVel.magnitude < 0.2 && Vector3.Distance(transform.position, lastPos) < 0.5) return;
            }
            transform.position = Vector3.Lerp(transform.position, transform.position - positionDiff, Time.deltaTime * CorrectionMultiplier);
        }
    }
}
