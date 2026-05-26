using ConvoyManager.Data;
using ConvoyManager.World;
using System.Collections.Generic;

namespace ConvoyManager.Economy
{
    public interface IEconomyEngine
    {
        IReadOnlyList<ItemDataSO> AllItems { get; }
        float GetPrice(ItemDataSO item, City city);
        void ApplyTransaction(ItemDataSO item, City city, int quantity, bool isBuy);
        void Initialize(IWorldState worldState);
        void DailyUpdate();
        void ApplyEconomicEvent(EconomicEventSO eventData);
        Dictionary<int, float> GetAllModifiers();
        void SetAllModifiers(Dictionary<int, float> modifiers);
    }
}