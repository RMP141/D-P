using ConvoyManager.Core;
using ConvoyManager.Travel;
using ConvoyManager.World;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class RoutePlannerScreen
    {
        private readonly IWorldState _worldState;
        private readonly IRoutePlanner _routePlanner;
        private readonly EventBus _eventBus;
        private readonly IUIManager _uiManager;

        private VisualElement _root;
        private List<DropdownField> _cityDropdowns = new List<DropdownField>();
        private Button _createButton;

        public RoutePlannerScreen(IWorldState worldState, IRoutePlanner routePlanner, EventBus eventBus, IUIManager uiManager)
        {
            _worldState = worldState;
            _routePlanner = routePlanner;
            _eventBus = eventBus;
            _uiManager = uiManager;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/RoutePlannerScreen");
            _root = visualTree.CloneTree();
            _uiManager.RegisterScreen("RoutePlanner", _root);

            var cityContainer = _root.Q<VisualElement>("city-dropdowns");
            for (int i = 0; i < 5; i++)
            {
                var dropdown = new DropdownField($"City {i + 1}");
                dropdown.choices = GetCityNames();
                _cityDropdowns.Add(dropdown);
                cityContainer.Add(dropdown);
            }

            _createButton = _root.Q<Button>("create-button");
            _createButton.clicked += OnCreateClicked;

            var closeButton = _root.Q<Button>("close-button");
            closeButton.clicked += () => _uiManager.HideCurrentScreen();
        }

        private List<string> GetCityNames()
        {
            var names = new List<string>();
            foreach (var city in _worldState.Cities)
                names.Add(city.Name);
            return names;
        }

        private void OnCreateClicked()
        {
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

            var cargo = new CargoItem[0];
            var entity = _routePlanner.CreateConvoy(selectedIndices.ToArray(), cargo);
            _eventBus.Publish(new ConvoyCreatedEvent(entity));

            _uiManager.ShowScreen("Map");
        }
    }

}