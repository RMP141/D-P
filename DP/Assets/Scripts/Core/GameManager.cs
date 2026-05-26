using ConvoyManager.Combat;
using ConvoyManager.Data;
using ConvoyManager.Economy;
using ConvoyManager.Events;
using ConvoyManager.Player;
using ConvoyManager.Travel;
using ConvoyManager.UI;
using ConvoyManager.Utils;
using ConvoyManager.World;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace ConvoyManager.Core
{
    public class GameManager : LifetimeScope
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private HexTileConfig _hexTileConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameConfig);
            builder.RegisterInstance(_hexTileConfig);
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<IRandomGenerator, UnityRandomGenerator>(Lifetime.Singleton);

            // World
            builder.Register<IWorldState, WorldState>(Lifetime.Singleton);

            // Economy
            builder.Register<IEconomyEngine, EconomyEngine>(Lifetime.Singleton);

            // Player
            builder.Register<IPlayerProgress, PlayerProgress>(Lifetime.Singleton);

            // Combat
            builder.Register<IMercenaryManager, MercenaryManager>(Lifetime.Singleton);
            builder.Register<ICaptainCollection, CaptainCollection>(Lifetime.Singleton);
            builder.Register<CaptainGacha>(Lifetime.Singleton);
            builder.Register<ICombatStrategy, DefaultCombatCalculator>(Lifetime.Singleton);

            // Travel
            builder.Register<IRoutePlanner, RoutePlanner>(Lifetime.Singleton);

            // Events
            builder.Register<EconomicEventManager>(Lifetime.Singleton);
            builder.Register<EventResolver>(Lifetime.Singleton);

            // ECS
            var entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            builder.RegisterInstance(entityManager);

            // UI
            var uiDocument = Object.FindFirstObjectByType<UIDocument>();
            if (uiDocument != null)
                builder.RegisterInstance(uiDocument);
            else
                Debug.LogError("UIDocument не найден на сцене!");

            builder.Register<IUIManager, UIManager>(Lifetime.Singleton).As<IStartable>();
            builder.Register<MarketScreen>(Lifetime.Singleton);
            builder.Register<RoutePlannerScreen>(Lifetime.Singleton);
            builder.Register<EventResolverUI>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameFlow>();
        }
    }
}