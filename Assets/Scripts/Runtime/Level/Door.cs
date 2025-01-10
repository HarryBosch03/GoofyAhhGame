using Runtime.Player;
using Unity.Netcode;
using UnityEngine;

namespace Runtime.Level
{
    public class Door : NetworkBehaviour, ICanInteract
    {
        public Transform rotor;
        public Vector3 openRotation;
        public Vector3 closedRotation;
        public float openTime;
        public AnimationCurve openCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        private float openPercent;
        
        public bool isOpen { get; private set; }

        private void Update()
        {
            openPercent = Mathf.MoveTowards(openPercent, isOpen ? 1f : 0f, Time.deltaTime / openTime);
            rotor.localRotation = Quaternion.SlerpUnclamped(Quaternion.Euler(closedRotation), Quaternion.Euler(openRotation), openCurve.Evaluate(openPercent));
        }

        public string GetInteractString(PlayerController player) => $"{(!isOpen ? "Open" : "Close")} Door";

        public void Interact(PlayerController player)
        {
            SetIsOpenRpc(!isOpen);
        }
        
        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void SetIsOpenRpc(bool isOpen)
        {
            this.isOpen = isOpen;
        }
    }
}