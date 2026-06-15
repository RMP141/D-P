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
        /// ��������� Gacha-����. ���������� �������� �������� ����� ��������.
        /// </summary>
        public CaptainDataSO Pull()
        {
            if (_pool.Length == 0)
                return GenerateProceduralCaptain();

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

        private static CaptainDataSO GenerateProceduralCaptain()
        {
            var captain = ScriptableObject.CreateInstance<CaptainDataSO>();
            int rarityRoll = Random.Range(0, 100);
            Rarity rarity;
            string rarityLabel;
            if (rarityRoll < 50) { rarity = Rarity.Common; rarityLabel = "the"; }
            else if (rarityRoll < 80) { rarity = Rarity.Rare; rarityLabel = "Brave"; }
            else if (rarityRoll < 95) { rarity = Rarity.Epic; rarityLabel = "Mighty"; }
            else { rarity = Rarity.Legendary; rarityLabel = "Great"; }

            string[] names = { "Aldric", "Bran", "Cedric", "Doran", "Einar", "Finn", "Gareth", "Hakon", "Ivar", "Jarl" };
            string name = names[Random.Range(0, names.Length)];

            captain.ID = Random.Range(1, 9999);
            captain.Name = $"{rarityLabel} {name}";
            captain.Rarity = rarity;
            captain.AttackBonus = rarity switch
            {
                Rarity.Common => Random.Range(1, 4),
                Rarity.Rare => Random.Range(3, 7),
                Rarity.Epic => Random.Range(6, 11),
                Rarity.Legendary => Random.Range(10, 16),
                _ => 1
            };
            captain.DefenseBonus = rarity switch
            {
                Rarity.Common => Random.Range(1, 3),
                Rarity.Rare => Random.Range(2, 5),
                Rarity.Epic => Random.Range(4, 8),
                Rarity.Legendary => Random.Range(7, 12),
                _ => 1
            };
            return captain;
        }

        /// <summary>
        /// ���������� �������� �� ID (��� �������������� �� ����������).
        /// </summary>
        public CaptainDataSO GetCaptainByID(int id) => System.Array.Find(_pool, c => c.ID == id);
    }
}