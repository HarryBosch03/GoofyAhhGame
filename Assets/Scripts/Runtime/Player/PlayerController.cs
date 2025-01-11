using System;
using Runtime.Damage;
using Runtime.Level;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runtime.Player
{
    public class PlayerController : NetworkBehaviour
    {
        public float mouseSensitivity = 0.3f;
        public float interactMaxRange = 2f;
        
        [Space]
        public Transform head;
        public GameObject[] firstPersonOnly;
        public GameObject[] thirdPersonOnly;

        [Space]
        public Canvas respawnCanvas;
        public Button respawnButton;

        private Camera mainCamera;
        private ICanInteract lookingAt;

        public PlayerMotor motor { get; private set; }
        public PlayerWeaponsManager weaponsManager { get; private set; }
        public HealthController health { get; private set; }

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
            motor = GetComponentInChildren<PlayerMotor>();
            weaponsManager = GetComponentInChildren<PlayerWeaponsManager>();
            health = GetComponentInChildren<HealthController>();
            
            mainCamera = Camera.main;
        }

        private void Start()
        {
            OnActiveViewerChanged();
            respawnButton.onClick.AddListener(health.RequestRespawn);
        }

        private void OnEnable()
        {
            health.DiedEvent += OnDied;
            health.RespawnedEvent += OnRespawn;
        }

        private void OnDisable()
        {
            health.DiedEvent -= OnDied;
            health.RespawnedEvent -= OnRespawn;
        }

        private void OnRespawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                respawnCanvas.gameObject.SetActive(false);
            }
        }

        private void OnDied()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                respawnCanvas.gameObject.SetActive(true);
            }
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
            else
            {
                respawnCanvas.gameObject.SetActive(false);
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

                motor.moveDirection = motor.transform.TransformVector(kb.dKey.ReadValue() - kb.aKey.ReadValue(), 0f, kb.wKey.ReadValue() - kb.sKey.ReadValue());
                motor.rotation += m.delta.ReadValue() * mouseSensitivity;

                if (kb.spaceKey.wasPressedThisFrame) motor.jump = true;
                if (kb.spaceKey.wasReleasedThisFrame) motor.jump = false;

                if (m.leftButton.wasPressedThisFrame) weaponsManager.SetShoot(true);
                if (m.leftButton.wasReleasedThisFrame) weaponsManager.SetShoot(false);

                if (m.rightButton.wasPressedThisFrame) weaponsManager.SetAim(true);
                if (m.rightButton.wasReleasedThisFrame) weaponsManager.SetAim(false);

                if (kb.rKey.wasPressedThisFrame) weaponsManager.SetReload(true);
                if (kb.rKey.wasReleasedThisFrame) weaponsManager.SetReload(false);
                
                if (kb.leftCtrlKey.wasPressedThisFrame) motor.crouching = true;
                if (kb.leftCtrlKey.wasReleasedThisFrame) motor.crouching = false;

                if (kb.digit1Key.wasPressedThisFrame) weaponsManager.SwitchToWeaponSlotRpc(0);
                if (kb.digit2Key.wasPressedThisFrame) weaponsManager.SwitchToWeaponSlotRpc(1);

                var ray = new Ray(motor.headPosition, motor.headRotation * Vector3.forward);
                if (Physics.Raycast(ray, out var hit, interactMaxRange))
                {
                    lookingAt = hit.collider.GetComponentInParent<ICanInteract>();
                    if (lookingAt != null && kb.eKey.wasPressedThisFrame)
                    {
                        lookingAt.Interact(this);
                    }
                }
                else
                {
                    lookingAt = null;
                }
            }
        }

        private void OnGUI()
        {
            if (lookingAt != null)
            {
                GUI.Label(new Rect(Screen.width / 2f + 50f, Screen.height / 2f - 10f, 200f, 20f), lookingAt.GetInteractString(this));
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