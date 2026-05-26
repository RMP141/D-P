using ConvoyManager.Data;
using ConvoyManager.Player;

namespace ConvoyManager.Combat
{
    public class MercenaryManager : IMercenaryManager
    {
        private readonly IPlayerProgress _playerProgress;
        private readonly GameConfig _config;

        public MercenaryManager(IPlayerProgress playerProgress, GameConfig config)
        {
            _playerProgress = playerProgress;
            _config = config;
        }

        public int MercenaryCount => _playerProgress.MercenaryCount;

        public bool Hire()
        {
            if (_playerProgress.MercenaryCount >= _config.MaxMercenaries)
                return false;
            if (!_playerProgress.SpendGold(50)) // стоимость найма
                return false;

            _playerProgress.AddMercenaries(1);
            return true;
        }

        public bool Fire()
        {
            if (_playerProgress.MercenaryCount <= 0)
                return false;

            _playerProgress.RemoveMercenaries(1);
            _playerProgress.AddGold(25); // возврат половины
            return true;
        }

        public int GetAttackBonus()
        {
            return _playerProgress.MercenaryCount * _config.MercenaryAttackPerUnit;
        }
    }
}