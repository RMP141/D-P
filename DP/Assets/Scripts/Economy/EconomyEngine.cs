using System;
using System.Collections.Generic;
using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Player;
using ConvoyManager.World;
using UnityEngine;
using CoreEventBus = ConvoyManager.Core.EventBus;

namespace ConvoyManager.Economy
{
    [Serializable]
    public struct ActiveEconomicEventData
    {
        public float TimeRemaining;
        public float PriceMultiplier;
        public ItemCategory[] AffectedCategories;

        public ActiveEconomicEventData(float timeRemaining, float priceMultiplier, ItemCategory[] affectedCategories)
        {
            TimeRemaining = timeRemaining;
            PriceMultiplier = priceMultiplier;
            AffectedCategories = affectedCategories;
        }
    }

    public class EconomyEngine : IEconomyEngine
    {
        private readonly GameConfig _config;
        private readonly CoreEventBus _eventBus;
        private readonly IPlayerProgress _playerProgress;
        private IWorldState _worldState;
        private List<ItemDataSO> _allItems;
        private Dictionary<int, float> _dynamicModifiers = new Dictionary<int, float>();
        private Dictionary<int, float> _eventModifiers = new Dictionary<int, float>();
        private List<ActiveEconomicEventData> _activeEvents = new List<ActiveEconomicEventData>();

        public EconomyEngine(GameConfig config, CoreEventBus eventBus, IPlayerProgress playerProgress)
        {
            _config = config;
            _eventBus = eventBus;
            _playerProgress = playerProgress;
            _allItems = Resources.LoadAll<ItemDataSO>("Items").ToList();
        }

        public IReadOnlyList<ItemDataSO> AllItems => _allItems;

        public void Initialize(IWorldState worldState)
        {
            _worldState = worldState;
            _dynamicModifiers.Clear();
            _eventModifiers.Clear();
            _activeEvents.Clear();
            foreach (var item in _allItems)
                _dynamicModifiers[item.ID] = 1f;
        }

        public float GetPrice(ItemDataSO item, City city)
        {
            float basePrice = item.BasePrice;
            float regional = item.GetRegionalModifier(city.Faction);
            float dynamic = _dynamicModifiers[item.ID];
            float eventMod = _eventModifiers.TryGetValue(item.ID, out var em) ? em : 1f;
            float repMod = GetReputationModifier(city.Faction);
            return basePrice * regional * dynamic * eventMod * repMod;
        }

        private float GetReputationModifier(Faction faction)
        {
            if (!_playerProgress.Reputation.TryGetValue(faction, out int rep))
                return 1f;

            if (rep >= 0)
                return 1f - (rep / 100f) * _config.ReputationPriceDiscount;
            else
                return 1f + (-rep / 100f) * _config.ReputationPricePenalty;
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

            ExpireEconomicEvents();
        }

        public void ApplyEconomicEvent(EconomicEventSO eventData)
        {
            var data = new ActiveEconomicEventData(
                eventData.DurationDays,
                eventData.PriceMultiplier,
                eventData.AffectedCategories
            );
            _activeEvents.Add(data);

            ApplyEventModifiers(data);
            PublishPriceUpdatesForAffectedCategories(data.AffectedCategories);
            _eventBus.Publish(new EconomicEventAppliedEvent(eventData));
        }

        private void ApplyEventModifiers(in ActiveEconomicEventData data)
        {
            foreach (var category in data.AffectedCategories)
            {
                foreach (var item in _allItems.Where(i => i.Category == category))
                {
                    _eventModifiers[item.ID] = data.PriceMultiplier;
                }
            }
        }

        private void RecalculateEventModifiers()
        {
            _eventModifiers.Clear();
            foreach (var evt in _activeEvents)
            {
                ApplyEventModifiers(evt);
            }
        }

        private void ExpireEconomicEvents()
        {
            bool changed = false;
            var expiredCategories = new HashSet<ItemCategory>();
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var evt = _activeEvents[i];
                evt.TimeRemaining -= 1f;
                _activeEvents[i] = evt;
                if (evt.TimeRemaining <= 0f)
                {
                    foreach (var cat in evt.AffectedCategories)
                        expiredCategories.Add(cat);
                    _activeEvents.RemoveAt(i);
                    changed = true;
                    Debug.Log($"[Economy] Economic event expired");
                }
            }

            if (changed)
            {
                RecalculateEventModifiers();
                PublishPriceUpdatesForAffectedCategories(expiredCategories.ToArray());
            }
        }

        private void PublishPriceUpdatesForAffectedCategories(ItemCategory[] categories)
        {
            if (_worldState == null) return;
            var affectedIds = new HashSet<int>();
            foreach (var cat in categories)
            {
                foreach (var item in _allItems.Where(i => i.Category == cat))
                    affectedIds.Add(item.ID);
            }
            foreach (var city in _worldState.Cities)
            {
                foreach (var id in affectedIds)
                {
                    var item = _allItems.FirstOrDefault(i => i.ID == id);
                    if (item != null)
                        _eventBus.Publish(new PriceUpdatedEvent(id, city.Index, GetPrice(item, city)));
                }
            }
        }

        public ActiveEconomicEventData[] GetActiveEventsData()
        {
            return _activeEvents.ToArray();
        }

        public void SetActiveEventsData(ActiveEconomicEventData[] events)
        {
            _activeEvents.Clear();
            if (events != null)
            {
                _activeEvents.AddRange(events);
            }
            RecalculateEventModifiers();
            if (_worldState != null)
            {
                foreach (var city in _worldState.Cities)
                {
                    foreach (var item in _allItems)
                        _eventBus.Publish(new PriceUpdatedEvent(item.ID, city.Index, GetPrice(item, city)));
                }
            }
        }

        public Dictionary<int, float> GetAllModifiers() => new Dictionary<int, float>(_dynamicModifiers);

        public bool IsItemAvailableAtCity(int itemId, City city)
        {
            return city.AvailableItemIds.Contains(itemId);
        }

        public int GetCityStock(int itemId, City city)
        {
            return city.Stock.TryGetValue(itemId, out int qty) ? qty : 0;
        }

        public bool CanBuyFromCity(ItemDataSO item, City city, int quantity)
        {
            if (!IsItemAvailableAtCity(item.ID, city)) return false;
            int stock = GetCityStock(item.ID, city);
            if (stock < quantity) return false;
            return true;
        }

        public bool CanSellToCity(ItemDataSO item, City city, int quantity)
        {
            float itemWeight = item.Weight * quantity;
            float currentWeight = 0f;
            foreach (var kvp in city.Stock)
            {
                var i = AllItems.FirstOrDefault(x => x.ID == kvp.Key);
                if (i != null) currentWeight += i.Weight * kvp.Value;
            }
            return currentWeight + itemWeight <= city.MaxWeight;
        }

        public void ModifyCityStock(City city, int itemId, int delta)
        {
            if (delta == 0) return;
            if (!city.Stock.ContainsKey(itemId) && delta > 0)
                city.Stock[itemId] = 0;
            city.Stock[itemId] = System.Math.Max(0, city.Stock[itemId] + delta);
        }

        public void SetAllModifiers(Dictionary<int, float> modifiers)
        {
            _dynamicModifiers = new Dictionary<int, float>(modifiers);
        }
    }
}