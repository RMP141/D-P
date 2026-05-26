using UnityEngine;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "EventData", menuName = "ConvoyManager/Event Data")]
    public class EventDataSO : ScriptableObject
    {
        public string Title;
        [TextArea] public string Description;
        public EventOption[] Options;
    }

    [System.Serializable]
    public struct EventOption
    {
        public string ButtonText;
        public EventEffect[] Effects;
    }

    [System.Serializable]
    public struct EventEffect
    {
        public EffectType Type;
        public int Value;      // ”ниверсальное число (золото, количество, сила врага)
        public int ItemId;     // ID товара или фракции (дл€ ChangeReputation, AddItem и т.д.)
    }

    public enum EffectType
    {
        None,
        AddGold,
        RemoveGold,
        AddMercenaries,
        RemoveMercenaries,
        AddItem,
        RemoveItem,
        ChangeReputation,
        DamageCart,
        RepairCart,
        Combat
    }
}