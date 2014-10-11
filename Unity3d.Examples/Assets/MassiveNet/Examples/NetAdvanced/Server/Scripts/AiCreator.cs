// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using MassiveNet;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {
    public class AiCreator : MonoBehaviour {

        public AiManager AiManager;
        private AggroChaser chaser;
        private AttackChecker attackChecker;
        private NetView view;
        
        private Vector3 velocity;

        private Vector3 randTarget;
        private Vector3 randTargetRoot;

        private const float NormalSpeed = 3f;
        private const float ChaseSpeed = 5f;
        private const float AccelRate = 6f;

        private float currentSpeed = 4f;

        private const int MaxHealth = 100;
        private int currentHealth = 100;

        private Transform aggroTarget;
        private Vector3 aggroTargetOffset;

        private bool Dead { get { return currentHealth <= 0; } }

        void Awake() {
            chaser = GetComponent<AggroChaser>();
            view = GetComponent<NetView>();
            attackChecker = GetComponent<AttackChecker>();
        }

        void Start() {
            chaser.OnNewTarget += TargetAcquired;
            chaser.OnRemoveTarget += TargetLost;

            attackChecker.OnTakeDamage += TakePlayerDamage;

            view.OnWriteSync += WriteSync;
            view.OnWriteProxyData += WriteProxyData;

            SetTargetPosition();
        }

        void TakePlayerDamage(NetView attackerView, int damage) {
            if (Dead) return;
            currentHealth -= damage;
            if (Dead) Die();
        }

        void Die() {
            AiManager.AiDied(this);
            Invoke("Despawn", 30);
            view.SendReliable("Die", RpcTarget.NonControllers);
            gameObject.SetActive(false);
        }

        public void Spawn(Vector3 spawnPos) {
            transform.position = spawnPos;
            gameObject.SetActive(true);
        }

        void Despawn() {
            AiManager.AiDespawned(this);
            gameObject.SetActive(false);
            view.Scope.DisableScopeCalculation();
        }

        void TargetAcquired(Transform magnet, Vector3 targetOffset) {
            aggroTarget = magnet;
            currentSpeed = ChaseSpeed;
            aggroTargetOffset = targetOffset + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            //LocalAvoidance.Instance.RegisterGoalFunc(GenGoal);
        }

        private LocalAvoidance.LAState laState = default(LocalAvoidance.LAState);
        public LocalAvoidance.LAState GenGoal() {
            laState.CurrPos = transform.position;
            laState.GoalPos = aggroTarget.position;
            laState.GoalId = aggroTarget.GetInstanceID();
            laState.ResultCallback = SetAvoidanceResult;
            laState.Speed = currentSpeed;
            return laState;
        }

        public void SetAvoidanceResult(Vector3 result) {
            transform.position = result;
        }

        void TargetLost() {
            aggroTarget = null;
            currentSpeed = NormalSpeed;
            //LocalAvoidance.Instance.DeregisterGoalFunc(GenGoal);
        }

        RpcTarget WriteSync(NetStream syncStream) {
            syncStream.WriteFloat(transform.position.x);
            syncStream.WriteFloat(transform.position.z);
            syncStream.WriteVector2(new Vector2(velocity.x, velocity.z));
            return RpcTarget.NonControllers;
        }

        void OnEnable() {
            currentHealth = MaxHealth;
            SetTargetRoot(transform.position);
            view.EnableSync();
            view.Scope.EnableScopeCalculation();
        }

        void OnDisable() {
            view.DisableSync();
            TargetLost();
        }

        void OnDestroy() {
            OnDisable();
        }

        void Update() {
            Vector3 oldPos = transform.position;
            Vector3 currTarget = aggroTarget != null ? aggroTarget.position + (aggroTargetOffset * Mathf.Clamp(Vector3.Distance(transform.position, aggroTarget.position) * 0.2f, 1.3f, 7)) : randTarget;
            float distance = Vector3.Distance(transform.position, currTarget);
            if (distance < 0.1) {
                if (aggroTarget == null) SetTargetPosition();
                velocity = transform.position - oldPos;
                return;
            }
            float targetSpeed = aggroTarget != null ? ChaseSpeed : NormalSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime*AccelRate);
            transform.position = Vector3.MoveTowards(transform.position, currTarget, Time.deltaTime * currentSpeed);
            velocity = transform.position - oldPos;
        }

        void SetTargetPosition() {
            randTarget = new Vector3(Random.Range(-200, 200) + randTargetRoot.x, 0, Random.Range(-200, 200) + randTargetRoot.z);
        }

        public void SetTargetRoot(Vector3 rootPos) {
            randTargetRoot = rootPos;
        }

        private void WriteProxyData(NetStream stream) {
            stream.WriteBool(Dead);
            stream.WriteFloat(transform.position.x);
            stream.WriteFloat(transform.position.z);
        }
    }
}
