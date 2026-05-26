using System.IO;
using System.Linq;
using ConvoyManager.Economy;
using ConvoyManager.Player;
using ConvoyManager.Utils;
using ConvoyManager.World;
using Newtonsoft.Json;
using UnityEngine;

namespace ConvoyManager.Core
{
    public interface ISaveSystem
    {
        bool HasSave();
        void SaveGame();
        void LoadGame();
    }

    /// <summary>
    /// Отвечает за сохранение и загрузку полного состояния игры в JSON.
    /// </summary>
    public class SaveSystem : ISaveSystem
    {
        private const string SaveFileName = "save.json";

        private readonly IWorldState _worldState;
        private readonly IEconomyEngine _economyEngine;
        private readonly IPlayerProgress _playerProgress;
        private readonly EventBus _eventBus;

        public SaveSystem(
            IWorldState worldState,
            IEconomyEngine economyEngine,
            IPlayerProgress playerProgress,
            EventBus eventBus)
        {
            _worldState = worldState;
            _economyEngine = economyEngine;
            _playerProgress = playerProgress;
            _eventBus = eventBus;
        }

        public bool HasSave() => File.Exists(GetSavePath());

        public void SaveGame()
        {
            var saveData = new SaveData
            {
                WorldState = WorldStateData.FromWorldState(_worldState),
                DynamicModifiers = new SerializableDictionary<int, float>(_economyEngine.GetAllModifiers()),
                PlayerProgress = new PlayerProgressData
                {
                    Gold = _playerProgress.Gold,
                    MercenaryCount = _playerProgress.MercenaryCount,
                    CartCount = _playerProgress.CartCount,
                    MaxConvoys = _playerProgress.MaxConvoys,
                    Reputation = new SerializableDictionary<int, int>(
                        _playerProgress.Reputation.ToDictionary(kvp => (int)kvp.Key, kvp => kvp.Value)),
                    Inventory = new SerializableDictionary<int, int>(_playerProgress.Inventory)
                }
            };

            string json = JsonConvert.SerializeObject(saveData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(GetSavePath(), json);
            Debug.Log($"[SaveSystem] Game saved to {GetSavePath()}");
        }

        public void LoadGame()
        {
            if (!HasSave()) return;

            string json = File.ReadAllText(GetSavePath());
            var saveData = JsonConvert.DeserializeObject<SaveData>(json);

            _worldState.LoadFrom(saveData.WorldState);
            _economyEngine.Initialize(_worldState);
            _economyEngine.SetAllModifiers(saveData.DynamicModifiers);

            var pp = saveData.PlayerProgress;
            _playerProgress.Restore(
                pp.Gold,
                pp.MercenaryCount,
                pp.CartCount,
                pp.MaxConvoys,
                pp.Reputation.ToDictionary(kvp => (Faction)kvp.Key, kvp => kvp.Value),
                pp.Inventory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );

            Debug.Log("[SaveSystem] Game loaded.");
        }

        private string GetSavePath() => Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    [System.Serializable]
    public class SaveData
    {
        public WorldStateData WorldState;
        public SerializableDictionary<int, float> DynamicModifiers;
        public PlayerProgressData PlayerProgress;
    }

    [System.Serializable]
    public class PlayerProgressData
    {
        public int Gold;
        public int MercenaryCount;
        public int CartCount;
        public int MaxConvoys;
        public SerializableDictionary<int, int> Reputation;
        public SerializableDictionary<int, int> Inventory;
    }
}