using System.Linq;
using System.Reflection;
using Unity.Netcode;

namespace Runtime
{
    public abstract class RollbackBehaviour : NetworkBehaviour
    {
        private FieldInfo[] networkedFields;
        private PropertyInfo[] networkedProperties;

        protected virtual void Awake()
        {
            networkedFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(e => e.IsDefined(typeof(NetworkedAttribute), false)).ToArray();
            networkedProperties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(e => e.IsDefined(typeof(NetworkedAttribute), false)).ToArray();
        }
        
        protected virtual void FixedUpdateNetwork() { }
    }
}