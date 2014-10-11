// // MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {
    public class PlayerController : MonoBehaviour {

        private NetView netView;

        private Camera cam;
        private readonly Vector3 camOffset = new Vector3(1, 2, -3);

        private float forwardVel;
        private float rotateVel;

        private bool grounded = true;
        private bool jumping;
        private const float JumpVel = 2;
        private const float FallVelGoal = 8;
        private float currJumpVel;
        private float currFallVel;

        private void Start() {
            cam = Camera.main;
            cam.transform.parent = transform;
            cam.transform.position = transform.position + camOffset;

            netView = GetComponent<NetView>();

            InputHandler.Instance.ListenToKey(RotateLeft, KeyBind.Code(Bind.Left));
            InputHandler.Instance.ListenToKey(RotateRight, KeyBind.Code(Bind.Right));
            InputHandler.Instance.ListenToKey(GoForward, KeyBind.Code(Bind.Forward));
            InputHandler.Instance.ListenToKey(GoBackward, KeyBind.Code(Bind.Backward));
            InputHandler.Instance.ListenToKeyDown(Jump, KeyBind.Code(Bind.Jump));
        }

        void RotateLeft() {
            rotateVel = Mathf.Lerp(rotateVel, -10, Time.deltaTime * 7);
        }

        void RotateRight() {
            rotateVel = Mathf.Lerp(rotateVel, 10, Time.deltaTime * 7);
        }

        void GoForward() {
            forwardVel = Mathf.Lerp(forwardVel, 6, Time.deltaTime * 4);
        }

        void GoBackward() {
            forwardVel = Mathf.Lerp(forwardVel, -3, Time.deltaTime * 2);
        }

        private void Update() {
            UpdateRotation();
            UpdateVelocity();
            UpdateJump();
        }

        private void UpdateRotation() {
            if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D)) rotateVel = Mathf.Lerp(rotateVel, 0, Time.deltaTime * 12);
            transform.Rotate(0, rotateVel * Time.smoothDeltaTime * 11, 0);
        }

        private void UpdateVelocity() {
            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S)) forwardVel = Mathf.Lerp(forwardVel, 0, Time.deltaTime * 4);
            transform.position += transform.forward * (forwardVel * Time.deltaTime);
        }

        void Jump() {
            if (jumping || !grounded) return;
            netView.SendReliable("StartJump", RpcTarget.Server);
            currJumpVel = JumpVel;
            jumping = true;
            grounded = false;
            currFallVel = 0;
        }

        private void UpdateJump() {
            if (jumping) {
                currJumpVel -= Time.deltaTime * 5;
                if (currJumpVel < 0) jumping = false;
                else transform.position = new Vector3(transform.position.x, transform.position.y + currJumpVel * Time.deltaTime * 5, transform.position.z);
            } else if (!grounded) {
                currFallVel = Mathf.Lerp(currFallVel, FallVelGoal, Time.deltaTime * 5);
                transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y - currFallVel * Time.deltaTime, 0, transform.position.y), transform.position.z);
                if (transform.position.y > 0.01) return;
                grounded = true;
                transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            }
        }
    }
}