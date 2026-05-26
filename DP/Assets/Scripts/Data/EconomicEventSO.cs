using UnityEngine;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "EconomicEvent", menuName = "ConvoyManager/Economic Event")]
    public class EconomicEventSO : ScriptableObject
    {
        public string Title;
        public string Description;
        public float DurationDays = 3f;
        public float PriceMultiplier = 1.2f;
        public ItemCategory[] AffectedCategories;
    }
}