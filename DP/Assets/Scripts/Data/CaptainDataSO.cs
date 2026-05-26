using UnityEngine;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "CaptainData", menuName = "ConvoyManager/Captain Data")]
    public class CaptainDataSO : ScriptableObject
    {
        public int ID;
        public string Name;
        public Rarity Rarity;
        public int AttackBonus;
        public int DefenseBonus;
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}