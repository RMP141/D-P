using System;
using ConvoyManager.Data;
using ConvoyManager.ECS;
using ConvoyManager.Economy;
using ConvoyManager.Events;
using ConvoyManager.Player;
using ConvoyManager.UI;
using ConvoyManager.World;
using UniRx;
using Unity.Entities;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;
using ConvoyManager.Core;

namespace ConvoyManager.Core
{
    /// <summary>
    /// Управляет первоначальной инициализацией игрового состояния,
    /// подписывается на ключевые события.
    /// </summary>
    public class GameFlow : IStartable, IDisposable
    {
        private readonly ISaveSystem _saveSystem;
        private readonly IWorldState _worldState;
        private readonly IEconomyEngine _economyEngine;
        private readonly EconomicEventManager _economicEventManager;
        private readonly EventResolver _eventResolver;
        private readonly IPlayerProgress _playerProgress;
        private readonly IUIManager _uiManager;
        private readonly MarketScreen _marketScreen;
        private readonly RoutePlannerScreen _routePlannerScreen;
        private readonly EventResolverUI _eventResolverUI;
        private readonly UIDocument _uiDocument;
        private readonly EventBus _eventBus;
        private readonly GameConfig _config;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public GameFlow(
            ISaveSystem saveSystem,
            IWorldState worldState,
            IEconomyEngine economyEngine,
            EconomicEventManager economicEventManager,
            EventResolver eventResolver,
            IPlayerProgress playerProgress,
            IUIManager uiManager,
            MarketScreen marketScreen,
            RoutePlannerScreen routePlannerScreen,
            EventResolverUI eventResolverUI,
            UIDocument uiDocument,
            EventBus eventBus,
            GameConfig config)
        {
            _saveSystem = saveSystem;
            _worldState = worldState;
            _economyEngine = economyEngine;
            _economicEventManager = economicEventManager;
            _eventResolver = eventResolver;
            _playerProgress = playerProgress;
            _uiManager = uiManager;
            _marketScreen = marketScreen;
            _routePlannerScreen = routePlannerScreen;
            _eventResolverUI = eventResolverUI;
            _uiDocument = uiDocument;
            _eventBus = eventBus;
            _config = config;
        }

        public void Start()
        {
            // Инициализация UI-экранов (после того, как UIDocument готов)
            var root = _uiDocument.rootVisualElement;
            _marketScreen.Initialize(root);
            _routePlannerScreen.Initialize(root);
            _eventResolverUI.Initialize(root);

            // Пытаемся загрузить сохранение или начать новую игру
            if (_saveSystem.HasSave())
            {
                _saveSystem.LoadGame();
                _eventBus.Publish(new GameLoadedEvent());
            }
            else
            {
                StartNewGame();
            }

            // Настройка ECS-систем
            ConfigureECSSystems();

            // Подписка на события караванов
            _eventBus.Subscribe<EventTriggeredMessage>()
                .Subscribe(msg =>
                {
                    // Временно автоматически выбираем первый вариант,
                    // пока не реализован полноценный UI выбора.
                    _eventResolver.Resolve(msg.EventData, 0);
                })
                .AddTo(_disposables);
        }

        private void StartNewGame()
        {
            int seed = Random.Range(0, int.MaxValue);
            _worldState.Generate(seed);
            _economyEngine.Initialize(_worldState);
            _economicEventManager.Start();
            _eventBus.Publish(new GameStartedEvent());
        }

        private void ConfigureECSSystems()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            // Загружаем пул событий
            var eventPool = Resources.LoadAll<EventDataSO>("Events/EventData");
            var triggerSystem = world.GetOrCreateSystemManaged<EventTriggerSystem>();
            triggerSystem.EventBus = _eventBus;
            triggerSystem.EventPool = eventPool;
            triggerSystem.Probability = _config.EventProbability;

            var publisherSystem = world.GetOrCreateSystemManaged<ConvoyEventPublisherSystem>();
            publisherSystem.EventBus = _eventBus;

            // Убеждаемся, что системы созданы
            world.GetOrCreateSystem<ConvoyMovementSystem>();
            world.GetOrCreateSystem<ConvoyResourceSystem>();
            world.GetOrCreateSystem<EventTimerSystem>();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}