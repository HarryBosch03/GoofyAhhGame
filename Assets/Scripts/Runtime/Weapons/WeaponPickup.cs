using System;
using Runtime.Level;
using Runtime.Player;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Weapons
{
    public class WeaponPickup : NetworkBehaviour, ICanInteract
    {
        public Weapon defaultWeapon;

        [Space]
        public float hoverHeight;
        public float bobAmplitude;
        public float bobFrequency;
        public float rotationSpeed;
        
        private string weaponId;
        private GameObject modelInstance;
        private string displayName;

        [Rpc(SendTo.Everyone)]
        public void ChangeWeaponRpc(string weaponId)
        {
            if (modelInstance != null) Destroy(modelInstance);

            var all = Resources.LoadAll("Weapons");
            var weapon = Resources.Load<GameObject>($"Weapons/{weaponId}").GetComponent<Weapon>();
            
            modelInstance = Instantiate(weapon.model, transform);
            displayName = weapon.name;
            name = $"Weapon Pickup - {displayName}";
            this.weaponId = weaponId;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ChangeWeaponRpc(defaultWeapon.id);
            }
        }

        private void Update()
        {
            if (modelInstance != null)
            {
                modelInstance.transform.localPosition = Vector3.up * (hoverHeight + Mathf.Sin(Time.time * Mathf.PI * bobFrequency) * bobAmplitude);
                modelInstance.transform.localRotation = Quaternion.Euler(-45f, Time.time * rotationSpeed, 0f);;
            }
        }

        public string GetInteractString(PlayerController player) => $"Pickup {displayName}";

        public void Interact(PlayerController player)
        {
            player.weaponsManager.PickupWeaponServerRpc(weaponId);
            if (IsServer) SetActiveRpc(false);
        }

        [Rpc(SendTo.Everyone)]
        private void SetActiveRpc(bool enabled)
        {
            gameObject.SetActive(enabled);
        }
    }
}