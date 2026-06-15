using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Economy;
using ConvoyManager.Player;
using ConvoyManager.World;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class MarketScreen
    {
        private readonly IEconomyEngine _economyEngine;
        private readonly IPlayerProgress _playerProgress;
        private readonly EventBus _eventBus;
        private readonly IWorldState _worldState;

        private VisualElement _root;
        private ListView _listView;
        private Label _weightLabel;
        private DropdownField _cityDropdown;
        private City _currentCity;
        private List<ItemDataSO> _items;
        private CompositeDisposable _disposables = new CompositeDisposable();
        private Dictionary<int, int> _quantities = new Dictionary<int, int>();
        private Dictionary<string, City> _displayNameToCity = new Dictionary<string, City>();

        public MarketScreen(IEconomyEngine economyEngine, IPlayerProgress playerProgress, EventBus eventBus, IWorldState worldState)
        {
            _economyEngine = economyEngine;
            _playerProgress = playerProgress;
            _eventBus = eventBus;
            _worldState = worldState;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/MarketScreen");
            _root = visualTree.CloneTree();
            _root.style.position = Position.Absolute;
            _root.style.top = 40;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.display = DisplayStyle.None;
            rootVisualElement.Add(_root);

            _weightLabel = _root.Q<Label>("weight-label");
            _listView = _root.Q<ListView>("items-list");
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;

            _cityDropdown = _root.Q<DropdownField>("city-dropdown");
            if (_cityDropdown != null)
                _cityDropdown.RegisterValueChangedCallback(evt => OnCityChanged(evt.newValue));

            var closeButton = _root.Q<Button>("close-button");
            closeButton.clicked += () => Hide();

            _eventBus.Subscribe<PriceUpdatedEvent>().Subscribe(OnPriceUpdated).AddTo(_disposables);
        }

        public void Show()
        {
            var visibleCities = _worldState.Cities
                .Where(c => _worldState.GetHex(c.HexIndex).IsDiscovered)
                .ToList();

            _displayNameToCity.Clear();
            var cityNames = new List<string>();
            foreach (var c in visibleCities)
            {
                string prefix = c.Type == SettlementType.City ? "★ " : "▪ ";
                string displayName = prefix + c.Name;
                _displayNameToCity[displayName] = c;
                cityNames.Add(displayName);
            }

            _cityDropdown.choices = cityNames;
            if (cityNames.Count > 0)
            {
                _cityDropdown.value = cityNames[0];
                SelectCity(visibleCities[0]);
            }
            UpdateGoldWeight();
            UpdateDropdownStyle();
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

        private void OnCityChanged(string displayName)
        {
            if (_displayNameToCity.TryGetValue(displayName, out var city))
            {
                SelectCity(city);
                _listView.RefreshItems();
            }
        }

        private void SelectCity(City city)
        {
            _currentCity = city;
            _items = new List<ItemDataSO>(_economyEngine.AllItems);
            _listView.itemsSource = _items;
            UpdateGoldWeight();
            UpdateDropdownStyle();
        }

        private void UpdateDropdownStyle()
        {
            if (_cityDropdown == null || _currentCity == null) return;
            bool isCity = _currentCity.Type == SettlementType.City;
            _cityDropdown.label = isCity ? "City: " : "Village: ";
            var labelColor = isCity ? new Color(0.83f, 0.63f, 0.09f) : new Color(0.36f, 0.71f, 0.75f);
            if (_cityDropdown.labelElement != null)
                _cityDropdown.labelElement.style.color = labelColor;
        }

        private VisualElement MakeItem()
        {
            var template = Resources.Load<VisualTreeAsset>("UI/MarketItem");
            var element = template.CloneTree();

            var qtyField = element.Q<IntegerField>("quantity");
            qtyField.RegisterValueChangedCallback(evt =>
            {
                int itemId = (int)element.userData;
                _quantities[itemId] = Mathf.Max(1, evt.newValue);
            });

            return element;
        }

        private void BindItem(VisualElement element, int index)
        {
            var item = _items[index];
            element.userData = item.ID;

            var nameLabel = element.Q<Label>("name");
            var ownedLabel = element.Q<Label>("owned");
            var buyPriceLabel = element.Q<Label>("buy-price");
            var sellPriceLabel = element.Q<Label>("sell-price");
            var buyButton = element.Q<Button>("buy-button");
            var sellButton = element.Q<Button>("sell-button");
            var qtyField = element.Q<IntegerField>("quantity");

            float buyPrice = _economyEngine.GetPrice(item, _currentCity);
            float sellPrice = buyPrice * 0.8f;

            _currentCity.PlayerCache.TryGetValue(item.ID, out int ownedQty);
            int cityStock = _economyEngine.GetCityStock(item.ID, _currentCity);

            nameLabel.text = item.Name;
            ownedLabel.text = $"own:{ownedQty} stock:{cityStock}";
            buyPriceLabel.text = buyPrice.ToString("F1");
            sellPriceLabel.text = sellPrice.ToString("F1");

            qtyField.SetValueWithoutNotify(_quantities.TryGetValue(item.ID, out int stored) ? stored : 1);

            bool canBuyHere = _economyEngine.IsItemAvailableAtCity(item.ID, _currentCity) && cityStock > 0;
            buyButton.style.display = canBuyHere ? DisplayStyle.Flex : DisplayStyle.None;

            buyButton.clickable = new Clickable(() =>
            {
                if (!canBuyHere) return;
                int qty = Mathf.Max(1, qtyField.value);
                if (!_economyEngine.CanBuyFromCity(item, _currentCity, qty)) return;
                int totalCost = (int)(buyPrice * qty);
                if (!_playerProgress.SpendGold(totalCost)) return;

                _currentCity.AddToPlayerCache(item.ID, qty);
                _economyEngine.ModifyCityStock(_currentCity, item.ID, -qty);
                _economyEngine.ApplyTransaction(item, _currentCity, qty, true);
                _eventBus.Publish(new ShowToastRequest($"Bought {qty} {item.Name} for {totalCost} gold"));
                UpdateGoldWeight();
                _listView.RefreshItems();
            });

            sellButton.clickable = new Clickable(() =>
            {
                int qty = Mathf.Max(1, qtyField.value);
                _currentCity.PlayerCache.TryGetValue(item.ID, out int available);
                if (available < qty) qty = available;
                if (qty <= 0) return;
                if (!_economyEngine.CanSellToCity(item, _currentCity, qty))
                {
                    _eventBus.Publish(new ShowToastRequest("City warehouse is full, cannot sell here"));
                    return;
                }

                _currentCity.RemoveFromPlayerCache(item.ID, qty);
                int revenue = (int)(sellPrice * qty);
                _playerProgress.AddGold(revenue);
                _economyEngine.ModifyCityStock(_currentCity, item.ID, qty);
                _economyEngine.ApplyTransaction(item, _currentCity, qty, false);
                _eventBus.Publish(new ShowToastRequest($"Sold {qty} {item.Name} for {revenue} gold"));
                UpdateGoldWeight();
                _listView.RefreshItems();
            });
        }

        private void UpdateGoldWeight()
        {
            float currentW = 0, maxW = 0;
            if (_currentCity != null)
            {
                currentW = CalculateCacheWeight(_currentCity);
                maxW = _currentCity.MaxWeight;
            }
            _weightLabel.text = $"Gold: {_playerProgress.Gold}  |  Local: {currentW:F1} / {maxW:F1}";
        }

        private float CalculateCacheWeight(City city)
        {
            float total = 0;
            foreach (var kvp in city.PlayerCache)
            {
                var item = _economyEngine.AllItems.FirstOrDefault(i => i.ID == kvp.Key);
                if (item != null) total += item.Weight * kvp.Value;
            }
            return total;
        }

        private void OnPriceUpdated(PriceUpdatedEvent evt)
        {
            if (_currentCity != null && evt.CityIndex == _currentCity.Index)
            {
                _listView.RefreshItems();
                UpdateGoldWeight();
            }
        }
    }
}
