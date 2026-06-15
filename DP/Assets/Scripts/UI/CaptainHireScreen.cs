using ConvoyManager.Combat;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Player;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class CaptainHireScreen
    {
        private readonly CaptainGacha _gacha;
        private readonly ICaptainCollection _collection;
        private readonly IPlayerProgress _playerProgress;
        private readonly GameConfig _config;

        private VisualElement _root;
        private Label _goldLabel;
        private Label _pullCostLabel;
        private Label _resultLabel;
        private Button _pullButton;
        private VisualElement _captainsContainer;

        public CaptainHireScreen(CaptainGacha gacha, ICaptainCollection collection, IPlayerProgress playerProgress, GameConfig config)
        {
            _gacha = gacha;
            _collection = collection;
            _playerProgress = playerProgress;
            _config = config;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/CaptainHireScreen");
            _root = visualTree.CloneTree();
            _root.style.position = Position.Absolute;
            _root.style.top = 40;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.display = DisplayStyle.None;
            rootVisualElement.Add(_root);

            _goldLabel = _root.Q<Label>("gold-label");
            _pullCostLabel = _root.Q<Label>("pull-cost-label");
            _resultLabel = _root.Q<Label>("result-label");
            _captainsContainer = _root.Q<VisualElement>("captains-container");

            _pullButton = _root.Q<Button>("pull-button");
            _pullButton.clicked += OnPullClicked;

            var closeButton = _root.Q<Button>("close-button");
            closeButton.clicked += () => Hide();
        }

        public void Show()
        {
            Refresh();
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

        private void Refresh()
        {
            _goldLabel.text = $"Gold: {_playerProgress.Gold}";
            _pullCostLabel.text = $"Hire cost: {_config.CaptainGachaCost} gold";
            _pullButton.SetEnabled(_playerProgress.Gold >= _config.CaptainGachaCost);
            RefreshCaptainList();
        }

        private void RefreshCaptainList()
        {
            _captainsContainer.Clear();

            foreach (var captain in _collection.Captains)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 5;
                row.style.minHeight = 30;
                row.style.marginBottom = 3;

                bool isActive = _collection.ActiveCaptain != null && _collection.ActiveCaptain.ID == captain.ID;

                var nameLabel = new Label($"{captain.Name} ({captain.Rarity})");
                nameLabel.style.width = 200;
                nameLabel.style.color = isActive ? Color.yellow : Color.white;
                nameLabel.style.unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal;
                row.Add(nameLabel);

                string speedText = isActive ? " SPD:+5" : "";
                var statsLabel = new Label($"ATK:{captain.AttackBonus} DEF:{captain.DefenseBonus}{speedText}");
                statsLabel.style.width = 150;
                statsLabel.style.color = Color.gray;
                row.Add(statsLabel);

                if (!isActive)
                {
                    var activateBtn = new Button(() =>
                    {
                        _collection.SetActiveCaptain(captain.ID);
                        Refresh();
                    });
                    activateBtn.text = "Activate";
                    activateBtn.style.width = 80;
                    row.Add(activateBtn);
                }
                else
                {
                    var activeLabel = new Label("Active");
                    activeLabel.style.width = 80;
                    activeLabel.style.color = Color.yellow;
                    row.Add(activeLabel);
                }

                _captainsContainer.Add(row);
            }
        }

        private void OnPullClicked()
        {
            int cost = _config.CaptainGachaCost;
            if (!_playerProgress.SpendGold(cost))
            {
                _resultLabel.text = "Not enough gold!";
                return;
            }

            var captain = _gacha.Pull();
            if (captain == null)
            {
                _resultLabel.text = "No captains available!";
                _playerProgress.AddGold(cost);
                Refresh();
                return;
            }

            bool added = _collection.AddCaptain(captain);
            if (!added)
            {
                _resultLabel.text = "Captain roster full!";
                _playerProgress.AddGold(cost);
                Refresh();
                return;
            }

            _resultLabel.text = $"Hired: {captain.Name} ({captain.Rarity})!";
            Refresh();
        }
    }
}
