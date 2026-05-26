using ConvoyManager.Utils;

namespace ConvoyManager.Combat
{
    public class DefaultCombatCalculator : ICombatStrategy
    {
        public CombatResult Resolve(int mercenaryCount, int captainBonus, float enemyPower, IRandomGenerator random)
        {
            float attack = (mercenaryCount * 2 + captainBonus) * random.Range(0.8f, 1.2f);
            float defense = enemyPower * random.Range(0.8f, 1.2f);
            return attack > defense ? CombatResult.Victory : CombatResult.Defeat;
        }
    }
}