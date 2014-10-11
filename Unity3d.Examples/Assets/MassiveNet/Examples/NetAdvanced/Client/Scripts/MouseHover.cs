// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using UnityEngine;

public class MouseHover : MonoBehaviour {

    public delegate void HoverStart();

    public event HoverStart OnHoverStart;

    public delegate void HoverEnd();

    public event HoverEnd OnHoverEnd;

    public float HoverDelay = 0.2f;

    private float start;
    private bool triggered;

    private Collider col;

    void Awake() {
        col = GetComponent<Collider>();
        if (col == null) {
            Debug.LogError("Collider not found. Collider component required for MouseHover.");
        }
    }

    void Update() {
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, 100.0f) || hit.collider != col) {
            start = 0;
            if (triggered) TriggerHoverEnd();
        } else {
            if (start == 0) start = Time.deltaTime;
            else if (!triggered && Time.time - start > HoverDelay) TriggerHoverStart();
        }
    }

    void TriggerHoverStart() {
        triggered = true;
        if (OnHoverStart != null) OnHoverStart();
    }

    void TriggerHoverEnd() {
        triggered = false;
        if (OnHoverEnd != null) OnHoverEnd();
    }
}
