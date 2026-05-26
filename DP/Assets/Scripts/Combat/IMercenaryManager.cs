namespace ConvoyManager.Combat
{
    public interface IMercenaryManager
    {
        int MercenaryCount { get; }
        bool Hire();
        bool Fire();
        int GetAttackBonus();
    }
}