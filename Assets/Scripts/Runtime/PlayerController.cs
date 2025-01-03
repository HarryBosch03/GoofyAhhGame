using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime
{
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : NetworkBehaviour
    {
        public float mouseSensitivity = 0.3f;

        [Space]
        public Transform head;
        public GameObject[] ownerOnly;
        public GameObject[] observerOnly;

        private PlayerMotor motor;
        private Camera mainCamera;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            mainCamera = Camera.main;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            foreach (var e in ownerOnly) e.SetActive(IsOwner);
            foreach (var e in observerOnly) e.SetActive(!IsOwner);
        }

        public override void OnNetworkDespawn()
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