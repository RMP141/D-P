using ConvoyManager.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SetupPrototype
{
    [MenuItem("ConvoyManager/Create All Assets")]
    public static void CreateAllAssets()
    {
        CreateConfigs();
        CreatePanelSettings();
        CreateTiles();
        CreateEvents();
        CreateCaptains();
        Debug.Log("=== All assets created. Now set up the scene per ConvoyManager > Show Scene Guide ===");
    }

    [MenuItem("ConvoyManager/Create Config Assets")]
    public static void CreateConfigs()
    {
        EnsureFolder("Assets/Settings");

        if (!AssetExists("Assets/Settings/GameConfig.asset"))
        {
            var gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(gameConfig, "Assets/Settings/GameConfig.asset");
            Debug.Log("Created: GameConfig.asset");
        }

        if (!AssetExists("Assets/Settings/HexTileConfig.asset"))
        {
            var hexConfig = ScriptableObject.CreateInstance<HexTileConfig>();
            AssetDatabase.CreateAsset(hexConfig, "Assets/Settings/HexTileConfig.asset");
            Debug.Log("Created: HexTileConfig.asset");
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("ConvoyManager/Create PanelSettings")]
    public static void CreatePanelSettings()
    {
        if (AssetExists("Assets/DefaultPanelSettings.asset"))
        {
            Debug.Log("DefaultPanelSettings already exists");
            return;
        }

        var panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
        AssetDatabase.CreateAsset(panelSettings, "Assets/DefaultPanelSettings.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Created: DefaultPanelSettings.asset (default settings)");
    }

    [MenuItem("ConvoyManager/Create Tile Assets")]
    public static void CreateTiles()
    {
        EnsureFolder("Assets/Tiles");

        var hexConfig = AssetDatabase.LoadAssetAtPath<HexTileConfig>("Assets/Settings/HexTileConfig.asset");

        var hexSprite = CreateOrLoadSprite("Assets/Tiles/HexTileTex.png", new Color(0.3f, 0.6f, 0.3f));
        var fogSprite = CreateOrLoadSprite("Assets/Tiles/FogTileTex.png", new Color(0.15f, 0.15f, 0.15f));

        DeleteOldTile("Assets/Tiles/HexTile.asset");
        var hexTile = ScriptableObject.CreateInstance<Tile>();
        hexTile.sprite = hexSprite;
        AssetDatabase.CreateAsset(hexTile, "Assets/Tiles/HexTile.asset");

        DeleteOldTile("Assets/Tiles/FogTile.asset");
        var fogTile = ScriptableObject.CreateInstance<Tile>();
        fogTile.sprite = fogSprite;
        AssetDatabase.CreateAsset(fogTile, "Assets/Tiles/FogTile.asset");

        if (hexConfig != null)
        {
            hexConfig.HexTile = hexTile;
            hexConfig.FogTile = fogTile;
            EditorUtility.SetDirty(hexConfig);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created: HexTile.asset + FogTile.asset with persistent sprites");
    }

    private static void DeleteOldTile(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    private static Sprite CreateOrLoadSprite(string path, Color fill)
    {
        if (!System.IO.File.Exists(path))
        {
            var tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, fill);
            tex.Apply();
            var png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("ConvoyManager/Create Event Assets")]
    public static void CreateEvents()
    {
        EnsureFolder("Assets/Resources/Events/EventData");
        EnsureFolder("Assets/Resources/Events/EconomicEvents");

        CreateEventData("BanditAmbush", "Bandit Ambush", "A group of bandits blocks the road ahead.",
            new EventOption
            {
                ButtonText = "Fight (50g)",
                Effects = new[] { new EventEffect { Type = EffectType.Combat, Value = 20 } }
            },
            new EventOption
            {
                ButtonText = "Bribe (100g)",
                Effects = new[] { new EventEffect { Type = EffectType.RemoveGold, Value = 100 } }
            },
            new EventOption
            {
                ButtonText = "Retreat",
                Effects = new[] { new EventEffect { Type = EffectType.DamageCart, Value = 10 } }
            });

        CreateEventData("MerchantOffer", "Wandering Merchant", "A merchant offers a rare deal.",
            new EventOption
            {
                ButtonText = "Buy Wood (50g)",
                Effects = new[] { new EventEffect { Type = EffectType.RemoveGold, Value = 50 }, new EventEffect { Type = EffectType.AddItem, Value = 5, ItemId = 1 } }
            },
            new EventOption
            {
                ButtonText = "Buy Iron (100g)",
                Effects = new[] { new EventEffect { Type = EffectType.RemoveGold, Value = 100 }, new EventEffect { Type = EffectType.AddItem, Value = 3, ItemId = 2 } }
            },
            new EventOption
            {
                ButtonText = "Ignore",
                Effects = new EventEffect[0]
            });

        CreateEventData("BrokenWheel", "Broken Wheel", "A wheel on your cart has cracked.",
            new EventOption
            {
                ButtonText = "Repair (30g)",
                Effects = new[] { new EventEffect { Type = EffectType.RemoveGold, Value = 30 }, new EventEffect { Type = EffectType.RepairCart, Value = 20 } }
            },
            new EventOption
            {
                ButtonText = "Fix it yourself",
                Effects = new[] { new EventEffect { Type = EffectType.DamageCart, Value = 5 } }
            });

        CreateEconomicEvent("HarvestFestival", "Harvest Festival", "A bountiful harvest floods the market with food.",
            5f, 0.7f, ItemCategory.Food);

        CreateEconomicEvent("MineCollapse", "Mine Collapse", "A local mine collapses, metal prices soar.",
            4f, 1.8f, ItemCategory.Metal);

        AssetDatabase.SaveAssets();
        Debug.Log("Created: event assets");
    }

    private static void CreateEventData(string fileName, string title, string desc, params EventOption[] options)
    {
        string path = $"Assets/Resources/Events/EventData/{fileName}.asset";
        if (AssetExists(path)) return;

        var evt = ScriptableObject.CreateInstance<EventDataSO>();
        evt.Title = title;
        evt.Description = desc;
        evt.Options = options;
        AssetDatabase.CreateAsset(evt, path);
    }

    private static void CreateEconomicEvent(string fileName, string title, string desc, float duration, float multiplier, ItemCategory category)
    {
        string path = $"Assets/Resources/Events/EconomicEvents/{fileName}.asset";
        if (AssetExists(path)) return;

        var evt = ScriptableObject.CreateInstance<EconomicEventSO>();
        evt.Title = title;
        evt.Description = desc;
        evt.DurationDays = duration;
        evt.PriceMultiplier = multiplier;
        evt.AffectedCategories = new[] { category };
        AssetDatabase.CreateAsset(evt, path);
    }

    [MenuItem("ConvoyManager/Show Scene Guide")]
    public static void ShowSceneGuide()
    {
        EditorUtility.DisplayDialog(
            "Prototype Scene Setup",
            "1. Create GameObject named 'GameManager'\n" +
            "   - Add Component: GameManager (Script)\n" +
            "   - Inspector → _gameConfig ← GameConfig.asset\n" +
            "   - Inspector → _hexTileConfig ← HexTileConfig.asset\n\n" +
            "2. Create child of GameManager named 'Grid'\n" +
            "   - Add Component: Grid\n" +
            "     Cell Layout: Rectangle | Cell Size: (1,1,0)\n" +
            "   - Add Component: HexGridGenerator\n\n" +
            "3. Create child of Grid named 'Tilemap'\n" +
            "   - Add Component: Tilemap\n" +
            "   - Add Component: TilemapRenderer\n\n" +
            "4. Create GameObject named 'UI' (separate, not child)\n" +
            "   - Add Component: UIDocument\n" +
            "   - Panel Settings ← DefaultPanelSettings.asset\n" +
            "   - Source Asset ← (leave empty)\n\n" +
            "5. Save scene. Press Play.",
            "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    [MenuItem("ConvoyManager/Create Captain Assets")]
    public static void CreateCaptains()
    {
        EnsureFolder("Assets/Resources/Captains");

        CreateCaptain("Scout", 1, Rarity.Common, 2, 1);
        CreateCaptain("Guard", 2, Rarity.Common, 1, 3);
        CreateCaptain("Warrior", 3, Rarity.Rare, 5, 3);
        CreateCaptain("Shieldbearer", 4, Rarity.Rare, 2, 6);
        CreateCaptain("Knight", 5, Rarity.Epic, 8, 5);
        CreateCaptain("Strategist", 6, Rarity.Epic, 4, 10);
        CreateCaptain("Dragonborn", 7, Rarity.Legendary, 12, 8);

        AssetDatabase.SaveAssets();
        Debug.Log("Created: captain assets");
    }

    private static void CreateCaptain(string name, int id, Rarity rarity, int attack, int defense)
    {
        string path = $"Assets/Resources/Captains/{name}.asset";
        if (AssetExists(path)) return;

        var captain = ScriptableObject.CreateInstance<CaptainDataSO>();
        captain.ID = id;
        captain.Name = name;
        captain.Rarity = rarity;
        captain.AttackBonus = attack;
        captain.DefenseBonus = defense;
        AssetDatabase.CreateAsset(captain, path);
    }

    private static bool AssetExists(string path)
    {
        return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null;
    }
}
