using Unity.Netcode;
using UnityEngine;

namespace Runtime.Damage
{
    public struct DamageSource : INetworkSerializeByMemcpy
    {
        public Vector3 direction;
        public Vector3 point;
        public Vector3 normal;

        public DamageSource(Vector3 direction, RaycastHit hit)
        {
            this.direction = direction;
            point = hit.point;
            normal = hit.normal;
        }
    }
}