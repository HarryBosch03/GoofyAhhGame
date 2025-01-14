using System;
using Runtime.Level;
using Runtime.Player;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Weapons
{
    public class WeaponPickup : NetworkBehaviour, ICanInteract
    {
        public Weapon weapon;

        [Space]
        public float hoverHeight;
        public float bobAmplitude;
        public float bobFrequency;
        public float rotationSpeed;
        
        private string displayName;

        public void ChangeWeaponRpc(Weapon weapon)
        {
            weapon.transform.SetParent(transform);
            weapon.gameObject.SetActive(true);
            weapon.enabled = false;
        }

        private void Awake()
        {
            ChangeWeaponRpc(weapon);
        }

        private void Update()
        {
            if (weapon != null)
            {
                weapon.model.transform.localPosition = Vector3.up * (hoverHeight + Mathf.Sin(Time.time * Mathf.PI * bobFrequency) * bobAmplitude);
                weapon.model.transform.localRotation = Quaternion.Euler(-45f, Time.time * rotationSpeed, 0f);;
            }
        }

        public string GetInteractString(PlayerController player) => $"Pickup {displayName}";

        public void Interact(PlayerController player)
        {
            player.weaponsManager.PickupWeaponServerRpc(this);
        }
    }
}