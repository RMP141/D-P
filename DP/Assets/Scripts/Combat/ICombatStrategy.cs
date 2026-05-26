using ConvoyManager.Utils;

namespace ConvoyManager.Combat
{
    public interface ICombatStrategy
    {
        CombatResult Resolve(int mercenaryCount, int captainBonus, float enemyPower, IRandomGenerator random);
    }
}