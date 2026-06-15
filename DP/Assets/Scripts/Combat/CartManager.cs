using ConvoyManager.Data;
using ConvoyManager.Player;

namespace ConvoyManager.Combat
{
    public class CartManager : ICartManager
    {
        private readonly IPlayerProgress _playerProgress;
        private readonly GameConfig _config;
        private int _inUseCarts;

        public CartManager(IPlayerProgress playerProgress, GameConfig config)
        {
            _playerProgress = playerProgress;
            _config = config;
        }

        public int CartCount => _playerProgress.CartCount;
        public int AvailableCarts => _playerProgress.CartCount - _inUseCarts;

        public bool BuyCart()
        {
            if (_playerProgress.CartCount >= _config.MaxCarts) return false;
            int cost = 100;
            if (!_playerProgress.SpendGold(cost)) return false;
            _playerProgress.AddCarts(1);
            return true;
        }

        public bool UseCart()
        {
            if (AvailableCarts <= 0) return false;
            _inUseCarts++;
            return true;
        }

        public void ReturnCart()
        {
            _inUseCarts = System.Math.Max(0, _inUseCarts - 1);
        }
    }
}
