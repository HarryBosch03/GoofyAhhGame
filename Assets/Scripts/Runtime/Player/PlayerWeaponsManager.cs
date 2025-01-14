using Runtime.Weapons;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Player
{
    public class PlayerWeaponsManager : NetworkBehaviour
    {
        public Transform weaponParent;
        public Weapon currentWeapon;
        public Weapon[] weaponSlots = new Weapon[2];

        [Rpc(SendTo.Everyone)]
        public void SwitchToWeaponSlotRpc(int index)
        {
            if (currentWeapon != null) currentWeapon.gameObject.SetActive(false);
            currentWeapon = weaponSlots[index];
            if (currentWeapon != null) currentWeapon.gameObject.SetActive(true);
        }
        
        [ServerRpc]
        public void PickupWeaponServerRpc(WeaponPickup pickup)
        {
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

            var oldWeapon = weaponSlots[slotIndex];
            var newWeapon = pickup.weapon;

            SetWeaponInSlotRpc(slotIndex, newWeapon);
            pickup.ChangeWeaponRpc(oldWeapon);

        }

        [Rpc(SendTo.Everyone)]
        private void SetWeaponInSlotRpc(int slotIndex, Weapon weapon)
        {
            weaponSlots[slotIndex] = weapon;
            weapon.transform.SetParent(weaponParent);
            weapon.enabled = true;
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
                    weapon.gameObject.SetActive(false);
                }
                weaponSlots[slot] = null;
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