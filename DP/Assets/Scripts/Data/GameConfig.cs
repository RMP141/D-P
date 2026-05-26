using UnityEngine;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ConvoyManager/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player Start")]
        public int StartGold = 1000;
        public int StartCarts = 1;
        public int StartMercenaries = 2;
        public int MaxConvoys = 1;
        public int MaxCarts = 10;
        public int MaxMercenaries = 50;

        [Header("Economy")]
        [Range(0f, 1f)] public float PriceRecoveryRate = 0.07f;
        public float ReputationPriceDiscount = 0.2f;
        public float ReputationPricePenalty = 0.2f;

        [Header("Combat")]
        public int MercenaryAttackPerUnit = 2;
        public int CaptainGachaCost = 500;
        public float CaptainGachaCommonWeight = 50f;
        public float CaptainGachaRareWeight = 30f;
        public float CaptainGachaEpicWeight = 15f;
        public float CaptainGachaLegendaryWeight = 5f;

        [Header("Events")]
        public float EventCheckInterval = 60f;
        [Range(0f, 1f)] public float EventProbability = 0.3f;

        [Header("ECS Convoy")]
        public float FoodConsumptionPerSec = 0.5f;
        public float WearRatePerSec = 0.1f;
        public float DefaultSpeed = 1f;
    }
}