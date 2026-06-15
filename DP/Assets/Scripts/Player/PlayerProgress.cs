using System.Collections.Generic;
using ConvoyManager.World;
using UnityEngine;

namespace ConvoyManager.Player
{
    public class PlayerProgress : IPlayerProgress
    {
        public int Gold { get; private set; } = 1000;
        public int MercenaryCount { get; private set; } = 2;
        public int CartCount { get; private set; } = 0;
        public int MaxConvoys { get; private set; } = 3;

        public Dictionary<Faction, int> Reputation { get; private set; } = new Dictionary<Faction, int>();
        public Dictionary<int, int> Inventory { get; private set; } = new Dictionary<int, int>();

        public void AddGold(int amount) => Gold += amount;

        public bool SpendGold(int amount)
        {
            if (Gold >= amount)
            {
                Gold -= amount;
                return true;
            }
            return false;
        }

        public void AddMercenaries(int count)
        {
            MercenaryCount = Mathf.Min(MercenaryCount + count, 50);
        }

        public void RemoveMercenaries(int count)
        {
            MercenaryCount = Mathf.Max(MercenaryCount - count, 0);
        }

        public void AddCarts(int count)
        {
            CartCount += count;
        }

        public void RemoveCarts(int count)
        {
            CartCount = Mathf.Max(CartCount - count, 0);
        }

        public void AddItem(int itemId, int quantity)
        {
            if (Inventory.ContainsKey(itemId))
                Inventory[itemId] += quantity;
            else
                Inventory[itemId] = quantity;
        }

        public void RemoveItem(int itemId, int quantity)
        {
            if (Inventory.TryGetValue(itemId, out int current))
            {
                int newQty = current - quantity;
                if (newQty <= 0)
                    Inventory.Remove(itemId);
                else
                    Inventory[itemId] = newQty;
            }
        }

        public void ChangeReputation(Faction faction, int delta)
        {
            if (!Reputation.ContainsKey(faction))
                Reputation[faction] = 0;
            Reputation[faction] = Mathf.Clamp(Reputation[faction] + delta, -100, 100);
        }

        public void SetMaxConvoys(int count) => MaxConvoys = count;

        /// <summary>
        /// ��������������� ��������� ��������� �� ����������.
        /// </summary>
        public void Restore(int gold, int mercenaryCount, int cartCount, int maxConvoys,
            Dictionary<Faction, int> reputation, Dictionary<int, int> inventory)
        {
            Gold = gold;
            MercenaryCount = mercenaryCount;
            CartCount = cartCount;
            MaxConvoys = maxConvoys;
            Reputation = new Dictionary<Faction, int>(reputation);
            Inventory = new Dictionary<int, int>(inventory);
        }
    }
}