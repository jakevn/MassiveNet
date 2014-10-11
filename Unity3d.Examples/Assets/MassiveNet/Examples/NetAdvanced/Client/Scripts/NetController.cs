using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

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
        private Vector3 lastVel;
        private float lastTime = -2;

        private Vector3 positionDiff;

        private NetView view;

        void Awake() {
            view = GetComponent<NetView>();
            view.OnReadSync += ReadSync;
        }

        void ReadSync(NetStream syncStream) {
            Vector3 position = new Vector3(syncStream.ReadFloat(), transform.position.y, syncStream.ReadFloat());
            Vector2 velocity = syncStream.ReadVector2();
            
            lastPos = position;
            lastVel = new Vector3(velocity.x, 0, velocity.y);
            if (Time.time - lastTime > 1.2) {
                if (Vector3.Distance(transform.position, lastPos) > 2f) {
                    transform.position = lastPos;
                }
            }
            lastTime = Time.time;
            positionDiff = transform.position - position;
        }

        private Vector3 localLastPos;
        void Update() {
            if (Time.time - lastTime > 1.2) return;
            SmoothCorrectPosition();
            transform.position = transform.position + lastVel * Time.deltaTime;
            Vector3 vel = transform.position - localLastPos;
            if (vel != Vector3.zero) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(vel), Time.deltaTime * 4);
            localLastPos = transform.position;
        }

        /// <summary>
        /// SmoothCorrectPosition will correct a character's position over time.
        /// This is necessary because the simulation is non-deterministic and
        /// quickly becomes inaccurate.
        /// </summary>
        void SmoothCorrectPosition() {
            if (Time.time - lastTime > 0.8f) return;
            float dist = Vector3.Distance(transform.position, lastPos);
            if (PreciseStop) {
                if (lastVel.magnitude < 0.2 && dist < 0.5) return;
            }
            transform.position = dist > 10 ? lastPos : Vector3.Lerp(transform.position, transform.position - positionDiff, Time.deltaTime * CorrectionMultiplier);
        }

    }

}
