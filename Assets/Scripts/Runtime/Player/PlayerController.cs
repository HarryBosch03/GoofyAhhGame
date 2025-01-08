using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerWeaponsManager))]
    public class PlayerController : NetworkBehaviour
    {
        public float mouseSensitivity = 0.3f;

        [Space]
        public Transform head;
        public GameObject[] firstPersonOnly;
        public GameObject[] thirdPersonOnly;

        private Camera mainCamera;

        public PlayerMotor motor { get; private set; }
        public PlayerWeaponsManager weaponsManager { get; private set; }

        public bool isActiveViewer => activeViewer == this;
        public static PlayerController activeViewer { get; private set; }

        public static void SetActiveViewer(PlayerController activeViewer)
        {
            var prev = PlayerController.activeViewer;
            if (prev == activeViewer) return;
            PlayerController.activeViewer = activeViewer;

            if (prev != null) prev.OnActiveViewerChanged();
            if (activeViewer != null) activeViewer.OnActiveViewerChanged();
        }

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            weaponsManager = GetComponent<PlayerWeaponsManager>();
            mainCamera = Camera.main;
        }

        private void OnActiveViewerChanged()
        {
            foreach (var e in firstPersonOnly) e.SetActive(isActiveViewer);
            foreach (var e in thirdPersonOnly) e.SetActive(!isActiveViewer);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                SetActiveViewer(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner) Cursor.lockState = CursorLockMode.None;
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
                if (kb.rKey.wasReleasedThisFrame) weaponsManager.reload = false;
            }
        }

        private void LateUpdate()
        {
            if (isActiveViewer)
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