using Runtime.Player;

namespace Runtime.Level
{
    public interface ICanInteract
    {
        string GetInteractString(PlayerController player);
        void Interact(PlayerController player);
    }
}