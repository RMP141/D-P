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
using ConvoyManager.Utils;
using ConvoyManager.World;
using UniRx;
using Unity.Entities;
using UnityEngine;
using VContainer.Unity;
using Random = UnityEngine.Random;
using UnityEngine.UIElements;

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
        private readonly IMercenaryManager _mercManager;
        private readonly ICartManager _cartManager;
        private readonly ICombatStrategy _combatCalculator;
        private readonly ConfirmDialog _confirmDialog;
        private UIDocument _uiDocument;
        private readonly EventBus _eventBus;
        private readonly GameConfig _config;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private VisualElement _toolbar;
        private HexClickHandler _hexClickHandler;
        private CameraPan _cameraPan;
        private Unity.Entities.World _ecsWorld;
        private ConvoyCleanupSystem _cleanupSystem;
        private bool _gameStarted;
        private bool _hasActiveSession;
        private Label _goldLabel;
        private Label _mercLabel;
        private Label _convoyLabel;
        private int _convoyCount;
        private VisualElement _legendPanel;
        private Label _toastLabel;
        private IDisposable _toastTimer;
        private bool _firstConvoyHandled;
        private VisualElement _battleOverlay;
        private Entity _battleConvoyEntity;

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
            IMercenaryManager mercManager,
            ICartManager cartManager,
            ICombatStrategy combatCalculator,
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
            _mercManager = mercManager;
            _cartManager = cartManager;
            _combatCalculator = combatCalculator;
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
            _eventBus.Subscribe<ConvoyArrivedAtCityEvent>().Subscribe(OnConvoyArrivedAtCity).AddTo(_disposables);
            _eventBus.Subscribe<ConvoyCreatedEvent>().Subscribe(OnConvoyCreated).AddTo(_disposables);
            _eventBus.Subscribe<EventTriggeredMessage>().Subscribe(msg => { HideAllOverlays(); _eventResolverUI.Show(msg); SetMapInteraction(false); }).AddTo(_disposables);
            _eventBus.Subscribe<CombatResolvedEvent>().Subscribe(evt => ShowToast(evt.Result == CombatResult.Victory ? "Combat victory! +30 gold" : "Combat defeat...")).AddTo(_disposables);
            _eventBus.Subscribe<DamageCartEvent>().Subscribe(evt => ShowToast($"Cart damaged by {evt.DamageAmount}!")).AddTo(_disposables);
            _eventBus.Subscribe<RepairCartEvent>().Subscribe(evt => ShowToast($"Cart repaired by {evt.RepairAmount}!")).AddTo(_disposables);
            _eventBus.Subscribe<EventResolvedEvent>().Subscribe(evt => ShowToast($"Event: {evt.EventTitle}")).AddTo(_disposables);
            _eventBus.Subscribe<EconomicEventAppliedEvent>().Subscribe(evt => ShowToast($"Economic shift: {evt.EventData.Title}")).AddTo(_disposables);
            _eventBus.Subscribe<ShowToastRequest>().Subscribe(r => ShowToast(r.Message, r.Duration)).AddTo(_disposables);
            _eventBus.Subscribe<OutOfFoodEvent>().Subscribe(evt =>
            {
                ShowToast("A convoy has run out of food and stopped!");
                var em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.HasComponent<ConvoyStateComponent>(evt.ConvoyEntity))
                {
                    em.SetComponentData(evt.ConvoyEntity, new ConvoyStateComponent { State = ConvoyState.WaitingForInput, Progress = 0f });
                }
            }).AddTo(_disposables);
            _eventBus.Subscribe<BrokenEvent>().Subscribe(evt =>
            {
                ShowToast("A convoy has broken down and stopped!");
                var em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
                if (em.HasComponent<ConvoyStateComponent>(evt.ConvoyEntity))
                {
                    em.SetComponentData(evt.ConvoyEntity, new ConvoyStateComponent { State = ConvoyState.WaitingForInput, Progress = 0f });
                }
            }).AddTo(_disposables);
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
                    SetMapInteraction(true);
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

        private void RefreshTilemap()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return;
            var gen = grid.GetComponent<HexGridGenerator>();
            if (gen != null)
                gen.Redraw();
        }

        private void StartNewGame()
        {
            _firstConvoyHandled = false;
            _convoyCount = 0;
            int seed = Random.Range(0, int.MaxValue);
            _worldState.Generate(seed);
            _economyEngine.Initialize(_worldState);
            _economicEventManager.Start();
            _captainCollection.Clear();
            _eventBus.Publish(new GameStartedEvent());
        }

        private void ContinueGame()
        {
            if (!_hasActiveSession) return;

            _gameStarted = true;
            _hasActiveSession = false;
            _ecsWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            _economicEventManager.Start();
            SetECSPaused(false);
            SetupHexClickHandler();
            CreateToolbar();
            _uiManager.HideCurrentScreen();
        }

        private void OnConvoyCreated(ConvoyCreatedEvent evt)
        {
            _convoyCount++;
            UpdateHUD();

            if (!_firstConvoyHandled)
            {
                _firstConvoyHandled = true;
                ShowFirstConvoyBattle(evt.ConvoyEntity);
            }
        }

        private void ShowFirstConvoyBattle(Entity convoyEntity)
        {
            _battleConvoyEntity = convoyEntity;

            var em = _ecsWorld.EntityManager;
            if (em.HasComponent<ConvoyStateComponent>(convoyEntity))
            {
                em.SetComponentData(convoyEntity, new ConvoyStateComponent { State = ConvoyState.WaitingForInput, Progress = 0f });
            }

            var playerForceLabel = _battleOverlay.Q<Label>("BattlePlayerForceLabel");
            if (playerForceLabel != null)
                playerForceLabel.text = "Your forces: Mercenaries x" + _mercManager.MercenaryCount;

            var resultLabel = _battleOverlay.Q<Label>("BattleResultLabel");
            if (resultLabel != null) resultLabel.text = "";

            _battleOverlay.Q<Button>("BattleEngageBtn").style.display = DisplayStyle.Flex;
            _battleOverlay.Q<Button>("BattleDismissBtn").style.display = DisplayStyle.None;

            SetECSPaused(true);

            HideAllOverlays();
            _battleOverlay.style.display = DisplayStyle.Flex;
        }

        private void ResolveFirstConvoyBattle()
        {
            int mercCount = _mercManager.MercenaryCount;
            int captainBonus = _captainCollection.ActiveCaptain != null ? 5 : 0;
            float enemyPower = 2.5f;

            CombatResult result = _combatCalculator.Resolve(mercCount, captainBonus, enemyPower, new UnityRandomGenerator());

            var resultLabel = _battleOverlay.Q<Label>("BattleResultLabel");

            if (result == CombatResult.Victory)
            {
                resultLabel.text = "Victory! The bandits are defeated.\nYour convoy continues.";
                _battleOverlay.Q<Button>("BattleEngageBtn").style.display = DisplayStyle.None;
                _battleOverlay.Q<Button>("BattleDismissBtn").style.display = DisplayStyle.Flex;
                _playerProgress.AddGold(30);
            }
            else
            {
                resultLabel.text = "Defeat... Your convoy was raided.\nYou lost some supplies.";
                _battleOverlay.Q<Button>("BattleEngageBtn").style.display = DisplayStyle.None;
                _battleOverlay.Q<Button>("BattleDismissBtn").style.display = DisplayStyle.Flex;

                var em = _ecsWorld.EntityManager;
                if (em.HasComponent<CargoComponent>(_battleConvoyEntity))
                {
                    var cargo = em.GetComponentData<CargoComponent>(_battleConvoyEntity);
                    cargo.Blob.Dispose();
                    em.SetComponentData(_battleConvoyEntity, new CargoComponent { Blob = default });
                }
            }
        }

        private void DismissFirstConvoyBattle()
        {
            _battleOverlay.style.display = DisplayStyle.None;

            var em = _ecsWorld.EntityManager;
            if (em.HasComponent<ConvoyStateComponent>(_battleConvoyEntity))
            {
                em.SetComponentData(_battleConvoyEntity, new ConvoyStateComponent { State = ConvoyState.Traveling, Progress = 0f });
            }

            SetECSPaused(false);
            _battleConvoyEntity = default;
        }

        private void CreateBattleOverlay(VisualElement root)
        {
            _battleOverlay = new VisualElement();
            _battleOverlay.style.position = Position.Absolute;
            _battleOverlay.style.top = 0;
            _battleOverlay.style.left = 0;
            _battleOverlay.style.right = 0;
            _battleOverlay.style.bottom = 0;
            _battleOverlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _battleOverlay.style.alignItems = Align.Center;
            _battleOverlay.style.justifyContent = Justify.Center;
            _battleOverlay.style.display = DisplayStyle.None;

            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(0.15f, 0.12f, 0.1f, 0.95f);
            panel.style.borderTopWidth = 2;
            panel.style.borderBottomWidth = 2;
            panel.style.borderLeftWidth = 2;
            panel.style.borderRightWidth = 2;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.paddingTop = 20;
            panel.style.paddingBottom = 20;
            panel.style.width = 420;

            var title = new Label("Ambush!");
            title.name = "BattleTitleLabel";
            title.style.color = new Color(0.9f, 0.3f, 0.2f);
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 12;
            panel.Add(title);

            var desc = new Label("Bandits attack your first convoy!");
            desc.style.color = Color.white;
            desc.style.fontSize = 14;
            desc.style.unityTextAlign = TextAnchor.MiddleCenter;
            desc.style.marginBottom = 16;
            panel.Add(desc);

            var forcesRow = new VisualElement();
            forcesRow.style.flexDirection = FlexDirection.Row;
            forcesRow.style.justifyContent = Justify.Center;
            forcesRow.style.marginBottom = 16;

            var playerForce = new Label("Your forces: Mercenaries x" + _mercManager.MercenaryCount);
            playerForce.name = "BattlePlayerForceLabel";
            playerForce.style.color = new Color(0.3f, 0.8f, 0.3f);
            playerForce.style.fontSize = 14;
            playerForce.style.marginRight = 20;
            forcesRow.Add(playerForce);

            var enemyForce = new Label("Enemy: Bandits x5");
            enemyForce.style.color = new Color(0.9f, 0.3f, 0.2f);
            enemyForce.style.fontSize = 14;
            forcesRow.Add(enemyForce);

            panel.Add(forcesRow);

            var resultLabel = new Label("");
            resultLabel.name = "BattleResultLabel";
            resultLabel.style.color = new Color(0.8f, 0.7f, 0.3f);
            resultLabel.style.fontSize = 16;
            resultLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            resultLabel.style.marginBottom = 16;
            resultLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(resultLabel);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            var engageBtn = new Button(ResolveFirstConvoyBattle);
            engageBtn.name = "BattleEngageBtn";
            engageBtn.text = "Engage!";
            engageBtn.style.width = 140;
            engageBtn.style.height = 36;
            engageBtn.style.backgroundColor = new Color(0.4f, 0.25f, 0.05f);
            engageBtn.style.color = Color.white;
            engageBtn.style.fontSize = 15;
            engageBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            buttonRow.Add(engageBtn);

            var dismissBtn = new Button(DismissFirstConvoyBattle);
            dismissBtn.name = "BattleDismissBtn";
            dismissBtn.text = "Continue";
            dismissBtn.style.width = 140;
            dismissBtn.style.height = 36;
            dismissBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
            dismissBtn.style.color = Color.white;
            dismissBtn.style.fontSize = 15;
            dismissBtn.style.display = DisplayStyle.None;
            buttonRow.Add(dismissBtn);

            panel.Add(buttonRow);
            _battleOverlay.Add(panel);
            root.Add(_battleOverlay);
        }

        private void OnConvoyArrived(ConvoyArrivedEvent evt)
        {
            // Route complete — final city reached, convoy is done
            var world = _ecsWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            var route = em.GetComponentData<RouteComponent>(evt.ConvoyEntity);
            if (!route.Blob.IsCreated || route.Blob.Value.CityIndices.Length < 2)
            {
                _cleanupSystem?.PendingDestroy.Enqueue(evt.ConvoyEntity);
                return;
            }

            // Find the arrival city from the last waypoint
            ref var routeBlob = ref route.Blob.Value;
            int cityIdx = routeBlob.CityIndices[routeBlob.CityIndices.Length - 1];
            var arrivalCity = _worldState.GetCity(cityIdx);

            UnloadConvoyCargo(em, evt.ConvoyEntity, arrivalCity);

            _convoyCount = System.Math.Max(0, _convoyCount - 1);
            _cartManager.ReturnCart();
            UpdateHUD();
            Debug.Log($"[Convoy] Route complete at {arrivalCity.Name}");
            _cleanupSystem?.PendingDestroy.Enqueue(evt.ConvoyEntity);
        }

        private void OnConvoyArrivedAtCity(ConvoyArrivedAtCityEvent evt)
        {
            // Mid-route stop — unload cargo and resume
            var world = _ecsWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;

            var route = em.GetComponentData<RouteComponent>(evt.ConvoyEntity);
            if (!route.Blob.IsCreated) return;

            ref var routeBlob = ref route.Blob.Value;
            var state = em.GetComponentData<ConvoyStateComponent>(evt.ConvoyEntity);

            // Find which city waypoint the convoy is at
            int cityWaypointIdx = -1;
            for (int i = 0; i < routeBlob.CityWaypoints.Length; i++)
            {
                if (routeBlob.CityWaypoints[i] == state.CurrentHexIndex)
                {
                    cityWaypointIdx = i;
                    break;
                }
            }
            if (cityWaypointIdx < 0) return;

            int arrivalCityIdx = routeBlob.CityIndices[cityWaypointIdx];
            var arrivalCity = _worldState.GetCity(arrivalCityIdx);

            UnloadConvoyCargo(em, evt.ConvoyEntity, arrivalCity);

            // Resume traveling toward next city
            state.State = ConvoyState.Traveling;
            state.Progress = 0f;
            em.SetComponentData(evt.ConvoyEntity, state);
        }

        private void UnloadConvoyCargo(EntityManager em, Entity convoyEntity, City arrivalCity)
        {
            if (!em.HasComponent<CargoComponent>(convoyEntity)) return;
            var cargo = em.GetComponentData<CargoComponent>(convoyEntity);
            if (!cargo.Blob.IsCreated) return;

            ref var cargoBlob = ref cargo.Blob.Value;
            string itemSummary = "";
            for (int i = 0; i < cargoBlob.Items.Length; i++)
            {
                var item = cargoBlob.Items[i];
                arrivalCity.AddToPlayerCache(item.ItemId, item.Quantity);
                string itemName = "Item#" + item.ItemId;
                var itemDef = _economyEngine.AllItems.FirstOrDefault(d => d.ID == item.ItemId);
                if (itemDef != null) itemName = itemDef.Name;
                if (i > 0) itemSummary += ", ";
                itemSummary += itemName + " x" + item.Quantity;
            }
            string toastMsg = $"Convoy arrived at {arrivalCity.Name}\nReceived: {itemSummary}";
            ShowToast(toastMsg);
            cargo.Blob.Dispose();
            em.SetComponentData(convoyEntity, new CargoComponent { Blob = default });
            UpdateHUD();
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

            var hireBtn = new Button(() =>
            {
                if (_mercManager.Hire())
                {
                    ShowToast("Hired a mercenary! -50 gold");
                    UpdateHUD();
                }
                else
                {
                    ShowToast("Cannot hire: not enough gold or at max mercenaries");
                }
            });
            hireBtn.text = "Hire";
            hireBtn.style.width = 60;
            hireBtn.style.height = 30;
            hireBtn.style.marginLeft = 3;
            hireBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f);
            hireBtn.style.color = Color.white;
            hireBtn.style.fontSize = 11;
            _toolbar.Add(hireBtn);

            var fireBtn = new Button(() =>
            {
                if (_mercManager.Fire())
                {
                    ShowToast("Fired a mercenary! +25 gold refund");
                    UpdateHUD();
                }
                else
                {
                    ShowToast("No mercenaries to fire");
                }
            });
            fireBtn.text = "Fire";
            fireBtn.style.width = 60;
            fireBtn.style.height = 30;
            fireBtn.style.marginLeft = 3;
            fireBtn.style.backgroundColor = new Color(0.4f, 0.2f, 0.2f);
            fireBtn.style.color = Color.white;
            fireBtn.style.fontSize = 11;
            _toolbar.Add(fireBtn);

            var cartLabel = new Label($"Carts: {_cartManager.AvailableCarts}");
            cartLabel.name = "cart-label";
            cartLabel.style.width = 80;
            cartLabel.style.color = Color.white;
            cartLabel.style.height = 30;
            _toolbar.Add(cartLabel);

            var buyCartBtn = new Button(() =>
            {
                if (_cartManager.BuyCart())
                {
                    ShowToast("Bought a cart! -100 gold");
                    UpdateHUD();
                }
                else
                {
                    ShowToast("Cannot buy cart: not enough gold or at max carts");
                }
            });
            buyCartBtn.text = "Buy Cart";
            buyCartBtn.style.width = 80;
            buyCartBtn.style.height = 30;
            buyCartBtn.style.marginLeft = 3;
            buyCartBtn.style.backgroundColor = new Color(0.2f, 0.3f, 0.5f);
            buyCartBtn.style.color = Color.white;
            buyCartBtn.style.fontSize = 11;
            _toolbar.Add(buyCartBtn);

            _convoyLabel = new Label($"Convoys: {_convoyCount}");
            _convoyLabel.style.width = 100;
            _convoyLabel.style.color = Color.white;
            _convoyLabel.style.height = 30;
            _toolbar.Add(_convoyLabel);

            var marketBtn = new Button(() => { HideAllOverlays(); _marketScreen.Show(); SetMapInteraction(false); });
            marketBtn.text = "Market";
            marketBtn.style.width = 80;
            marketBtn.style.height = 30;
            _toolbar.Add(marketBtn);

            var routeBtn = new Button(() => { HideAllOverlays(); _routePlannerScreen.Show(); SetMapInteraction(false); });
            routeBtn.text = "Route";
            routeBtn.style.width = 80;
            routeBtn.style.height = 30;
            routeBtn.style.marginLeft = 5;
            _toolbar.Add(routeBtn);

            var captainBtn = new Button(() => { HideAllOverlays(); _captainHireScreen.Show(); SetMapInteraction(false); });
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

            var legendBtn = new Button(() => ToggleLegend());
            legendBtn.text = "Legend";
            legendBtn.style.width = 80;
            legendBtn.style.height = 30;
            legendBtn.style.marginLeft = 5;
            _toolbar.Add(legendBtn);

            root.Add(_toolbar);
            CreateLegendPanel(root);
            CreateToast(root);
            CreateBattleOverlay(root);
        }

        private void CreateLegendPanel(VisualElement root)
        {
            _legendPanel = new VisualElement();
            _legendPanel.style.position = Position.Absolute;
            _legendPanel.style.top = 45;
            _legendPanel.style.right = 5;
            _legendPanel.style.backgroundColor = new Color(0, 0, 0, 0.75f);
            _legendPanel.style.paddingLeft = 8;
            _legendPanel.style.paddingRight = 8;
            _legendPanel.style.paddingTop = 8;
            _legendPanel.style.paddingBottom = 8;
            _legendPanel.style.display = DisplayStyle.None;

            AddLegendEntry(_legendPanel, new Color(0.3f, 0.6f, 0.3f), "Plains");
            AddLegendEntry(_legendPanel, new Color(0.1f, 0.4f, 0.1f), "Forest");
            AddLegendEntry(_legendPanel, new Color(0.4f, 0.35f, 0.25f), "Mountains");
            AddLegendEntry(_legendPanel, new Color(0.2f, 0.4f, 0.6f), "Water (impassable)");
            AddLegendEntry(_legendPanel, new Color(0.83f, 0.63f, 0.09f), "City");
            AddLegendEntry(_legendPanel, new Color(0.36f, 0.71f, 0.75f), "Village");

            root.Add(_legendPanel);
        }

        private static void AddLegendEntry(VisualElement parent, Color color, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;

            var swatch = new VisualElement();
            swatch.style.width = 14;
            swatch.style.height = 14;
            swatch.style.backgroundColor = color;
            swatch.style.marginRight = 6;
            row.Add(swatch);

            var text = new Label(label);
            text.style.color = Color.white;
            text.style.fontSize = 12;
            row.Add(text);

            parent.Add(row);
        }

        private void ToggleLegend()
        {
            if (_legendPanel == null) return;
            bool isVisible = _legendPanel.style.display == DisplayStyle.Flex;
            _legendPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void CreateToast(VisualElement root)
        {
            _toastLabel = new Label();
            _toastLabel.style.position = Position.Absolute;
            _toastLabel.style.bottom = 40;
            _toastLabel.style.left = 0;
            _toastLabel.style.right = 0;
            _toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toastLabel.style.fontSize = 15;
            _toastLabel.style.color = Color.white;
            _toastLabel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            _toastLabel.style.paddingTop = 10;
            _toastLabel.style.paddingBottom = 10;
            _toastLabel.style.paddingLeft = 16;
            _toastLabel.style.paddingRight = 16;
            _toastLabel.style.whiteSpace = WhiteSpace.Normal;
            _toastLabel.style.display = DisplayStyle.None;
            root.Add(_toastLabel);
        }

        private void ShowToast(string message, float duration = 4f)
        {
            _toastTimer?.Dispose();
            _toastLabel.text = message;
            _toastLabel.style.display = DisplayStyle.Flex;
            _toastTimer = Observable.Timer(TimeSpan.FromSeconds(duration))
                .Subscribe(_ =>
                {
                    _toastLabel.style.display = DisplayStyle.None;
                })
                .AddTo(_disposables);
        }

        private void UpdateHUD()
        {
            if (_goldLabel != null) _goldLabel.text = $"Gold: {_playerProgress.Gold}";
            if (_mercLabel != null) _mercLabel.text = $"Mercs: {_playerProgress.MercenaryCount}";
            if (_convoyLabel != null) _convoyLabel.text = $"Convoys: {_convoyCount}";
            var cartLabel = _toolbar?.Q<Label>("cart-label");
            if (cartLabel != null) cartLabel.text = $"Carts: {_cartManager.AvailableCarts}";
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

            var publisherSystem = world.GetOrCreateSystemManaged<ConvoyEventPublisherSystem>();
            publisherSystem.EventBus = _eventBus;

            world.GetOrCreateSystem<ConvoyMovementSystem>();
            world.GetOrCreateSystem<ConvoyResourceSystem>();
            world.GetOrCreateSystem<EventTimerSystem>();

            var visSystem = world.GetOrCreateSystemManaged<ConvoyVisualizationSystem>();
            visSystem.SetWorldState(_worldState);

            _cleanupSystem = world.GetOrCreateSystemManaged<ConvoyCleanupSystem>();
        }

        private void SetMapInteraction(bool enabled)
        {
            if (_hexClickHandler != null)
                _hexClickHandler.SetActive(enabled);
            if (_cameraPan != null)
                _cameraPan.SetActive(enabled);
        }

        private void SetupHexClickHandler()
        {
            var grid = GameObject.Find("Grid");
            if (grid == null) return;
            _hexClickHandler = grid.GetComponent<HexClickHandler>();
            if (_hexClickHandler == null)
                _hexClickHandler = grid.AddComponent<HexClickHandler>();
            _hexClickHandler.Initialize(_worldState, _eventBus);
            SetMapInteraction(true);

            if (Camera.main != null)
                _cameraPan = Camera.main.GetComponent<CameraPan>();
        }

        private void ShowMainMenu(bool hasActiveSession = false)
        {
            if (_toolbar != null)
            {
                _toolbar.style.display = DisplayStyle.None;
                _toolbar.RemoveFromHierarchy();
                _toolbar = null;
            }
            _economicEventManager.Stop();
            SetMapInteraction(false);
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
            SetMapInteraction(true);
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
