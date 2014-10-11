// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System.Collections;
using System.Collections.Generic;
using Massive.Examples.NetAdvanced;
using UnityEngine;

public class AggroChaser : MonoBehaviour {

    private static readonly List<AggroChaser> Chasers = new List<AggroChaser>();
    private static readonly List<Transform> ChaserTrans = new List<Transform>();

    public static void MaybeChangeAggro(Transform magnet, Transform possibleChaser, float amount) {
        if (!ChaserTrans.Contains(possibleChaser)) return;
        var chaser = Chasers[ChaserTrans.IndexOf(possibleChaser)];
        chaser.ChangeModifier(magnet, amount);
    }

    public static void ChangeGlobalAggro(Transform magnet, float amount) {
        for (int i = Chasers.Count - 1; i >= 0; i--) {
            Chasers[i].ChangeModifier(magnet, amount);
        }
    }

    public static void RemoveMagnetAggro(Transform magnet) {
        for (int i = Chasers.Count - 1; i >= 0; i--) {
            Chasers[i].RemoveAggro(magnet);
        }
    }

    public static void AggroPing(Transform magnet) {
        for (int i = Chasers.Count - 1; i >= 0; i--) {
            Chasers[i].UpdateAggro(magnet);
        }
    }

    static void AddChaser(AggroChaser chaser, Transform trans) {
        if (ChaserTrans.Contains(trans)) return;
        Chasers.Add(chaser);
        ChaserTrans.Add(trans);
    }

    static void RemoveChaser(AggroChaser chaser, Transform trans) {
        if (ChaserTrans.Contains(trans)) ChaserTrans.Remove(trans);
        if (Chasers.Contains(chaser)) Chasers.Remove(chaser);
    }


    public delegate void NewTarget(Transform target, Vector3 offset);
    public event NewTarget OnNewTarget;

    public delegate void RemoveTarget();
    public event RemoveTarget OnRemoveTarget;

    public float ProximityAggroRadius = 16;
    public float ProximityAggroGain = 4;
    public float ProximityAggroDecay = 1;

    public float AggroStartThreshold = 6;
    public float AggroEndThreshold = 0;

    public int MaxAggroCount = 16;
    public float MaxAggroAmount = 32;
    public float MaxDistance = 64;

    private float targetStartTime;
    private const float MaxTargetingTime = 60f;
    private const float TargetSwitchCooldown = 3f;

    private float randomDelay;

    private readonly List<Transform> aggroTrans = new List<Transform>();
    private readonly List<float> aggroAmount = new List<float>();

    private Transform currentTarget;

    void OnEnable() {
        StartCoroutine(AggroDecay());
        AddChaser(this, transform.root);
        randomDelay = Random.Range(0.7f, 1.1f);
    }

    void OnDisable() {
        if (currentTarget != null) RemoveAggro(currentTarget);
        aggroTrans.Clear();
        aggroAmount.Clear();
        RemoveChaser(this, transform.root);
        StopCoroutine(AggroDecay());
    }

    void OnDestroy() {
        OnDisable();
    }

    void UpdateAggro(Transform magnet) {
        if (aggroTrans.Contains(magnet)) return;
        if (Vector3.Distance(transform.position, magnet.position) > ProximityAggroRadius) return;
        ChangeModifier(magnet, ProximityAggroGain);
    }

    IEnumerator AggroDecay() {
        while (enabled) {
            yield return new WaitForSeconds(randomDelay);
            if (!enabled || aggroTrans.Count == 0) continue;
            for (int i = aggroTrans.Count - 1; i >= 0; i--) {
                if (aggroTrans[i] == null) {
                    RemoveAtIndex(i);
                    continue;
                }
                float distance = Vector3.Distance(transform.position, aggroTrans[i].position);
                if (distance > MaxDistance) RemoveAggro(aggroTrans[i]);
                else if (distance > ProximityAggroRadius) ChangeModifier(aggroTrans[i], -ProximityAggroDecay * randomDelay);
                else ChangeModifier(aggroTrans[i], ProximityAggroGain * randomDelay);
            }
            UpdateTarget();
        }
    }

    void UpdateTarget() {
        if (aggroTrans.Count == 0) return;
        int highIndex = HighestAggroIndex();
        if (currentTarget != null) {
            if (aggroTrans.IndexOf(currentTarget) == highIndex) return;
            if (aggroAmount[highIndex] < AggroStartThreshold || !CanSwitchTarget()) return;
            SetTarget(highIndex);
        } else if (aggroAmount[highIndex] >= AggroStartThreshold) SetTarget(highIndex);       
    }

    private Vector3 claimedOffset = Vector3.zero;
    void SetTarget(int index) {
        var newAggroTrans = aggroTrans[index];
        AggroMagnet magnet = newAggroTrans.GetComponent<AggroMagnet>();
        if (magnet == null) return;
        Vector3 targetOffset;
        if (!magnet.TryGetOffset(out targetOffset)) return;
        currentTarget = newAggroTrans;
        claimedOffset = targetOffset;
        targetStartTime = Time.time;
        if (OnNewTarget != null) OnNewTarget(aggroTrans[index], targetOffset);
    }

    bool TargetTimeout() {
        return Time.time - targetStartTime > MaxTargetingTime;
    }

    bool CanSwitchTarget() {
        return Time.time - targetStartTime > TargetSwitchCooldown;
    }

    void ChangeModifier(Transform magnet, float amount) {
        if (aggroTrans.Contains(magnet)) {
            int index = aggroTrans.IndexOf(magnet);
            aggroAmount[index] += amount;
            if (aggroAmount[index] <= AggroEndThreshold) RemoveAggro(magnet);
        } else if (amount > AggroEndThreshold) {
            AddAggro(magnet, amount);
        }
    }

    void RemoveAggro(Transform magnet) {
        if (!aggroTrans.Contains(magnet)) return;
        int index = aggroTrans.IndexOf(magnet);
        aggroTrans.RemoveAt(index);
        aggroAmount.RemoveAt(index);

        if (currentTarget != magnet) return;
        currentTarget = null;
        var aggroMagnet = magnet.GetComponent<AggroMagnet>();
        if (aggroMagnet != null && claimedOffset != Vector3.zero) {
            aggroMagnet.ReleaseOffset(claimedOffset);
            claimedOffset = Vector3.zero;
        }
        if (OnRemoveTarget != null) OnRemoveTarget();
    }

    void RemoveAtIndex(int index) {
        aggroTrans.RemoveAt(index);
        aggroAmount.RemoveAt(index);
    }

    void AddAggro(Transform magnet, float amount) {
        if (MaxAggroCount <= aggroTrans.Count) {
            Transform lowest = aggroTrans[LowestAggroIndex()];
            RemoveAggro(lowest);
        }
        aggroTrans.Add(magnet);
        aggroAmount.Add(amount);
    }

    int HighestAggroIndex() {
        int highestIndex = 0;
        for (int i = aggroAmount.Count - 1; i >= 0; i--) {
            if (i == highestIndex) continue;
            if (aggroAmount[i] > aggroAmount[highestIndex]) highestIndex = i;
        }
        return highestIndex;
    }

    int LowestAggroIndex() {
        int lowestIndex = 0;
        for (int i = aggroAmount.Count - 1; i >= 0; i--) {
            if (i == lowestIndex) continue;
            if (aggroAmount[i] < aggroAmount[lowestIndex]) lowestIndex = i;
        }
        return lowestIndex;
    }
}
