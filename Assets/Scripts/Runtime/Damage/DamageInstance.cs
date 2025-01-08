using Unity.Netcode;

namespace Runtime.Damage
{
    [System.Serializable]
    public struct DamageInstance : INetworkSerializeByMemcpy
    {
        public int damage;
    }
}