using System;
using PurrNet;
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

        [Space]
        public Transform leftHandIkTarget;
        public Transform rightHandIkTarget;
        private int currentMagazine;
        public PlayerController player { get; private set; }
        
        public bool shoot { get; set; }
        public bool aim { get; set; }
        public bool reload { get; set; }
        public float cycleTimer { get; private set; }

        public event Action ShootEvent;

        private void Awake()
        {
            player = GetComponentInParent<PlayerController>();

            var main = flashFX.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = player.head;
            main.stopAction = ParticleSystemStopAction.None;

            main = hitFX.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
        }

        private void FixedUpdate()
        {
            if (shoot && cycleTimer <= 0f)
            {
                Shoot(player.motor.headPosition + player.motor.headRotation * Vector3.forward * player.motor.radius * 1.5f, player.motor.headRotation);

                cycleTimer = 60f / fireRate;
            }

            cycleTimer -= Time.fixedDeltaTime;
        }

        [ObserversRpc(runLocally:true)]
        private void Shoot(Vector3 position, Quaternion orientation)
        {
            var projectile = Instantiate(projectilePrefab, position, orientation);
            projectile.velocity = player.motor.velocity + orientation * Vector3.forward * projectileSpeed;
            if (visualMuzzle != null) projectile.interpolatePosition = visualMuzzle.position;
            projectile.HitEvent += OnProjectileHit;
            if (flashFX != null) flashFX.Play();

            ShootEvent?.Invoke();
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
    }
}