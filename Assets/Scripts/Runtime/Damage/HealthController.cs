using System;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Damage
{
    public class HealthController : NetworkBehaviour, ICanBeDamaged
    {
        public int maxHealth;
        public int currentHealth;
        
        public bool isDead { get; private set; }

        public event Action DiedEvent;
        public event Action RespawnedEvent;
        
        private void Awake() { currentHealth = maxHealth; }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                RespawnRpc();
            }
        }

        public void RequestRespawn()
        {
            RequestRespawnServerRpc();
        }

        [ServerRpc]
        private void RequestRespawnServerRpc()
        {
            if (!isDead) return;
            RespawnRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void RespawnRpc()
        {
            SetIsDead(false);
            currentHealth = maxHealth;
        }

        public void Damage(DamageInstance damage, DamageSource source)
        {
            if (!IsServer) return;
            
            currentHealth -= damage.damage;
            if (currentHealth <= 0) SetIsDead(true);
            
            NotifyDamageRpc(damage, source, currentHealth, isDead);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifyDamageRpc(DamageInstance damage, DamageSource source, int currentHealth, bool isDead)
        {
            this.currentHealth = currentHealth;
            if (isDead != this.isDead) SetIsDead(isDead);
        }

        private void SetIsDead(bool isDead)
        {
            this.isDead = isDead;
            gameObject.SetActive(!isDead);
            
            if (isDead) DiedEvent?.Invoke();
            else RespawnedEvent?.Invoke();
        }

        private void OnGUI()
        {
            if (IsOwner)
            {
                GUI.Label(new Rect(20, 20, 200, 20), $"Health: {currentHealth}/{maxHealth}");
            }
        }
    }
}