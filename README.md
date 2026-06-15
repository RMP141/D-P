# D-P — Convoy Manager

Дипломный проект — симулятор управления торговыми караванами в фэнтезийном мире.
Unity **6000.4.0f1**, ECS (DOTS), VContainer, UniRx, UI Toolkit.

## Структура скриптов (`Assets/Scripts/`)

### Core — ядро игры
| Файл | Описание |
|------|----------|
| `GameManager.cs` | DI-контейнер (VContainer LifetimeScope): регистрация всех систем и конфигов |
| `GameFlow.cs` | Оркестрация игрового цикла: меню → инициализация → тулбар → UI |
| `SaveSystem.cs` | JSON-сохранение/загрузка (3 слота): мир, экономика, игрок, ECS |
| `EventBus.cs` | Шина событий publish/subscribe на UniRx |
| `CoreEvents.cs` | Структуры всех событий игры (старт, загрузка, прибытие, поломка и т.д.) |

### World — мир и гексагональная карта
| Файл | Описание |
|------|----------|
| `IWorldState.cs` | Интерфейс состояния мира |
| `WorldState.cs` | Центральное состояние: генерация сетки 30×30, размещение городов, открытие клеток |
| `WorldStateData.cs` | Снимок мира для сериализации (hexes, cities) |
| `Hex.cs` | Класс одной гексагональной клетки: координаты, terrain, позиция + TerrainCost (cost/speed per terrain type) |
| `HexGenerator.cs` | Заглушка генерации рельефа (логика в WorldState) |
| `HexGridGenerator.cs` | MonoBehaviour: отрисовка гексов и городов на Tilemap |
| `HexClickHandler.cs` | Обработка клика по гексу для разведки (scout) с кулдауном |
| `City.cs` | Класс города: фракция, название, доступные товары, PlayerCache |
| `Faction.cs` | Enum 11 фракций (люди, дворфы, эльфы, зверокланы, аномалия) |
| `CameraPan.cs` | Edge-scroll камеры по границам карты |

### Economy — экономика
| Файл | Описание |
|------|----------|
| `IEconomyEngine.cs` | Интерфейс экономики: цены, транзакции, модификаторы |
| `EconomyEngine.cs` | Полная симуляция: спрос/предложение, региональные и событийные модификаторы, ежедневное восстановление |
| `TradeCalculator.cs` | Статические расчёты прибыли и стоимости сделок |

### Travel — маршруты и pathfinding
| Файл | Описание |
|------|----------|
| `IRoutePlanner.cs` | Интерфейс планировщика маршрутов |
| `RoutePlanner.cs` | Создание ECS-сущности каравана: A* hex-path, terrain-speeds, city-waypoints, груз |
| `CargoItem.cs` | Пара «ID предмета + количество» |

### Combat — бой, наёмники, капитаны, телеги
| Файл | Описание |
|------|----------|
| `ICombatStrategy.cs` | Интерфейс боевой системы |
| `DefaultCombatCalculator.cs` | Простой расчёт: рандомизированная атака vs защита → Victory/Defeat |
| `CombatResult.cs` | Enum: Victory, Defeat |
| `IMercenaryManager.cs` | Интерфейс управления наёмниками |
| `MercenaryManager.cs` | Наём/увольнение, стоимость, лимит, бонус к атаке |
| `ICartManager.cs` | Интерфейс управления телегами |
| `CartManager.cs` | Покупка/использование/возврат телег, лимит MaxCarts |
| `ICaptainCollection.cs` | Интерфейс коллекции капитанов |
| `CaptainCollection.cs` | Коллекция капитанов (макс. 20), выбор активного |
| `CaptainGacha.cs` | Gacha: weighted random по редкости + procedural fallback при пустом пуле |

### Events — игровые события
| Файл | Описание |
|------|----------|
| `EventResolver.cs` | Применение эффектов выбора события (золото, предметы, репутация, бой) |
| `EconomicEventManager.cs` | Периодический триггер экономических событий (изменение цен) |

### UI — интерфейс (UI Toolkit)
| Файл | Описание |
|------|----------|
| `IUIManager.cs` | Интерфейс управления экранами |
| `UIManager.cs` | Показ/скрытие экранов через UIDocument по `ShowScreenEvent` |
| `MainMenuScreen.cs` | Главное меню: New Game, Continue, Load, Quit |
| `SaveLoadSelectScreen.cs` | Модальное окно выбора слота сохранения (3 слота) |
| `RoutePlannerScreen.cs` | Планирование маршрута: проверка телег, городов, груза, создание каравана |
| `MarketScreen.cs` | Рынок: покупка/продажа товаров с ценами и количеством |
| `CaptainHireScreen.cs` | Наём капитанов (gacha): стоимость, результат, отображение активного |
| `EventResolverUI.cs` | Отображение triggered-события: заголовок, описание, кнопки выбора |
| `ConfirmDialog.cs` | Универсальный диалог подтверждения (да/нет) с callback |

### ECS — Entities система (DOTS)
| Файл | Описание |
|------|----------|
| `ConvoyTag.cs` | Tag-компонент «это караван» |
| `ConvoyStateComponent.cs` | Состояние + CurrentHexIndex (hex-by-hex движение) + прогресс |
| `PositionComponent.cs` | float3 позиция каравана |
| `MovementSpeed.cs` | Скорость каравана |
| `ResourceComponent.cs` | Запасы еды и износ каравана |
| `CargoComponent.cs` | Blob-массив грузов (ItemId + Quantity) |
| `RouteComponent.cs` | Blob: CityIndices, HexPath, HexTerrainSpeeds, CityWaypoints, CurrentSegment |
| `EventTimerComponent.cs` | Таймер до следующей проверки события |
| `ConvoyEventTags.cs` | Tag-компоненты: Arrived, ArrivedAtCity, OutOfFood, Broken, NeedCheck |
| `ConvoyMovementSystem.cs` | Hex-by-hex движение с terrain-скоростью; arrival-теги в городах |
| `ConvoyResourceSystem.cs` | Система ресурсов: расход еды, износ, теги OutOfFood/Broken |
| `ConvoyEventPublisherSystem.cs` | Теги → EventBus; очистка тегов |
| `EventTriggerSystem.cs` | Случайный триггер событий на караванах |
| `EventTimerSystem.cs` | Обратный отсчёт EventTimerComponent, добавление EventCheckTag |
| `ConvoyVisualizer.cs` | Lerp-визуализация по hex-пути (world-space позиции) |
| `ConvoyCleanupSystem.cs` | Отложенное уничтожение ECS-сущностей караванов |
| `ECSSerializer.cs` | Сериализация ECS-сущностей для save/load (HexPath, CityWaypoints, terrain speeds) |

### Data — ScriptableObject конфиги
| Файл | Описание |
|------|----------|
| `GameConfig.cs` | Центральный баланс: стартовые ресурсы, экономика, бой, события, ECS, MaxCarts, MaxConvoys |
| `HexTileConfig.cs` | Настройки тайлов рельефа, шума, CellSize (spacing гексов) |
| `ItemDataSO.cs` | Определение предмета: ID, цена, вес, категория, модификаторы |
| `ItemCategory.cs` | Enum категорий: Food, Weapons, Textiles, Gems, Metal, Elixirs, Artifacts |
| `EventDataSO.cs` | Определение события: заголовок, описание, варианты выбора, эффекты |
| `EconomicEventSO.cs` | Экономическое событие: длительность, множитель цен, категории |
| `CaptainDataSO.cs` | Определение капитана: ID, имя, редкость, бонусы атаки/защиты |

### Utils — утилиты
| Файл | Описание |
|------|----------|
| `IRandomGenerator.cs` | Абстракция для Random |
| `UnityRandomGenerator.cs` | Адаптер `UnityEngine.Random` под IRandomGenerator |
| `MathUtils.cs` | Хелперы: lerp, clamp, hex-дистанция (куб/offset) |
| `HexPathfinder.cs` | A* по hex-сетке с обходом воды и terrain-весами |
| `SerializableDictionary.cs` | Dictionary, сериализуемый Unity + JSON |
| `UnityMathContractResolver.cs` | Newtonsoft.Json resolver: пропуск Unity.Mathematics при сериализации |

### Editor
| Файл | Описание |
|------|----------|
| `SetupPrototype.cs` | Editor Menu для быстрого создания всех конфигов и тайлов в сцене |
