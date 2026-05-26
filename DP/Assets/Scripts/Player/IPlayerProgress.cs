using System.Collections.Generic;
using ConvoyManager.World;

namespace ConvoyManager.Player
{
    public interface IPlayerProgress
    {
        int Gold { get; }
        int MercenaryCount { get; }
        int CartCount { get; }
        int MaxConvoys { get; }
        Dictionary<Faction, int> Reputation { get; }
        Dictionary<int, int> Inventory { get; }

        void AddGold(int amount);
        bool SpendGold(int amount);
        void AddMercenaries(int count);
        void RemoveMercenaries(int count);
        void AddItem(int itemId, int quantity);
        void RemoveItem(int itemId, int quantity);
        void ChangeReputation(Faction faction, int delta);
        void SetMaxConvoys(int count);
        void Restore(int gold, int mercenaryCount, int cartCount, int maxConvoys,
            Dictionary<Faction, int> reputation, Dictionary<int, int> inventory);
    }
}