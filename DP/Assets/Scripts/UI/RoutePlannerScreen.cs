using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.ECS;
using ConvoyManager.Economy;
using ConvoyManager.Player;
using ConvoyManager.Travel;
using ConvoyManager.World;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class RoutePlannerScreen
    {
        private readonly IWorldState _worldState;
        private readonly IRoutePlanner _routePlanner;
        private readonly EventBus _eventBus;
        private readonly IPlayerProgress _playerProgress;
        private readonly IEconomyEngine _economyEngine;
        private readonly EntityManager _entityManager;

        private VisualElement _root;
        private List<DropdownField> _cityDropdowns = new List<DropdownField>();
        private Button _createButton;
        private VisualElement _cargoContainer;
        private List<VisualElement> _cargoRows = new List<VisualElement>();
        private Dictionary<int, IntegerField> _cargoQtyFields = new Dictionary<int, IntegerField>();
        private EntityQuery _convoyQuery;

        public RoutePlannerScreen(IWorldState worldState, IRoutePlanner routePlanner, EventBus eventBus, IPlayerProgress playerProgress, IEconomyEngine economyEngine, EntityManager entityManager)
        {
            _worldState = worldState;
            _routePlanner = routePlanner;
            _eventBus = eventBus;
            _playerProgress = playerProgress;
            _economyEngine = economyEngine;
            _entityManager = entityManager;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            _convoyQuery = _entityManager.CreateEntityQuery(typeof(ConvoyTag));

            var visualTree = Resources.Load<VisualTreeAsset>("UI/RoutePlannerScreen");
            _root = visualTree.CloneTree();
            _root.style.position = Position.Absolute;
            _root.style.top = 40;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.display = DisplayStyle.None;
            rootVisualElement.Add(_root);

            var cityContainer = _root.Q<VisualElement>("city-dropdowns");
            for (int i = 0; i < 5; i++)
            {
                var dropdown = new DropdownField($"City {i + 1}");
                _cityDropdowns.Add(dropdown);
                cityContainer.Add(dropdown);
            }

            if (_cityDropdowns.Count > 0)
                _cityDropdowns[0].RegisterValueChangedCallback(_ => RefreshCargo());

            _cargoContainer = _root.Q<VisualElement>("cargo-container");

            _createButton = _root.Q<Button>("create-button");
            _createButton.clicked += OnCreateClicked;

            var closeButton = _root.Q<Button>("close-button");
            closeButton.clicked += () => Hide();
        }

        public void Show()
        {
            RefreshCities();
            BringToFront();
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void BringToFront()
        {
            var parent = _root.parent;
            if (parent != null)
            {
                _root.RemoveFromHierarchy();
                parent.Add(_root);
            }
        }

        public void RefreshCities()
        {
            var cityNames = GetCityNames();
            foreach (var dropdown in _cityDropdowns)
                dropdown.choices = cityNames;

            if (_cityDropdowns.Count >= 2 && cityNames.Count >= 2)
            {
                _cityDropdowns[0].value = cityNames[0];
                _cityDropdowns[1].value = cityNames[^1];
            }

            RefreshCargo();
        }

        private City GetDepartureCity()
        {
            string depName = _cityDropdowns.Count > 0 ? _cityDropdowns[0].value : null;
            return _worldState.Cities.FirstOrDefault(c => c.Name == depName);
        }

        private void RefreshCargo()
        {
            _cargoContainer.Clear();
            _cargoRows.Clear();
            _cargoQtyFields.Clear();

            var depCity = GetDepartureCity();
            if (depCity == null) return;

            foreach (var kvp in depCity.PlayerCache)
            {
                if (kvp.Value <= 0) continue;
                var item = _economyEngine.AllItems.FirstOrDefault(i => i.ID == kvp.Key);
                if (item == null) continue;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 5;
                row.style.minHeight = 25;

                var nameLabel = new Label(item.Name);
                nameLabel.style.width = 120;
                row.Add(nameLabel);

                var qtyField = new IntegerField();
                qtyField.style.width = 60;
                qtyField.value = 0;
                qtyField.isDelayed = true;
                row.Add(qtyField);

                _cargoQtyFields[item.ID] = qtyField;

                var availableLabel = new Label($"avail: {kvp.Value}");
                availableLabel.style.width = 80;
                availableLabel.style.color = Color.gray;
                row.Add(availableLabel);

                _cargoContainer.Add(row);
                _cargoRows.Add(row);
            }
        }

        private List<string> GetCityNames()
        {
            var names = new List<string>();
            foreach (var city in _worldState.Cities)
                if (_worldState.GetHex(city.HexIndex).IsDiscovered)
                    names.Add(city.Name);
            return names;
        }

        private void OnCreateClicked()
        {
            int convoyCount = _convoyQuery.CalculateEntityCount();
            if (convoyCount >= _playerProgress.MaxConvoys)
            {
                Debug.LogWarning($"[RoutePlanner] Max convoys reached ({_playerProgress.MaxConvoys})");
                return;
            }

            var selectedIndices = new List<int>();
            foreach (var dropdown in _cityDropdowns)
            {
                if (!string.IsNullOrEmpty(dropdown.value))
                {
                    int index = _worldState.Cities
                        .Select((city, idx) => new { city, idx })
                        .FirstOrDefault(x => x.city.Name == dropdown.value)?.idx ?? -1;
                    if (index >= 0)
                        selectedIndices.Add(index);
                }
            }

            if (selectedIndices.Count < 2)
            {
                Debug.LogWarning("Select at least 2 cities");
                return;
            }

            var depCity = GetDepartureCity();
            var cargoItems = new List<CargoItem>();
            foreach (var kvp in _cargoQtyFields)
            {
                int qty = kvp.Value.value;
                if (qty > 0 && depCity != null)
                {
                    depCity.PlayerCache.TryGetValue(kvp.Key, out int available);
                    int loadQty = Mathf.Min(qty, available);
                    if (loadQty > 0)
                    {
                        cargoItems.Add(new CargoItem { ItemId = kvp.Key, Quantity = loadQty });
                        depCity.RemoveFromPlayerCache(kvp.Key, loadQty);
                    }
                }
            }

            try
            {
                var entity = _routePlanner.CreateConvoy(selectedIndices.ToArray(), cargoItems.ToArray());
                _eventBus.Publish(new ConvoyCreatedEvent(entity));
                Hide();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[RoutePlanner] Cannot create convoy: {ex.Message}");
            }
        }
    }
}
