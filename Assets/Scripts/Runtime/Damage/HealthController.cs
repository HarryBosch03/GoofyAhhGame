using System;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Damage
{
    public class HealthController : NetworkBehaviour
    {
        public int maxHealth;
        public int currentHealth;
        
        public bool isDead { get; private set; }
        
        private void Awake() { currentHealth = maxHealth; }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SetHealthRpc(currentHealth);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SetHealthRpc(int currentHealth) { this.currentHealth = currentHealth; }

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