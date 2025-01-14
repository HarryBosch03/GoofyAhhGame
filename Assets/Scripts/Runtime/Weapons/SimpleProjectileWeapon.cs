using System;
using System.Collections;
using Runtime.Damage;
using Runtime.Player;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Weapons
{
    public class SimpleProjectileWeapon : Weapon
    {
        public Projectile projectilePrefab;
        public DamageInstance damage;
        public float projectileSpeed;

        [Space]
        public float fireRate;
        public bool automatic;
        public int magazineSize;
        public float reloadDuration = 1.2f;

        [Space]
        public Transform visualMuzzle;
        public ParticleSystem flashFX;
        public ParticleSystem hitFX;
        
        private int currentMagazine;
        public PlayerController player { get; private set; }

        public bool isReloading { get; set; }
        public float reloadPercent { get; set; }
        public float cycleTimer { get; private set; }

        public event Action ShootEvent;

        private void Awake()
        {
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

        private void OnEnable()
        {
            player = GetComponentInParent<PlayerController>();
            
            if (IsOwner && isReloading)
            {
                isReloading = false;
                StartCoroutine(Reload());
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                if (shoot && cycleTimer <= 0f)
                {
                    if (currentMagazine > 0)
                    {
                        ShootRpc(player.motor.headPosition + player.motor.headRotation * Vector3.forward * player.motor.radius * 1.5f, player.motor.headRotation);
                        cycleTimer = 60f / fireRate;
                    }
                    else
                    {
                        ReloadRpc();
                    }

                    if (!automatic) shoot = false;
                }

                if (reload && currentMagazine < magazineSize)
                {
                    ReloadRpc();
                }

                cycleTimer -= Time.fixedDeltaTime;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ReloadRpc()
        {
            StartCoroutine(Reload());
        }
        
        private IEnumerator Reload()
        {
            if (isReloading) yield break;
            isReloading = true;
            currentMagazine = 0;
            reload = false;

            reloadPercent = 0f;
            while (reloadPercent < 1f)
            {
                reloadPercent += Time.deltaTime / reloadDuration;
                yield return null;
            }
            reloadPercent = 0f;
            
            currentMagazine = magazineSize;
            isReloading = false;
        }

        [Rpc(SendTo.Everyone)]
        private void ShootRpc(Vector3 position, Quaternion orientation)
        {
            var projectile = Instantiate(projectilePrefab, position, orientation);
            projectile.damage = damage;
            projectile.velocity = player.motor.velocity + orientation * Vector3.forward * projectileSpeed;
            if (visualMuzzle != null) projectile.interpolatePosition = visualMuzzle.position;
            projectile.HitEvent += OnProjectileHit;
            if (flashFX != null) flashFX.Play();

            currentMagazine--;

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