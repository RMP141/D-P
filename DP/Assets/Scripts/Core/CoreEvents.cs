using ConvoyManager.Data;
using Unity.Entities;

namespace ConvoyManager.Core
{
    // ---------- ��������� ���� ���� ----------
    public readonly struct GameStartedEvent { }
    public readonly struct GameLoadedEvent { }

    // ---------- ��� � �������� ������ ----------
    public readonly struct HexDiscoveredEvent
    {
        public readonly int HexIndex;
        public HexDiscoveredEvent(int hexIndex) => HexIndex = hexIndex;
    }

    // ---------- ECS-������� ��������� ----------
    public readonly struct ConvoyArrivedEvent
    {
        public readonly Entity ConvoyEntity;
        public ConvoyArrivedEvent(Entity entity) => ConvoyEntity = entity;
    }

    public readonly struct OutOfFoodEvent
    {
        public readonly Entity ConvoyEntity;
        public OutOfFoodEvent(Entity entity) => ConvoyEntity = entity;
    }

    public readonly struct BrokenEvent
    {
        public readonly Entity ConvoyEntity;
        public BrokenEvent(Entity entity) => ConvoyEntity = entity;
    }

    // ---------- ��������� ������� (�������) ----------
    public readonly struct EventTriggeredMessage
    {
        public readonly Entity ConvoyEntity;
        public readonly EventDataSO EventData;
        public EventTriggeredMessage(Entity entity, EventDataSO data)
        {
            ConvoyEntity = entity;
            EventData = data;
        }
    }

    // ---------- ������������� ������� ----------
    public readonly struct PriceUpdatedEvent
    {
        public readonly int ItemID;
        public readonly int CityIndex;
        public readonly float NewPrice;
        public PriceUpdatedEvent(int itemId, int cityIndex, float newPrice)
        {
            ItemID = itemId;
            CityIndex = cityIndex;
            NewPrice = newPrice;
        }
    }

    public readonly struct EconomicEventAppliedEvent
    {
        public readonly EconomicEventSO EventData;
        public EconomicEventAppliedEvent(EconomicEventSO eventData) => EventData = eventData;
    }

    // ---------- ������ ������� � ������� ----------
    public readonly struct CombatResolvedEvent
    {
        public readonly Combat.CombatResult Result;
        public readonly int EnemyPower;
        public CombatResolvedEvent(Combat.CombatResult result, int enemyPower)
        {
            Result = result;
            EnemyPower = enemyPower;
        }
    }

    public readonly struct DamageCartEvent
    {
        public readonly int DamageAmount;
        public DamageCartEvent(int damage) => DamageAmount = damage;
    }

    public readonly struct RepairCartEvent
    {
        public readonly int RepairAmount;
        public RepairCartEvent(int repair) => RepairAmount = repair;
    }

    // ---------- UI-������� ----------
    public readonly struct ShowScreenEvent
    {
        public readonly string ScreenName;
        public ShowScreenEvent(string name) => ScreenName = name;
    }

    public readonly struct ConvoyCreatedEvent
    {
        public readonly Entity ConvoyEntity;
        public ConvoyCreatedEvent(Entity entity) => ConvoyEntity = entity;
    }

    public readonly struct EventResolvedEvent
    {
        public readonly string EventTitle;
        public readonly int Choice;
        public EventResolvedEvent(string title, int choice)
        {
            EventTitle = title;
            Choice = choice;
        }
    }

    public readonly struct ShowToastRequest
    {
        public readonly string Message;
        public readonly float Duration;
        public ShowToastRequest(string message, float duration = 4f)
        {
            Message = message;
            Duration = duration;
        }
    }
}