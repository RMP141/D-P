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
        ActiveEconomicEventData[] GetActiveEventsData();
        void SetActiveEventsData(ActiveEconomicEventData[] events);
        bool IsItemAvailableAtCity(int itemId, City city);
        int GetCityStock(int itemId, City city);
        bool CanBuyFromCity(ItemDataSO item, City city, int quantity);
        bool CanSellToCity(ItemDataSO item, City city, int quantity);
        void ModifyCityStock(City city, int itemId, int delta);
        Dictionary<int, float> GetAllModifiers();
        void SetAllModifiers(Dictionary<int, float> modifiers);
    }
}