# D-P — Convoy Manager

Дипломный проект — симулятор управления торговыми караванами в фэнтезийном мире.
Unity **6000.4.0f1**, ECS, VContainer, UniRx, UGUI Toolkit.

## Структура скриптов (`Assets/Scripts/`)

### Core — ядро игры
| Файл | Описание |
|------|----------|
| `GameManager.cs` | DI-контейнер (VContainer LifetimeScope): регистрация всех систем и конфигов |
| `GameFlow.cs` | Оркестрация игрового цикла: меню → инициализация → UI |
| `SaveSystem.cs` | JSON-сохранение/загрузка (3 слота): мир, экономика, игрок, ECS |
| `EventBus.cs` | Шина событий publish/subscribe на UniRx |
| `CoreEvents.cs` | Структуры всех событий игры (старт, загрузка, прибытие, поломка и т.д.) |

### World — мир и гексагональная карта
| Файл | Описание |
|------|----------|
| `IWorldState.cs` | Интерфейс состояния мира |
| `WorldState.cs` | Центральное состояние: генерация сетки 30×30, размещение городов, открытие клеток |
| `WorldStateData.cs` | Снимок мира для сериализации (hexes, cities) |
| `Hex.cs` | Класс одной гексагональной клетки: координаты, terrain, позиция |
| `HexGenerator.cs` | Заглушка генерации рельефа (логика в WorldState) |
| `HexGridGenerator.cs` | MonoBehaviour: отрисовка гексов и городов на Tilemap |
| `HexClickHandler.cs` | Обработка клика по гексу для разведки (scout) с кулдауном |
| `City.cs` | Класс города: фракция, название, доступные товары |
| `Faction.cs` | Enum 11 фракций (люди, дворфы, эльфы, зверокланы, аномалия) |
| `CameraPan.cs` | Edge-scroll камеры по границам карты |

### Economy — экономика
| Файл | Описание |
|------|----------|
| `IEconomyEngine.cs` | Интерфейс экономики: цены, транзакции, модификаторы |
| `EconomyEngine.cs` | Полная симуляция: спрос/предложение, региональные и событийные модификаторы, ежедневное восстановление |
| `TradeCalculator.cs` | Статические расчёты прибыли и стоимости сделок |

### Travel — маршруты
| Файл | Описание |
|------|----------|
| `IRoutePlanner.cs` | Интерфейс планировщика маршрутов |
| `RoutePlanner.cs` | Создание ECS-сущности каравана с маршрутом, грузом и состоянием |
| `CargoItem.cs` | Пара «ID предмета + количество» |

### Combat — бой и наёмники
| Файл | Описание |
|------|----------|
| `ICombatStrategy.cs` | Интерфейс боевой системы |
| `DefaultCombatCalculator.cs` | Простой расчёт: рандомизированная атака vs защита → Victory/Defeat |
| `CombatResult.cs` | Enum: Victory, Defeat |
| `IMercenaryManager.cs` | Интерфейс управления наёмниками |
| `MercenaryManager.cs` | Наём/увольнение, стоимость, лимит, бонус к атаке |
| `ICaptainCollection.cs` | Интерфейс коллекции капитанов |
| `CaptainCollection.cs` | Коллекция капитанов в памяти (макс. 20), выбор активного |
| `CaptainGacha.cs` | Gacha-система: weighted random по редкости (Common→Legendary) |

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
| `RoutePlannerScreen.cs` | Планирование маршрута: выбор городов, груза, создание каравана |
| `MarketScreen.cs` | Рынок: покупка/продажа товаров с ценами и количеством |
| `CaptainHireScreen.cs` | Наём капитанов (gacha): стоимость, результат, отображение |
| `EventResolverUI.cs` | Отображение triggered-события: заголовок, описание, кнопки выбора |
| `ConfirmDialog.cs` | Универсальный диалог подтверждения (да/нет) с callback |

### ECS — Entities система (DOTS)
| Файл | Описание |
|------|----------|
| `ConvoyTag.cs` | Tag-компонент «это караван» |
| `ConvoyStateComponent.cs` | Состояние каравана: Idle/Traveling/WaitingForInput + прогресс 0..1 |
| `PositionComponent.cs` | float3 позиция каравана |
| `MovementSpeed.cs` | Скорость каравана |
| `ResourceComponent.cs` | Запасы еды и износ каравана |
| `CargoComponent.cs` | Blob-массив грузов (ItemId + Quantity) |
| `RouteComponent.cs` | Blob-массив индексов городов маршрута |
| `EventTimerComponent.cs` | Таймер до следующей проверки события |
| `ConvoyEventTags.cs` | Tag-компоненты событий: прибыл, кончилась еда, сломался, need check |
| `ConvoyMovementSystem.cs` | Система движения: продвижение по маршруту, прибытие |
| `ConvoyResourceSystem.cs` | Система ресурсов: расход еды, износ, теги OutOfFood/Broken |
| `ConvoyEventPublisherSystem.cs` | Обнаружение event-тегов → публикация в EventBus |
| `EventTriggerSystem.cs` | Случайный триггер событий ивентов на караванах |
| `EventTimerSystem.cs` | Обратный отсчёт EventTimerComponent, добавление EventCheckTag |
| `ConvoyVisualizer.cs` | Система визуализации: обновление позиции GameObject по ECS-данным |
| `ECSSerializer.cs` | Сериализация ECS-сущностей для save/load |

### Data — ScriptableObject конфиги
| Файл | Описание |
|------|----------|
| `GameConfig.cs` | Центральный баланс: стартовые ресурсы, экономика, бой, события, ECS |
| `HexTileConfig.cs` | Настройки тайлов рельефа, шума, layout сетки |
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
| `HexPathfinder.cs` | A* по hex-сетке с обходом воды |
| `SerializableDictionary.cs` | Dictionary, сериализуемый Unity + JSON |
| `UnityMathContractResolver.cs` | Newtonsoft.Json resolver: пропуск Unity.Mathematics при сериализации |

### Editor
| Файл | Описание |
|------|----------|
| `SetupPrototype.cs` | Утилита Editor Menu для быстрого создания всех конфигов и тайлов в сцене |
