using System;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Weapons
{
    public abstract class Weapon : NetworkBehaviour
    {
        public string id;
        
        public GameObject model;
        public WeaponPickup weaponPickupPrefab;
        
        [Space]
        public Transform leftHandIkTarget;
        public Transform rightHandIkTarget;

        public bool shoot { get; set; }
        public bool aim { get; set; }
        public bool reload { get; set; }
        
        [ServerRpc]
        public void DropWeaponServerRpc(Vector3 position)
        {
            var weaponPickup = Instantiate(weaponPickupPrefab, position, Quaternion.identity);
            weaponPickup.ChangeWeaponRpc(id);
            NetworkObject.Spawn(weaponPickup);
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
            {
                id = name;
            }
        }
#endif
    }
}