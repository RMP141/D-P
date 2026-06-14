using System;
using System.Linq;
using ConvoyManager.Combat;
using ConvoyManager.Data;
using ConvoyManager.ECS;
using ConvoyManager.Economy;
using ConvoyManager.Events;
using ConvoyManager.Player;
using ConvoyManager.Rendering;
using ConvoyManager.UI;
using ConvoyManager.World;
using UniRx;
using Unity.Entities;
using UnityEngine;
using VContainer.Unity;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;
using ConvoyManager.Core;

namespace ConvoyManager.Core
{
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
        private readonly MainMenuScreen _mainMenuScreen;
        private readonly SaveLoadSelectScreen _saveLoadSelect;
        private readonly CaptainHireScreen _captainHireScreen;
        private readonly ICaptainCollection _captainCollection;
        private readonly ConfirmDialog _confirmDialog;
        private UIDocument _uiDocument;
        private readonly EventBus _eventBus;
        private readonly GameConfig _config;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private VisualElement _toolbar;
        private HexClickHandler _hexClickHandler;
        private Unity.Entities.World _ecsWorld;
        private bool _gameStarted;
        private bool _hasActiveSession;
        private Label _goldLabel;
        private Label _mercLabel;
        private Label _convoyLabel;
        private int _convoyCount;

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
            MainMenuScreen mainMenuScreen,
            SaveLoadSelectScreen saveLoadSelect,
            CaptainHireScreen captainHireScreen,
            ICaptainCollection captainCollection,
            ConfirmDialog confirmDialog,
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
            _mainMenuScreen = mainMenuScreen;
            _saveLoadSelect = saveLoadSelect;
            _captainHireScreen = captainHireScreen;
            _captainCollection = captainCollection;
            _confirmDialog = confirmDialog;
            _eventBus = eventBus;
            _config = config;
        }

        public void Start()
        {
            _uiDocument = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            if (_uiDocument != null)
            {
                var root = _uiDocument.rootVisualElement;
                _mainMenuScreen.Initialize(root);
                _marketScreen.Initialize(root);
                _routePlannerScreen.Initialize(root);
                _eventResolverUI.Initialize(root);
                _saveLoadSelect.Initialize(root);
                _captainHireScreen.Initialize(root);
                _confirmDialog.Initialize(root);
            }

            _saveLoadSelect.OnSaveSlotSelected += slot => SaveToSlot(slot);
            _saveLoadSelect.OnLoadSlotSelected += slot => LoadSlot(slot);
            _saveLoadSelect.OnDeleteSlotClicked += OnDeleteSlot;
            _saveLoadSelect.OnBackClicked += OnSaveLoadBack;
            _saveLoadSelect.OnMainMenuClicked += OnSaveLoadMainMenu;

            _eventBus.Subscribe<ConvoyArrivedEvent>().Subscribe(OnConvoyArrived).AddTo(_disposables);
            _eventBus.Subscribe<ConvoyCreatedEvent>().Subscribe(e => { _convoyCount++; UpdateHUD(); }).AddTo(_disposables);
            _eventBus.Subscribe<EventTriggeredMessage>().Subscribe(msg => { HideAllOverlays(); _eventResolverUI.Show(msg); }).AddTo(_disposables);
            Observable.Interval(System.TimeSpan.FromSeconds(1)).Subscribe(_ => UpdateHUD()).AddTo(_disposables);

            _mainMenuScreen.OnNewGameClicked += StartGame;
            _mainMenuScreen.OnLoadGameClicked += ShowSaveLoadForLoad;
            _mainMenuScreen.OnContinueClicked += ContinueGame;
            _mainMenuScreen.OnQuitClicked += OnQuitClicked;

            _uiManager.ShowScreen("MainMenu");
        }

        private void StartGame()
        {
            if (_gameStarted) return;
            _gameStarted = true;
            _hasActiveSession = false;

            StartNewGame();
            ConfigureECSSystems();
            SetECSPaused(false);
            SetupHexClickHandler();
            CreateToolbar();
            _uiManager.HideCurrentScreen();
        }

        private void ShowSaveLoadForSave()
        {
            HideAllOverlays();
            _saveLoadSelect.ShowForSave();
        }

        private void ShowSaveLoadForLoad()
        {
            HideAllOverlays();
            _saveLoadSelect.ShowForLoad();
        }

        private void SaveToSlot(int slot)
        {
            if (_saveSystem.HasSlot(slot))
            {
                _confirmDialog.Show("Overwrite existing save data?", confirmed =>
                {
                    if (confirmed)
                    {
                        _saveSystem.SaveGame(slot);
                        _saveLoadSelect.Hide();
                    }
                });
            }
            else
            {
                _saveSystem.SaveGame(slot);
                _saveLoadSelect.Hide();
            }
        }

        private void LoadSlot(int slot)
        {
            if (_gameStarted) return;

            _gameStarted = true;
            _hasActiveSession = false;
            _saveLoadSelect.Hide();
            _saveSystem.LoadGame(slot);
            _economicEventManager.Start();
            _eventBus.Publish(new GameLoadedEvent());
            ConfigureECSSystems();
            SetECSPaused(false);
            SetupHexClickHandler();
            CreateToolbar();
            _uiManager.HideCurrentScreen();
            RefreshTilemap();
        }

        private void OnDeleteSlot(int slot)
        {
            _confirmDialog.Show("Delete this save? This cannot be undone.", confirmed =>
            {
                if (confirmed)
                {
                    _saveSystem.DeleteGame(slot);
                    _saveLoadSelect.Refresh();
                }
            });
        }

        private void OnQuitClicked()
        {
            _confirmDialog.Show("Are you sure you want to quit?", confirmed =>
            {
                if (confirmed)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            });
        }

        private void ContinueGame()
        {
            if (!_hasActiveSession) return;

            _gameStarted = true;
            _hasActiveSession = false;
            _ecsWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            SetECSPaused(false);
            SetupHexClickHandler();
            CreateToolbar();
            _uiManager.HideCurrentScreen();
        }

        private void OnSaveLoadBack()
        {
            _saveLoadSelect.Hide();
            if (!_gameStarted)
                _uiManager.ShowScreen("MainMenu");
        }

        private void OnSaveLoadMainMenu()
        {
            _saveLoadSelect.Hide();
            ShowMainMenu(hasActiveSession: _gameStarted);
        }

        private void RefreshTilemap()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return;
            var gen = grid.GetComponent<HexGridGenerator>();
            if (gen != null)
                gen.Redraw();
        }

        private void UpdateHUD()
        {
            if (_goldLabel != null)
                _goldLabel.text = $"Gold: {_playerProgress.Gold}";
            if (_mercLabel != null)
                _mercLabel.text = $"Mercs: {_playerProgress.MercenaryCount}";
            if (_convoyLabel != null)
                _convoyLabel.text = $"Convoys: {_convoyCount}";
        }

        private void OnConvoyArrived(ConvoyArrivedEvent evt)
        {
            var world = _ecsWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            var route = em.GetComponentData<RouteComponent>(evt.ConvoyEntity);
            ref var routeBlob = ref route.Blob.Value;
            int arrivalCityIdx = routeBlob.CityIndices[routeBlob.CurrentSegment + 1];
            var arrivalCity = _worldState.GetCity(arrivalCityIdx);

            var cargo = em.GetComponentData<CargoComponent>(evt.ConvoyEntity);
            if (cargo.Blob.IsCreated)
            {
                ref var cargoBlob = ref cargo.Blob.Value;
                float totalEarned = 0;
                for (int i = 0; i < cargoBlob.Items.Length; i++)
                {
                    var item = cargoBlob.Items[i];
                    var itemSO = _economyEngine.AllItems.FirstOrDefault(it => it.ID == item.ItemId);
                    if (itemSO == null) continue;
                    float sellPrice = _economyEngine.GetPrice(itemSO, arrivalCity) * 0.8f;
                    float earned = sellPrice * item.Quantity;
                    _playerProgress.AddGold((int)earned);
                    _economyEngine.ApplyTransaction(itemSO, arrivalCity, item.Quantity, false);
                    totalEarned += earned;
                }
                Debug.Log($"[Convoy] Arrived at {arrivalCity.Name}, sold cargo for {totalEarned:F0} gold");
                cargo.Blob.Dispose();
                em.SetComponentData(evt.ConvoyEntity, new CargoComponent { Blob = default });
                UpdateHUD();
            }

            if (routeBlob.CurrentSegment < routeBlob.CityIndices.Length - 2)
            {
                int newSegment = routeBlob.CurrentSegment + 1;
                using (var blobBuilder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var newRoute = ref blobBuilder.ConstructRoot<RouteBlob>();
                    var arr = blobBuilder.Allocate(ref newRoute.CityIndices, routeBlob.CityIndices.Length);
                    for (int i = 0; i < routeBlob.CityIndices.Length; i++)
                        arr[i] = routeBlob.CityIndices[i];
                    newRoute.CurrentSegment = newSegment;
                    route.Blob.Dispose();
                    route.Blob = blobBuilder.CreateBlobAssetReference<RouteBlob>(Unity.Collections.Allocator.Persistent);
                    em.SetComponentData(evt.ConvoyEntity, route);
                }
                em.SetComponentData(evt.ConvoyEntity, new ConvoyStateComponent { State = ConvoyState.Traveling, Progress = 0f });
                Debug.Log($"[Convoy] Continuing to next city (segment {newSegment})");
            }
            else
            {
                em.SetComponentData(evt.ConvoyEntity, new ConvoyStateComponent { State = ConvoyState.WaitingForInput, Progress = 0f });
                _convoyCount = System.Math.Max(0, _convoyCount - 1);
                UpdateHUD();
                Debug.Log($"[Convoy] Route complete at {arrivalCity.Name}");
            }
        }

        private void CreateToolbar()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;

            if (_toolbar != null)
            {
                _toolbar.RemoveFromHierarchy();
                _toolbar = null;
            }

            _toolbar = new VisualElement();
            _toolbar.style.position = Position.Absolute;
            _toolbar.style.top = 0;
            _toolbar.style.left = 0;
            _toolbar.style.right = 0;
            _toolbar.style.height = 40;
            _toolbar.style.flexDirection = FlexDirection.Row;
            _toolbar.style.backgroundColor = new Color(0, 0, 0, 0.5f);
            _toolbar.style.paddingLeft = 5;
            _toolbar.style.paddingTop = 5;

            _goldLabel = new Label($"Gold: {_playerProgress.Gold}");
            _goldLabel.style.width = 120;
            _goldLabel.style.color = Color.white;
            _goldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _goldLabel.style.height = 30;
            _toolbar.Add(_goldLabel);

            _mercLabel = new Label($"Mercs: {_playerProgress.MercenaryCount}");
            _mercLabel.style.width = 80;
            _mercLabel.style.color = Color.white;
            _mercLabel.style.height = 30;
            _toolbar.Add(_mercLabel);

            _convoyLabel = new Label($"Convoys: {_convoyCount}");
            _convoyLabel.style.width = 100;
            _convoyLabel.style.color = Color.white;
            _convoyLabel.style.height = 30;
            _toolbar.Add(_convoyLabel);

            var marketBtn = new Button(() => { HideAllOverlays(); _marketScreen.Show(); });
            marketBtn.text = "Market";
            marketBtn.style.width = 80;
            marketBtn.style.height = 30;
            _toolbar.Add(marketBtn);

            var routeBtn = new Button(() => { HideAllOverlays(); _routePlannerScreen.Show(); });
            routeBtn.text = "Route";
            routeBtn.style.width = 80;
            routeBtn.style.height = 30;
            routeBtn.style.marginLeft = 5;
            _toolbar.Add(routeBtn);

            var captainBtn = new Button(() => { HideAllOverlays(); _captainHireScreen.Show(); });
            captainBtn.text = "Captains";
            captainBtn.style.width = 80;
            captainBtn.style.height = 30;
            captainBtn.style.marginLeft = 5;
            _toolbar.Add(captainBtn);

            var saveBtn = new Button(() => ShowSaveLoadForSave());
            saveBtn.text = "Save";
            saveBtn.style.width = 80;
            saveBtn.style.height = 30;
            saveBtn.style.marginLeft = 5;
            _toolbar.Add(saveBtn);

            var menuBtn = new Button(() => ShowMainMenu(hasActiveSession: true));
            menuBtn.text = "Menu";
            menuBtn.style.width = 80;
            menuBtn.style.height = 30;
            menuBtn.style.marginLeft = 5;
            _toolbar.Add(menuBtn);

            root.Add(_toolbar);
        }

        private void StartNewGame()
        {
            int seed = Random.Range(0, int.MaxValue);
            _worldState.Generate(seed);
            _economyEngine.Initialize(_worldState);
            _economicEventManager.Start();
            _captainCollection.Clear();
            _eventBus.Publish(new GameStartedEvent());
        }

        private void ConfigureECSSystems()
        {
            _ecsWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var world = _ecsWorld;
            var simGroup = world.GetOrCreateSystemManaged<Unity.Entities.SimulationSystemGroup>();

            var eventPool = Resources.LoadAll<EventDataSO>("Events/EventData");
            var triggerSystem = world.GetOrCreateSystemManaged<EventTriggerSystem>();
            triggerSystem.EventBus = _eventBus;
            triggerSystem.EventPool = eventPool;
            triggerSystem.Probability = _config.EventProbability;
            simGroup.AddSystemToUpdateList(triggerSystem);

            var publisherSystem = world.GetOrCreateSystemManaged<ConvoyEventPublisherSystem>();
            publisherSystem.EventBus = _eventBus;
            simGroup.AddSystemToUpdateList(publisherSystem);

            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<ConvoyMovementSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<ConvoyResourceSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<EventTimerSystem>());

            var visSystem = world.GetOrCreateSystemManaged<ConvoyVisualizationSystem>();
            visSystem.SetWorldState(_worldState);
            world.GetOrCreateSystemManaged<Unity.Entities.PresentationSystemGroup>().AddSystemToUpdateList(visSystem);

            simGroup.SortSystems();
        }

        private void SetupHexClickHandler()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return;
            _hexClickHandler = grid.GetComponent<HexClickHandler>();
            if (_hexClickHandler == null)
                _hexClickHandler = grid.AddComponent<HexClickHandler>();
            _hexClickHandler.Initialize(_worldState, _eventBus);
            _hexClickHandler.SetActive(true);
        }

        private void ShowMainMenu(bool hasActiveSession = false)
        {
            if (_toolbar != null)
            {
                _toolbar.style.display = DisplayStyle.None;
                _toolbar.RemoveFromHierarchy();
                _toolbar = null;
            }
            if (_hexClickHandler != null)
                _hexClickHandler.SetActive(false);
            SetECSPaused(true);
            _gameStarted = false;
            _hasActiveSession = hasActiveSession;
            _mainMenuScreen.SetContinueVisible(_hasActiveSession);
            _uiManager.ShowScreen("MainMenu");
        }

        private void HideAllOverlays()
        {
            _marketScreen.Hide();
            _routePlannerScreen.Hide();
            _saveLoadSelect.Hide();
            _eventResolverUI.Hide();
            _captainHireScreen.Hide();
        }

        private void SetECSPaused(bool paused)
        {
            if (_ecsWorld != null && _ecsWorld.IsCreated)
            {
                var simGroup = _ecsWorld.GetExistingSystemManaged<Unity.Entities.SimulationSystemGroup>();
                if (simGroup != null)
                    simGroup.Enabled = !paused;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
