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
            var cityNames = _worldState.Cities.Select(c => c.Name).ToList();
            _cityDropdown.choices = cityNames;
            _cityDropdown.value = cityNames[0];
            SelectCity(_worldState.Cities[0]);
            UpdateGoldWeight();
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

        private void OnCityChanged(string cityName)
        {
            var city = _worldState.Cities.FirstOrDefault(c => c.Name == cityName);
            if (city != null)
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

            _playerProgress.Inventory.TryGetValue(item.ID, out int ownedQty);

            nameLabel.text = item.Name;
            ownedLabel.text = ownedQty > 0 ? $"x{ownedQty}" : "";
            buyPriceLabel.text = buyPrice.ToString("F1");
            sellPriceLabel.text = sellPrice.ToString("F1");

            qtyField.SetValueWithoutNotify(_quantities.TryGetValue(item.ID, out int stored) ? stored : 1);

            buyButton.clickable = new Clickable(() =>
            {
                int qty = Mathf.Max(1, qtyField.value);
                int totalCost = (int)(buyPrice * qty);
                float currentWeight = CalculateWeight();
                float addedWeight = item.Weight * qty;
                float maxWeight = _playerProgress.CartCount * 100;

                if (currentWeight + addedWeight > maxWeight) return;
                if (!_playerProgress.SpendGold(totalCost)) return;

                _playerProgress.AddItem(item.ID, qty);
                _economyEngine.ApplyTransaction(item, _currentCity, qty, true);
                UpdateGoldWeight();
                _listView.RefreshItems();
            });

            sellButton.clickable = new Clickable(() =>
            {
                int qty = Mathf.Max(1, qtyField.value);
                if (!_playerProgress.Inventory.TryGetValue(item.ID, out int available) || available < qty)
                    qty = available;
                if (qty <= 0) return;

                _playerProgress.RemoveItem(item.ID, qty);
                _playerProgress.AddGold((int)(sellPrice * qty));
                _economyEngine.ApplyTransaction(item, _currentCity, qty, false);
                UpdateGoldWeight();
                _listView.RefreshItems();
            });
        }

        private void UpdateGoldWeight()
        {
            _weightLabel.text = $"Gold: {_playerProgress.Gold}  |  Weight: {CalculateWeight():F1} / {_playerProgress.CartCount * 100}";
        }

        private float CalculateWeight()
        {
            float total = 0;
            foreach (var kvp in _playerProgress.Inventory)
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
