using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Runtime.Player;
using UnityEngine;

namespace Runtime.Weapons
{
    public class SimpleProjectileWeapon : NetworkBehaviour
    {
        public Projectile projectilePrefab;
        public float projectileSpeed;

        [Space]
        public float fireRate;
        public bool automatic;
        public int magazineSize;

        [Space]
        public Transform visualMuzzle;
        public ParticleSystem flashFX;
        public ParticleSystem hitFX;

        private int currentMagazine;
        public PlayerMotor motor { get; private set; }

        public bool shoot { get; set; }
        public bool aim { get; set; }
        public bool reload { get; set; }
        public float cycleTimer { get; private set; }

        public event Action ShootEvent;

        private void Awake()
        {
            motor = GetComponentInParent<PlayerMotor>();

            var main = flashFX.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;

            main = hitFX.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
        }

        public override void OnStartNetwork() { TimeManager.OnTick += OnTick; }

        public override void OnStopNetwork() { TimeManager.OnTick -= OnTick; }

        private void OnTick()
        {
            RunInputs(GetInputData());
            CreateReconcile();
        }

        private InputData GetInputData()
        {
            if (!IsOwner) return default;

            var data = new InputData();
            data.shoot = shoot;
            data.aim = aim;
            data.reload = reload;

            if (!automatic) shoot = false;

            return data;
        }

        [Replicate]
        private void RunInputs(InputData input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (state.IsFuture()) return;

            if (input.shoot && cycleTimer <= 0f)
            {
                if (state == ReplicateState.CurrentCreated)
                {
                    var projectile = Instantiate(projectilePrefab, motor.rawHeadPosition + motor.headRotation * Vector3.forward * motor.radius * 1.5f, motor.headRotation);
                    projectile.velocity = motor.body.linearVelocity + motor.headRotation * Vector3.forward * projectileSpeed;
                    if (visualMuzzle != null) projectile.interpolatePosition = visualMuzzle.position;
                    projectile.HitEvent += OnProjectileHit;
                    if (flashFX != null) flashFX.Play();

                    ShootEvent?.Invoke();
                }

                cycleTimer = 60f / fireRate;
            }

            cycleTimer -= Time.fixedDeltaTime;
        }

        private void OnProjectileHit(Projectile projectile, RaycastHit hit)
        {
            if (hitFX != null)
            {
                hitFX.transform.position = hit.point;
                hitFX.transform.rotation = Quaternion.LookRotation(hit.normal, projectile.velocity);
                hitFX.Play();
            }
        }

        public override void CreateReconcile()
        {
            var data = new StateData();
            data.cycleTimer = cycleTimer;
            data.currentMagazine = currentMagazine;

            Reconcile(data);
        }

        [Reconcile]
        private void Reconcile(StateData data, Channel channel = Channel.Unreliable)
        {
            cycleTimer = data.cycleTimer;
            currentMagazine = data.currentMagazine;
        }

        protected override void OnValidate() { }

        public struct InputData : IReplicateData
        {
            public bool shoot;
            public bool aim;
            public bool reload;

            private uint tick;
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }

        public struct StateData : IReconcileData
        {
            public int currentMagazine;
            public float cycleTimer;

            private uint tick;
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
            public void Dispose() { }
        }
    }
}