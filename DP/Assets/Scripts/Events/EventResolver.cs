using ConvoyManager.Combat;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Player;
using ConvoyManager.Utils;
using ConvoyManager.World;
using UnityEngine;

namespace ConvoyManager.Events
{
    public class EventResolver
    {
        private readonly IPlayerProgress _playerProgress;
        private readonly EventBus _eventBus;
        private readonly ICombatStrategy _combatStrategy;
        private readonly IRandomGenerator _random;
        private readonly ICaptainCollection _captainCollection;

        public EventResolver(
            IPlayerProgress playerProgress,
            EventBus eventBus,
            ICombatStrategy combatStrategy,
            IRandomGenerator random,
            ICaptainCollection captainCollection)
        {
            _playerProgress = playerProgress;
            _eventBus = eventBus;
            _combatStrategy = combatStrategy;
            _random = random;
            _captainCollection = captainCollection;
        }

        public void Resolve(EventDataSO eventData, int optionIndex)
        {
            if (eventData == null || optionIndex < 0 || optionIndex >= eventData.Options.Length)
            {
                Debug.LogError("Invalid event or option index");
                return;
            }

            var option = eventData.Options[optionIndex];
            foreach (var effect in option.Effects)
            {
                ApplyEffect(effect);
            }

            _eventBus.Publish(new EventResolvedEvent(eventData.Title, optionIndex));
        }

        private void ApplyEffect(EventEffect effect)
        {
            switch (effect.Type)
            {
                case EffectType.AddGold:
                    _playerProgress.AddGold(effect.Value);
                    break;
                case EffectType.RemoveGold:
                    _playerProgress.SpendGold(effect.Value);
                    break;
                case EffectType.AddMercenaries:
                    _playerProgress.AddMercenaries(effect.Value);
                    break;
                case EffectType.RemoveMercenaries:
                    _playerProgress.RemoveMercenaries(effect.Value);
                    break;
                case EffectType.AddItem:
                    _playerProgress.AddItem(effect.ItemId, effect.Value);
                    break;
                case EffectType.RemoveItem:
                    _playerProgress.RemoveItem(effect.ItemId, effect.Value);
                    break;
                case EffectType.ChangeReputation:
                    var faction = (Faction)effect.ItemId;
                    _playerProgress.ChangeReputation(faction, effect.Value);
                    break;
                case EffectType.DamageCart:
                    _eventBus.Publish(new DamageCartEvent(effect.Value));
                    break;
                case EffectType.RepairCart:
                    _eventBus.Publish(new RepairCartEvent(effect.Value));
                    break;
                case EffectType.Combat:
                    ResolveCombat(effect.Value);
                    break;
            }
        }

        private void ResolveCombat(int enemyPower)
        {
            int mercenaryCount = _playerProgress.MercenaryCount;
            int captainBonus = _captainCollection.ActiveCaptain?.AttackBonus ?? 0;
            var result = _combatStrategy.Resolve(mercenaryCount, captainBonus, enemyPower, _random);

            if (result == CombatResult.Defeat)
            {
                _playerProgress.RemoveMercenaries(3);
                _playerProgress.SpendGold(50);
                _eventBus.Publish(new CombatResolvedEvent(result, enemyPower));
            }
            else
            {
                _playerProgress.AddGold(30);
                _eventBus.Publish(new CombatResolvedEvent(result, enemyPower));
            }
        }
    }
}