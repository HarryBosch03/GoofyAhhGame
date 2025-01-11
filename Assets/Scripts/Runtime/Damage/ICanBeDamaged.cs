namespace Runtime.Damage
{
    public interface ICanBeDamaged
    {
        public bool isDead { get; }
        void Damage(DamageInstance damage, DamageSource source);
    }
}