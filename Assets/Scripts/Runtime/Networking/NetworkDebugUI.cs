using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Networking
{
    public class NetworkDebugUI : MonoBehaviour
    {
        private NetworkManager netManager;

        private void Awake() { netManager = GetComponent<NetworkManager>(); }

        private void Update()
        {
            var kb = Keyboard.current;
            if (!netManager.IsServer && !netManager.IsClient)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) StartHost();
                if (kb.cKey.wasPressedThisFrame) StartClient();
            }
        }

        private void StartHost()
        {
            netManager.StartHost();
        }

        private void StartClient()
        {
            netManager.StartClient();
        }

        private void OnGUI()
        {
            if (!netManager.IsServer && !netManager.IsClient)
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 200, Screen.height - 40f)))
                {
                    if (GUILayout.Button("Start Host")) StartHost();
                    if (GUILayout.Button("Start Client")) StartClient();
                }
            }
        }
    }
}