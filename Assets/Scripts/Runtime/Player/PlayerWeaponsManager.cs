using System;
using FishNet.Object;
using Runtime.Weapons;
using UnityEngine;

namespace Runtime
{
    public class PlayerWeaponsManager : NetworkBehaviour
    {
        public Transform weaponsParent;
        public SimpleProjectileWeapon currentWeapon;
        
        public bool shoot { get; set; }
        public bool aim { get; set; }
        public bool reload { get; set; }

        private void Awake()
        {
            currentWeapon = weaponsParent.GetComponentInChildren<SimpleProjectileWeapon>();
        }

        private void Update()
        {
            if (currentWeapon != null)
            {
                currentWeapon.shoot = shoot;
                currentWeapon.aim = aim;
                currentWeapon.reload = reload;
            }
        }
    }
}