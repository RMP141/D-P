namespace ConvoyManager.World
{
    public enum SettlementType
    {
        City,
        Village
    }

    public class City
    {
        public int Index;
        public int HexIndex;
        public Faction Faction;
        public string Name;
        public SettlementType Type;
        public System.Collections.Generic.List<int> AvailableItemIds = new System.Collections.Generic.List<int>();
        public System.Collections.Generic.Dictionary<int, int> Stock = new System.Collections.Generic.Dictionary<int, int>();
        public float MaxWeight;
        public System.Collections.Generic.Dictionary<int, int> PlayerCache = new System.Collections.Generic.Dictionary<int, int>();

        public void AddToPlayerCache(int itemId, int quantity)
        {
            if (PlayerCache.ContainsKey(itemId))
                PlayerCache[itemId] += quantity;
            else
                PlayerCache[itemId] = quantity;
        }

        public int RemoveFromPlayerCache(int itemId, int quantity)
        {
            if (!PlayerCache.TryGetValue(itemId, out int current))
                return 0;
            int removed = quantity < current ? quantity : current;
            int newQty = current - removed;
            if (newQty <= 0)
                PlayerCache.Remove(itemId);
            else
                PlayerCache[itemId] = newQty;
            return removed;
        }
    }
}
