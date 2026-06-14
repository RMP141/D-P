using System;
using System.IO;
using System.Linq;
using ConvoyManager.Combat;
using ConvoyManager.Economy;
using ConvoyManager.ECS;
using ConvoyManager.Player;
using ConvoyManager.Utils;
using ConvoyManager.World;
using Newtonsoft.Json;
using UnityEngine;

namespace ConvoyManager.Core
{
    public struct SlotMetaData
    {
        public string Date;
        public int Discovered;
        public int Total;
        public bool IsEmpty => string.IsNullOrEmpty(Date);
    }

    public interface ISaveSystem
    {
        SlotMetaData GetSlotMeta(int slot);
        bool HasSlot(int slot);
        int GetLastUsedSlot();
        void SaveGame(int slot);
        void LoadGame(int slot);
        void DeleteGame(int slot);
    }

    public class SaveSystem : ISaveSystem
    {
        private const int SlotCount = 3;
        private const string SaveFileTemplate = "save_{0}.json";
        private const string MetaFileTemplate = "save_{0}.meta";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new Utils.UnityMathContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private readonly IWorldState _worldState;
        private readonly IEconomyEngine _economyEngine;
        private readonly IPlayerProgress _playerProgress;
        private readonly ECSSerializer _ecsSerializer;
        private readonly EventBus _eventBus;
        private readonly ICaptainCollection _captainCollection;
        private readonly CaptainGacha _captainGacha;

        public SaveSystem(
            IWorldState worldState,
            IEconomyEngine economyEngine,
            IPlayerProgress playerProgress,
            ECSSerializer ecsSerializer,
            EventBus eventBus,
            ICaptainCollection captainCollection,
            CaptainGacha captainGacha)
        {
            _worldState = worldState;
            _economyEngine = economyEngine;
            _playerProgress = playerProgress;
            _ecsSerializer = ecsSerializer;
            _eventBus = eventBus;
            _captainCollection = captainCollection;
            _captainGacha = captainGacha;
        }

        public SlotMetaData GetSlotMeta(int slot)
        {
            var metaPath = GetMetaPath(slot);
            if (File.Exists(metaPath))
            {
                string json = File.ReadAllText(metaPath);
                var meta = JsonConvert.DeserializeObject<SlotMetaData>(json);
                if (!meta.IsEmpty)
                    return meta;
            }

            if (HasSlot(slot))
            {
                string saveJson = File.ReadAllText(GetSavePath(slot));
                var data = JsonConvert.DeserializeObject<SaveData>(saveJson);
                return new SlotMetaData
                {
                    Date = "unknown",
                    Discovered = data.WorldState.Hexes.Count(h => h.IsDiscovered),
                    Total = data.WorldState.Hexes.Length
                };
            }

            return new SlotMetaData();
        }

        public bool HasSlot(int slot) => File.Exists(GetSavePath(slot));

        public int GetLastUsedSlot()
        {
            int lastSlot = -1;
            DateTime lastTime = DateTime.MinValue;
            for (int i = 1; i <= 3; i++)
            {
                var path = GetSavePath(i);
                if (File.Exists(path))
                {
                    var time = File.GetLastWriteTime(path);
                    if (time > lastTime)
                    {
                        lastTime = time;
                        lastSlot = i;
                    }
                }
            }
            return lastSlot;
        }

        public void SaveGame(int slot)
        {
            var saveData = new SaveData
            {
                WorldState = WorldStateData.FromWorldState(_worldState),
                DynamicModifiers = new SerializableDictionary<int, float>(_economyEngine.GetAllModifiers()),
                ActiveEconomicEvents = _economyEngine.GetActiveEventsData(),
                CaptainIds = _captainCollection.GetAllCaptainIds(),
                ActiveCaptainId = _captainCollection.GetActiveCaptainId(),
                PlayerProgress = new PlayerProgressData
                {
                    Gold = _playerProgress.Gold,
                    MercenaryCount = _playerProgress.MercenaryCount,
                    CartCount = _playerProgress.CartCount,
                    MaxConvoys = _playerProgress.MaxConvoys,
                    Reputation = new SerializableDictionary<int, int>(
                        _playerProgress.Reputation.ToDictionary(kvp => (int)kvp.Key, kvp => kvp.Value)),
                    Inventory = new SerializableDictionary<int, int>(_playerProgress.Inventory)
                },
                ECSData = _ecsSerializer.Save()
            };

            string json = JsonConvert.SerializeObject(saveData, JsonSettings);
            File.WriteAllText(GetSavePath(slot), json);

            int discovered = saveData.WorldState.Hexes.Count(h => h.IsDiscovered);
            var meta = new SlotMetaData
            {
                Date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Discovered = discovered,
                Total = saveData.WorldState.Hexes.Length
            };
            File.WriteAllText(GetMetaPath(slot), JsonConvert.SerializeObject(meta));

            Debug.Log($"[SaveSystem] Saved {discovered}/{saveData.WorldState.Hexes.Length} to slot {slot}");
        }

        public void LoadGame(int slot)
        {
            if (!HasSlot(slot)) return;

            string json = File.ReadAllText(GetSavePath(slot));
            var saveData = JsonConvert.DeserializeObject<SaveData>(json);

            _worldState.LoadFrom(saveData.WorldState);
            _economyEngine.Initialize(_worldState);
            _economyEngine.SetAllModifiers(saveData.DynamicModifiers);
            if (saveData.ActiveEconomicEvents != null)
                _economyEngine.SetActiveEventsData(saveData.ActiveEconomicEvents);

            var pp = saveData.PlayerProgress;
            _playerProgress.Restore(
                pp.Gold,
                pp.MercenaryCount,
                pp.CartCount,
                pp.MaxConvoys,
                pp.Reputation.ToDictionary(kvp => (Faction)kvp.Key, kvp => kvp.Value),
                pp.Inventory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );

            _captainCollection.Clear();
            if (saveData.CaptainIds != null)
            {
                foreach (var id in saveData.CaptainIds)
                {
                    var captain = _captainGacha.GetCaptainByID(id);
                    if (captain != null)
                        _captainCollection.AddCaptain(captain);
                }
                if (saveData.ActiveCaptainId >= 0)
                    _captainCollection.SetActiveCaptain(saveData.ActiveCaptainId);
            }

            if (saveData.ECSData != null)
                _ecsSerializer.Load(saveData.ECSData);

            int discovered = saveData.WorldState.Hexes.Count(h => h.IsDiscovered);
            Debug.Log($"[SaveSystem] Loaded {discovered}/{saveData.WorldState.Hexes.Length} from slot {slot}");
        }

        public void DeleteGame(int slot)
        {
            string savePath = GetSavePath(slot);
            string metaPath = GetMetaPath(slot);
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[SaveSystem] Deleted save slot {slot}");
            }
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        private string GetSavePath(int slot) => Path.Combine(Application.persistentDataPath, string.Format(SaveFileTemplate, slot));
        private string GetMetaPath(int slot) => Path.Combine(Application.persistentDataPath, string.Format(MetaFileTemplate, slot));
    }

    [System.Serializable]
    public class SaveData
    {
        public WorldStateData WorldState;
        public SerializableDictionary<int, float> DynamicModifiers;
        public ActiveEconomicEventData[] ActiveEconomicEvents;
        public int[] CaptainIds;
        public int ActiveCaptainId = -1;
        public PlayerProgressData PlayerProgress;
        public ECSData ECSData;
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
