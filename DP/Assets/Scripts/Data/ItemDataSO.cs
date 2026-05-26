using ConvoyManager.World;
using UnityEngine;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "ConvoyManager/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        public int ID;
        public string Name;
        public float BasePrice;
        public float Elasticity; // 0 Ц неэластичный, 1 Ц эластичный
        public float Weight;
        public ItemCategory Category;

        [Header("Regional Modifiers (per faction)")]
        public float[] RegionalModifiers = new float[11]; // 11 фракций

        public float GetRegionalModifier(Faction faction)
        {
            int index = (int)faction;
            if (index >= 0 && index < RegionalModifiers.Length)
                return RegionalModifiers[index];
            return 1f;
        }
    }
}