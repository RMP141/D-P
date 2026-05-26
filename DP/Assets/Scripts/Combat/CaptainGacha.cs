using ConvoyManager.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ConvoyManager.Combat
{
    public class CaptainGacha
    {
        private readonly GameConfig _config;
        private readonly CaptainDataSO[] _pool;

        public CaptainGacha(GameConfig config)
        {
            _config = config;
            _pool = Resources.LoadAll<CaptainDataSO>("Captains");
        }

        /// <summary>
        /// Выполняет Gacha-найм. Возвращает капитана согласно весам редкости.
        /// </summary>
        public CaptainDataSO Pull()
        {
            if (_pool.Length == 0) return null;

            float totalWeight = _config.CaptainGachaCommonWeight + _config.CaptainGachaRareWeight +
                                _config.CaptainGachaEpicWeight + _config.CaptainGachaLegendaryWeight;
            float roll = Random.Range(0f, totalWeight);

            Rarity targetRarity;
            if (roll < _config.CaptainGachaCommonWeight)
                targetRarity = Rarity.Common;
            else if (roll < _config.CaptainGachaCommonWeight + _config.CaptainGachaRareWeight)
                targetRarity = Rarity.Rare;
            else if (roll < _config.CaptainGachaCommonWeight + _config.CaptainGachaRareWeight + _config.CaptainGachaEpicWeight)
                targetRarity = Rarity.Epic;
            else
                targetRarity = Rarity.Legendary;

            var candidates = System.Array.FindAll(_pool, c => c.Rarity == targetRarity);
            if (candidates.Length == 0)
                candidates = _pool; // fallback
            return candidates[Random.Range(0, candidates.Length)];
        }

        /// <summary>
        /// Возвращает капитана по ID (для восстановления из сохранения).
        /// </summary>
        public CaptainDataSO GetCaptainByID(int id) => System.Array.Find(_pool, c => c.ID == id);
    }
}