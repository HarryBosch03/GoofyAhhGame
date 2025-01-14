using System;
using Runtime.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Player
{
    public class PlayerWeaponsManager : NetworkBehaviour
    {
        public Weapon currentWeapon;
        public Weapon[] weaponSlots = new Weapon[2];

        private PlayerController player;

        private void Awake() { player = GetComponentInParent<PlayerController>(); }

        [Rpc(SendTo.Everyone)]
        public void SwitchToWeaponSlotRpc(int index)
        {
            if (currentWeapon != null) currentWeapon.gameObject.SetActive(false);
            currentWeapon = weaponSlots[index];
            if (currentWeapon != null) currentWeapon.gameObject.SetActive(true);
        }

        [ServerRpc]
        public void PickupWeaponServerRpc(NetworkBehaviourReference newWeaponRef)
        {
            newWeaponRef.TryGet(out Weapon newWeapon);
            var slotIndex = GetCurrentWeaponSlot();

            for (var i = 0; i < weaponSlots.Length; i++)
            {
                var slot = weaponSlots[i];
                if (slot == null)
                {
                    slotIndex = i;
                    break;
                }
            }

            DropWeaponRpc(slotIndex);
            SetWeaponInSlotRpc(slotIndex, newWeapon);
            SwitchToWeaponSlotRpc(slotIndex);
        }

        [Rpc(SendTo.Everyone)]
        private void SetWeaponInSlotRpc(int slotIndex, NetworkBehaviourReference weaponRef)
        {
            weaponRef.TryGet(out Weapon weapon);
            weaponSlots[slotIndex] = weapon;
            weapon.SetPlayer(player);
        }

        [Rpc(SendTo.Everyone)]
        private void DropWeaponRpc(int slot)
        {
            if (slot >= 0)
            {
                var weapon = weaponSlots[slot];
                if (weapon != null)
                {
                    weapon.DropWeaponServerRpc(transform.position);
                    weaponSlots[slot] = null;
                }
            }
        }

        private int GetCurrentWeaponSlot()
        {
            for (var i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == currentWeapon) return i;
            }

            return -1;
        }

        public void SetShoot(bool shoot)
        {
            if (currentWeapon != null) currentWeapon.shoot = shoot;
        }

        public void SetAim(bool aim)
        {
            if (currentWeapon != null) currentWeapon.aim = aim;
        }

        public void SetReload(bool reload)
        {
            if (currentWeapon != null) currentWeapon.reload = reload;
        }
    }
}