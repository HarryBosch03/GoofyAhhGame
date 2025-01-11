using UnityEngine;

namespace Runtime.Rendering
{
    [RequireComponent(typeof(Camera))]
    public class MainCamera : MonoBehaviour
    {
        private Camera mainCamera;
        private Camera viewportCamera;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            viewportCamera = transform.GetChild(0).GetComponent<Camera>();
        }

        private void Start()
        {
            mainCamera.cullingMask = ~(0b1 << 3);
            viewportCamera.cullingMask = ~mainCamera.cullingMask;
        }
    }
}