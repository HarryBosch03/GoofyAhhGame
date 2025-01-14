using Runtime.Level;
using Runtime.Player;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Weapons
{
    [RequireComponent(typeof(SphereCollider))]
    public abstract class Weapon : NetworkBehaviour, ICanInteract
    {
        public GameObject model;

        [Space]
        public Transform leftHandIkTarget;
        public Transform rightHandIkTarget;

        private SphereCollider pickupCollider;

        public bool shoot { get; set; }
        public bool aim { get; set; }
        public bool reload { get; set; }
        public PlayerController player { get; private set; }

        public void SetPlayer(PlayerController player)
        {
            this.player = player;
            if (player != null)
            {
                pickupCollider.enabled = false;
                transform.SetParent(player.transform);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                pickupCollider.enabled = true;
                transform.SetParent(null, true);
            }
        }

        protected virtual void Awake()
        {
            pickupCollider = gameObject.GetComponent<SphereCollider>();
            pickupCollider.isTrigger = true;

            SetPlayer(null);
        }

        public string GetInteractString(PlayerController player) => $"Pickup {name}";

        [ServerRpc]
        public void DropWeaponServerRpc(Vector3 position)
        {
            player = null;
            transform.SetParent(null);
            transform.SetPositionAndRotation(position, Quaternion.identity);

            shoot = false;
            aim = false;
            reload = false;
        }

        public void Interact(PlayerController player) { player.weaponsManager.PickupWeaponServerRpc(this); }

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
            {
                var networkObject = GetComponent<NetworkObject>();
                networkObject.SyncOwnerTransformWhenParented = true;
                networkObject.SynchronizeTransform = true;
                networkObject.AutoObjectParentSync = true;
            }
        }
    }
}