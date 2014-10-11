using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public class CatAnimator : MonoBehaviour {

        public bool Dead;

        public Transform FrontLeftLegJoint;
        public Transform FrontRightLegJoint;
        public Transform BackLeftLegJoint;
        public Transform BackRightLegJoint;

        public Transform Body;
        public Transform Head;

        public Quaternion FrontLegForward;
        public Quaternion FrontLegBackward;

        public Quaternion BackLegForward;
        public Quaternion BackLegBackward;

        private Quaternion frontLegResting;
        private Quaternion backLegResting;
        private Quaternion bodyResting;
        private Quaternion headResting;

        private void Awake() {
            frontLegResting = FrontLeftLegJoint.localRotation;
            backLegResting = BackLeftLegJoint.localRotation;
            bodyResting = Body.localRotation;
            headResting = Head.localRotation;
        }

        private Vector3 lastPos;

        private void LateUpdate() {
            if (Dead) {
                PlayDead();
                return;
            }
            float speed = Vector3.Magnitude(transform.position - lastPos) * 3 / Time.deltaTime;
            lastPos = transform.position;
            if (speed > 0.5) Run(speed);
            else StopRun();
        }

        public void Reset() {
            Dead = false;
            opposite = false;
            lastPos = transform.position;
            FrontLeftLegJoint.localRotation = frontLegResting;
            FrontRightLegJoint.localRotation = frontLegResting;
            BackLeftLegJoint.localRotation = backLegResting;
            BackRightLegJoint.localRotation = backLegResting;
            Body.localRotation = bodyResting;
            Head.localRotation = headResting;
        }

        void OnEnable() {
            Reset();
        }

        private void PlayDead() {
            ToGoal(Body, Quaternion.Euler(90, 0, 0), 250f);
        }

        private bool opposite;

        private void Run(float speed) {
            if (ToGoal(FrontLeftLegJoint, opposite ? FrontLegForward : FrontLegBackward, speed * 10f)) opposite = !opposite;

            ToGoal(BackLeftLegJoint, opposite ? BackLegBackward : BackLegForward, speed * 10f);
            ToGoal(BackRightLegJoint, opposite ? BackLegForward : BackLegBackward, speed * 10f);

            ToGoal(FrontRightLegJoint, opposite ? FrontLegBackward : FrontLegForward, speed * 10f);
        }

        private void StopRun() {
            ToGoal(FrontLeftLegJoint, frontLegResting, 100f);
            ToGoal(FrontRightLegJoint, frontLegResting, 100f);
            ToGoal(BackRightLegJoint, backLegResting, 100f);
            ToGoal(BackLeftLegJoint, backLegResting, 100f);
        }

        private static bool ToGoal(Transform trans, Quaternion goal, float rate) {
            trans.localRotation = Quaternion.RotateTowards(trans.localRotation, goal, rate * Time.deltaTime);
            return (trans.localRotation == goal);
        }

    }

}