// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

namespace Massive.Examples.NetSimple {
    public class PlayerController : MonoBehaviour {

        private Camera cam;
        private readonly Vector3 camOffset = new Vector3(1, 2, -3);

        private float forwardVel;
        private float rotateVel;

        private void Start() {
            cam = Camera.main;
            cam.transform.parent = transform;
            cam.transform.position = transform.position + camOffset;
        }

        private void Update() {
            // Get left/right key input and set rotation:
            if (Input.GetKey(KeyCode.A)) rotateVel = Mathf.Lerp(rotateVel, -10, Time.deltaTime * 8);
            else if (Input.GetKey(KeyCode.D)) rotateVel = Mathf.Lerp(rotateVel, 10, Time.deltaTime * 8);
            else rotateVel = Mathf.Lerp(rotateVel, 0, Time.deltaTime * 10);

            transform.Rotate(0, rotateVel * Time.smoothDeltaTime * 9, 0);

            // Get forward/backward key input and set position:
            if (Input.GetKey(KeyCode.W)) forwardVel = Mathf.Lerp(forwardVel, 12, Time.deltaTime * 4);
            else if (Input.GetKey(KeyCode.S)) forwardVel = Mathf.Lerp(forwardVel, -6, Time.deltaTime * 2);
            else forwardVel = Mathf.Lerp(forwardVel, 0, Time.deltaTime * 8);

            transform.position += transform.forward * (forwardVel * Time.deltaTime);
        }
    }
}