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
        private readonly IUIManager _uiManager;

        private VisualElement _root;
        private ListView _listView;
        private Label _goldLabel;
        private Label _weightLabel;
        private City _currentCity;
        private List<ItemDataSO> _items;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public MarketScreen(IEconomyEngine economyEngine, IPlayerProgress playerProgress, EventBus eventBus, IUIManager uiManager)
        {
            _economyEngine = economyEngine;
            _playerProgress = playerProgress;
            _eventBus = eventBus;
            _uiManager = uiManager;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/MarketScreen");
            _root = visualTree.CloneTree();
            _uiManager.RegisterScreen("Market", _root);

            _goldLabel = _root.Q<Label>("gold-label");
            _weightLabel = _root.Q<Label>("weight-label");
            _listView = _root.Q<ListView>("items-list");
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;

            var closeButton = _root.Q<Button>("close-button");
            closeButton.clicked += () => _uiManager.HideCurrentScreen();

            _eventBus.Subscribe<PriceUpdatedEvent>().Subscribe(OnPriceUpdated).AddTo(_disposables);
        }

        public void Show(City city)
        {
            _currentCity = city;
            _items = new List<ItemDataSO>(_economyEngine.AllItems);
            _listView.itemsSource = _items;
            UpdateGoldWeight();
            _uiManager.ShowScreen("Market");
        }

        private VisualElement MakeItem()
        {
            var template = Resources.Load<VisualTreeAsset>("UI/MarketItem");
            return template.CloneTree();
        }

        private void BindItem(VisualElement element, int index)
        {
            var item = _items[index];
            var nameLabel = element.Q<Label>("name");
            var buyPriceLabel = element.Q<Label>("buy-price");
            var sellPriceLabel = element.Q<Label>("sell-price");
            var buyButton = element.Q<Button>("buy-button");
            var sellButton = element.Q<Button>("sell-button");

            float buyPrice = _economyEngine.GetPrice(item, _currentCity);
            float sellPrice = buyPrice * 0.8f;

            nameLabel.text = item.Name;
            buyPriceLabel.text = buyPrice.ToString("F1");
            sellPriceLabel.text = sellPrice.ToString("F1");

            buyButton.clickable = null;
            buyButton.clicked += () =>
            {
                if (_playerProgress.SpendGold((int)buyPrice))
                {
                    _playerProgress.AddItem(item.ID, 1);
                    _economyEngine.ApplyTransaction(item, _currentCity, 1, true);
                    UpdateGoldWeight();
                }
            };

            sellButton.clickable = null;
            sellButton.clicked += () =>
            {
                if (_playerProgress.Inventory.TryGetValue(item.ID, out int qty) && qty > 0)
                {
                    _playerProgress.RemoveItem(item.ID, 1);
                    _playerProgress.AddGold((int)sellPrice);
                    _economyEngine.ApplyTransaction(item, _currentCity, 1, false);
                    UpdateGoldWeight();
                }
            };
        }

        private void UpdateGoldWeight()
        {
            _goldLabel.text = $"Gold: {_playerProgress.Gold}";
            _weightLabel.text = $"Weight: {CalculateWeight():F1} / {_playerProgress.CartCount * 100}";
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
                _listView.Rebuild();
                UpdateGoldWeight();
            }
        }
    }
}