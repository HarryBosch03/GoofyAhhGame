using UnityEngine;

namespace Runtime.Level
{
    public class DamageZone : MonoBehaviour
    {
        public Bounds bounds;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawCube(bounds.center, bounds.size);
        }
    }
}