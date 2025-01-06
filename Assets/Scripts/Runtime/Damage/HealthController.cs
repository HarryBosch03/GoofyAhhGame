using System;
using PurrNet;
using UnityEngine;

namespace Runtime.Damage
{
    public class HealthController : NetworkBehaviour
    {
        public int maxHealth;
        public int currentHealth;

        public bool isDead { get; private set; }
        
        private void Awake() { currentHealth = maxHealth; }

        protected override void OnSpawned()
        {
            if (isServer)
            {
                SetHealth(currentHealth);
            }
        }

        [ObserversRpc(requireServer: true, runLocally: true)]
        private void SetHealth(int currentHealth) { this.currentHealth = currentHealth; }

        public void Damage(DamageInstance damage, DamageSource source)
        {
            currentHealth -= damage.damage;
            if (currentHealth <= 0) SetIsDead(true);
            
            NotifyDamage(damage, source, currentHealth, isDead);
        }

        [ObserversRpc(requireServer: true, runLocally: true)]
        private void NotifyDamage(DamageInstance damage, DamageSource source, int currentHealth, bool isDead)
        {
            this.currentHealth = currentHealth;
            if (isDead != this.isDead) SetIsDead(isDead);
        }

        private void SetIsDead(bool isDead) { gameObject.SetActive(false); }

        private void OnGUI()
        {
            if (isOwner)
            {
                GUI.Label(new Rect(20, 20, 200, 20), $"Health: {currentHealth}/{maxHealth}");
            }
        }
    }
}