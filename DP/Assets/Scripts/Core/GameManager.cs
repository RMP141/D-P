using ConvoyManager.Combat;
using ConvoyManager.Data;
using ConvoyManager.ECS;
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
        public GameConfig _gameConfig;
        public HexTileConfig _hexTileConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameConfig);
            builder.RegisterInstance(_hexTileConfig);
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<ISaveSystem, SaveSystem>(Lifetime.Singleton);
            builder.Register<IRandomGenerator, UnityRandomGenerator>(Lifetime.Singleton);

            // World
            builder.Register<IWorldState, WorldState>(Lifetime.Singleton);

            // Economy
            builder.Register<IEconomyEngine, EconomyEngine>(Lifetime.Singleton);

            // Player
            builder.Register<IPlayerProgress, PlayerProgress>(Lifetime.Singleton);

            // Combat
            builder.Register<IMercenaryManager, MercenaryManager>(Lifetime.Singleton);
            builder.Register<ICartManager, CartManager>(Lifetime.Singleton);
            builder.Register<ICaptainCollection, CaptainCollection>(Lifetime.Singleton);
            builder.Register<CaptainGacha>(Lifetime.Singleton);
            builder.Register<ICombatStrategy, DefaultCombatCalculator>(Lifetime.Singleton);

            // Travel
            builder.Register<IRoutePlanner, RoutePlanner>(Lifetime.Singleton);

            // Events
            builder.Register<EconomicEventManager>(Lifetime.Singleton);
            builder.Register<EventResolver>(Lifetime.Singleton);

            // ECS
            Unity.Entities.EntityManager entityManager;
            try
            {
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    world = new Unity.Entities.World("Default World", Unity.Entities.WorldFlags.Game);
                    Unity.Entities.World.DefaultGameObjectInjectionWorld = world;
                }
                entityManager = world.EntityManager;
            }
            catch
            {
                Debug.LogWarning("ECS World not available, creating fallback entity manager");
                var world = new Unity.Entities.World("Fallback World", Unity.Entities.WorldFlags.Game);
                Unity.Entities.World.DefaultGameObjectInjectionWorld = world;
                entityManager = world.EntityManager;
            }
            builder.RegisterInstance(entityManager);
            builder.Register<ECSSerializer>(Lifetime.Singleton);

            // UI
            var uiDocument = Object.FindFirstObjectByType<UIDocument>();
            if (uiDocument != null)
                builder.RegisterInstance(uiDocument);
            else
                Debug.LogError("UIDocument �� ������ �� �����!");

            builder.Register<IUIManager, UIManager>(Lifetime.Singleton).As<IStartable>();
            builder.Register<MainMenuScreen>(Lifetime.Singleton);
            builder.Register<SaveLoadSelectScreen>(Lifetime.Singleton);
            builder.Register<MarketScreen>(Lifetime.Singleton);
            builder.Register<RoutePlannerScreen>(Lifetime.Singleton);
            builder.Register<EventResolverUI>(Lifetime.Singleton);
            builder.Register<CaptainHireScreen>(Lifetime.Singleton);
            builder.Register<ConfirmDialog>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameFlow>();
        }
    }
}