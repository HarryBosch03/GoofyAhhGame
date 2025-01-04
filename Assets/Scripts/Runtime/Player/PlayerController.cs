using System;
using FishNet.Object;
using Runtime.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerWeaponsManager))]
    public class PlayerController : NetworkBehaviour
    {
        public float mouseSensitivity = 0.3f;

        [Space]
        public Transform head;
        public GameObject[] ownerOnly;
        public GameObject[] observerOnly;

        private PlayerMotor motor;
        private PlayerWeaponsManager weaponsManager;
        private Camera mainCamera;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            weaponsManager = GetComponent<PlayerWeaponsManager>();
            mainCamera = Camera.main;
        }

        public override void OnStartNetwork()
        {
            var isOwner = Owner == LocalConnection;
            
            if (isOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            foreach (var e in ownerOnly) e.SetActive(isOwner);
            foreach (var e in observerOnly) e.SetActive(!isOwner);
        }


        public override void OnStopNetwork()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                var kb = Keyboard.current;
                var m = Mouse.current;

                motor.moveDirection = transform.TransformVector(kb.dKey.ReadValue() - kb.aKey.ReadValue(), 0f, kb.wKey.ReadValue() - kb.sKey.ReadValue());
                motor.rotation += m.delta.ReadValue() * mouseSensitivity;

                if (kb.spaceKey.wasPressedThisFrame) motor.jump = true;
                if (kb.spaceKey.wasReleasedThisFrame) motor.jump = false;

                if (m.leftButton.wasPressedThisFrame) weaponsManager.shoot = true;
                if (m.leftButton.wasReleasedThisFrame) weaponsManager.shoot = false;
                
                if (m.rightButton.wasPressedThisFrame) weaponsManager.aim = true;
                if (m.rightButton.wasReleasedThisFrame) weaponsManager.aim = false;
                
                if (kb.rKey.wasPressedThisFrame) weaponsManager.reload = true;
            }
        }

        private void LateUpdate()
        {
            if (IsOwner)
            {
                mainCamera.transform.position = motor.headPosition;
                mainCamera.transform.rotation = motor.headRotation;
            }

            if (head != null)
            {
                head.position = motor.headPosition;
                head.rotation = motor.headRotation;
            }
        }
    }
}