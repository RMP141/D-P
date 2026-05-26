using System.Collections.Generic;
using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.World;
using UnityEngine;
// Алиас для устранения конфликта с Unity.VisualScripting.EventBus
using CoreEventBus = ConvoyManager.Core.EventBus;

namespace ConvoyManager.Economy
{
    public class EconomyEngine : IEconomyEngine
    {
        private readonly GameConfig _config;
        private readonly CoreEventBus _eventBus;
        private IWorldState _worldState;
        private List<ItemDataSO> _allItems;
        private Dictionary<int, float> _dynamicModifiers = new Dictionary<int, float>();
        private Dictionary<int, float> _eventModifiers = new Dictionary<int, float>();

        public EconomyEngine(GameConfig config, CoreEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
            _allItems = Resources.LoadAll<ItemDataSO>("Items").ToList();
        }

        public IReadOnlyList<ItemDataSO> AllItems => _allItems;

        public void Initialize(IWorldState worldState)
        {
            _worldState = worldState;
            _dynamicModifiers.Clear();
            _eventModifiers.Clear();
            foreach (var item in _allItems)
                _dynamicModifiers[item.ID] = 1f;
        }

        public float GetPrice(ItemDataSO item, City city)
        {
            float basePrice = item.BasePrice;
            float regional = item.GetRegionalModifier(city.Faction);
            float dynamic = _dynamicModifiers[item.ID];
            float eventMod = _eventModifiers.TryGetValue(item.ID, out var em) ? em : 1f;
            return basePrice * regional * dynamic * eventMod;
        }

        public void ApplyTransaction(ItemDataSO item, City city, int quantity, bool isBuy)
        {
            float elasticity = item.Elasticity;
            float delta = isBuy ? quantity * 0.01f * elasticity : -quantity * 0.01f * elasticity;
            float current = _dynamicModifiers[item.ID];
            float newMod = Mathf.Clamp(current + delta, 0.5f, 2f);
            _dynamicModifiers[item.ID] = newMod;

            _eventBus.Publish(new PriceUpdatedEvent(item.ID, city.Index, GetPrice(item, city)));
        }

        public void DailyUpdate()
        {
            float recoveryRate = _config.PriceRecoveryRate;
            foreach (var item in _allItems)
            {
                float current = _dynamicModifiers[item.ID];
                float diff = 1f - current;
                _dynamicModifiers[item.ID] = current + diff * recoveryRate;
            }
        }

        public void ApplyEconomicEvent(EconomicEventSO eventData)
        {
            foreach (var category in eventData.AffectedCategories)
            {
                foreach (var item in _allItems.Where(i => i.Category == category))
                {
                    _eventModifiers[item.ID] = eventData.PriceMultiplier;
                }
            }
            _eventBus.Publish(new EconomicEventAppliedEvent(eventData));
        }

        public Dictionary<int, float> GetAllModifiers() => new Dictionary<int, float>(_dynamicModifiers);

        public void SetAllModifiers(Dictionary<int, float> modifiers)
        {
            _dynamicModifiers = new Dictionary<int, float>(modifiers);
        }
    }
}