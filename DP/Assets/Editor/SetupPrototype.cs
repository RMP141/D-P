using System;
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

        var plainsSprite = CreateTerrainSprite("Assets/Tiles/PlainsTileTex.png",
            new Color(0.3f, 0.6f, 0.3f), DrawPlainsIcon);
        var forestSprite = CreateTerrainSprite("Assets/Tiles/ForestTileTex.png",
            new Color(0.1f, 0.4f, 0.1f), DrawForestIcon);
        var mountainSprite = CreateTerrainSprite("Assets/Tiles/MountainsTileTex.png",
            new Color(0.4f, 0.35f, 0.25f), DrawMountainIcon);
        var waterSprite = CreateTerrainSprite("Assets/Tiles/WaterTileTex.png",
            new Color(0.2f, 0.4f, 0.6f), DrawWaterIcon);
        var fogSprite = CreateOrLoadSprite("Assets/Tiles/FogTileTex.png", new Color(0.15f, 0.15f, 0.15f));

        var citySprite = CreateEmblemSprite("Assets/Tiles/CityTileTex.png",
            new Color(0.83f, 0.63f, 0.09f),   // gold
            DrawCityEmblem);
        var villageSprite = CreateEmblemSprite("Assets/Tiles/VillageTileTex.png",
            new Color(0.36f, 0.71f, 0.75f),   // teal
            DrawVillageEmblem);

        DeleteOldTile("Assets/Tiles/HexTile.asset");

        var plainsTile = CreateTile("Assets/Tiles/PlainsTile.asset", plainsSprite);
        var forestTile = CreateTile("Assets/Tiles/ForestTile.asset", forestSprite);
        var mountainsTile = CreateTile("Assets/Tiles/MountainsTile.asset", mountainSprite);
        var waterTile = CreateTile("Assets/Tiles/WaterTile.asset", waterSprite);
        var fogTile = CreateTile("Assets/Tiles/FogTile.asset", fogSprite);
        var cityTile = CreateTile("Assets/Tiles/CityTile.asset", citySprite);
        var villageTile = CreateTile("Assets/Tiles/VillageTile.asset", villageSprite);

        if (hexConfig != null)
        {
            hexConfig.PlainsTile = plainsTile;
            hexConfig.ForestTile = forestTile;
            hexConfig.MountainsTile = mountainsTile;
            hexConfig.WaterTile = waterTile;
            hexConfig.FogTile = fogTile;
            hexConfig.CityTile = cityTile;
            hexConfig.VillageTile = villageTile;
            EditorUtility.SetDirty(hexConfig);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created: PlainsTile, ForestTile, MountainsTile, WaterTile, FogTile, CityTile, VillageTile");
    }

    private static Sprite CreateEmblemSprite(string path, Color fill, System.Action<Texture2D> drawEmblem)
    {
        if (!System.IO.File.Exists(path))
        {
            var tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    tex.SetPixel(x, y, fill);
            drawEmblem(tex);
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

    private static void DrawCityEmblem(Texture2D tex)
    {
        // Castle / tower shape: a square with a cross on top
        int cx = 32, cy = 32;
        Color emblemColor = Color.white;

        // Main tower body: 7x9 rectangle centered
        for (int x = cx - 3; x <= cx + 3; x++)
            for (int y = cy - 4; y <= cy + 4; y++)
                tex.SetPixel(x, y, emblemColor);

        // Battlements (3 small squares on top)
        for (int x = cx - 5; x <= cx - 3; x++)
            for (int y = cy + 5; y <= cy + 7; y++)
                tex.SetPixel(x, y, emblemColor);
        for (int x = cx - 1; x <= cx + 1; x++)
            for (int y = cy + 5; y <= cy + 7; y++)
                tex.SetPixel(x, y, emblemColor);
        for (int x = cx + 3; x <= cx + 5; x++)
            for (int y = cy + 5; y <= cy + 7; y++)
                tex.SetPixel(x, y, emblemColor);

        // Door: dark rectangle at bottom center
        Color doorColor = new Color(0.5f, 0.3f, 0f);
        for (int x = cx - 1; x <= cx + 1; x++)
            for (int y = cy - 4; y <= cy - 2; y++)
                tex.SetPixel(x, y, doorColor);
    }

    private static void DrawVillageEmblem(Texture2D tex)
    {
        // House shape: triangle roof on square body
        int cx = 32, cy = 32;
        Color emblemColor = Color.white;

        // House body: 7x7 square
        for (int x = cx - 3; x <= cx + 3; x++)
            for (int y = cy - 3; y <= cy + 3; y++)
                tex.SetPixel(x, y, emblemColor);

        // Roof: triangle above the body
        for (int dy = 0; dy <= 4; dy++)
        {
            int halfWidth = 4 - dy;
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
                tex.SetPixel(cx + dx, cy + 4 + dy, emblemColor);
        }

        // Door: dark rectangle at bottom center
        Color doorColor = new Color(0.3f, 0.5f, 0.5f);
        for (int x = cx - 1; x <= cx + 1; x++)
            for (int y = cy - 3; y <= cy - 1; y++)
                tex.SetPixel(x, y, doorColor);
    }

    private static Sprite CreateTerrainSprite(string path, Color fill, System.Action<Texture2D> drawIcon)
    {
        // Always delete old texture to force regeneration with icon
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            System.IO.File.Delete(path);
        }

        var tex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tex.SetPixel(x, y, fill);
        drawIcon(tex);
        tex.Apply();
        var png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path);

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

    private static void DrawPlainsIcon(Texture2D tex)
    {
        // Three grass blades
        Color iconColor = new Color(0.5f, 0.8f, 0.3f);
        int cx = 32, cy = 32;
        for (int i = -1; i <= 1; i++)
        {
            int bx = cx + i * 8;
            for (int h = 0; h < 8; h++)
                tex.SetPixel(bx, cy + h, iconColor);
            // blade tip
            tex.SetPixel(bx - 1, cy + 8, iconColor);
            tex.SetPixel(bx + 1, cy + 8, iconColor);
        }
    }

    private static void DrawForestIcon(Texture2D tex)
    {
        // Pine tree: trunk below, wide-to-narrow foliage above
        Color trunkColor = new Color(0.3f, 0.2f, 0.1f);
        Color leafColor = new Color(0.0f, 0.35f, 0.0f);
        int cx = 32;

        // Trunk: 3px wide, 6px tall at the bottom
        for (int x = cx - 1; x <= cx + 1; x++)
            for (int y = 20; y <= 25; y++)
                tex.SetPixel(x, y, trunkColor);

        // Foliage: three stacked triangles above trunk
        for (int layer = 0; layer < 3; layer++)
        {
            int baseY = 27 + layer * 5;  // bottom → top: 27, 32, 37
            int width = 9 - layer * 2;    // bottom → top: 9, 7, 5
            for (int dx = -width / 2; dx <= width / 2; dx++)
                for (int dy = 0; dy <= 3; dy++)
                    tex.SetPixel(cx + dx, baseY + dy, leafColor);
        }
    }

    private static void DrawMountainIcon(Texture2D tex)
    {
        // Mountain peak triangle with snow cap
        Color snowColor = Color.white;
        Color rockColor = new Color(0.5f, 0.45f, 0.35f);
        int cx = 32, cy = 22;

        // Main mountain body (triangle)
        for (int dy = 0; dy <= 16; dy++)
        {
            int halfW = 10 - dy / 2;
            for (int dx = -halfW; dx <= halfW; dx++)
                tex.SetPixel(cx + dx, cy + dy, rockColor);
        }

        // Snow cap (top 4 rows)
        for (int dy = 0; dy <= 4; dy++)
        {
            int halfW = 10 - dy / 2;
            for (int dx = -halfW; dx <= halfW; dx++)
                tex.SetPixel(cx + dx, cy + dy, snowColor);
        }

        // Snow streaks down the sides
        for (int s = 0; s < 2; s++)
        {
            int sx = s == 0 ? cx - 5 : cx + 5;
            for (int dy = 5; dy <= 10; dy += 2)
                tex.SetPixel(sx, cy + dy, snowColor);
        }
    }

    private static void DrawWaterIcon(Texture2D tex)
    {
        // Three horizontal wave lines
        Color waveColor = new Color(0.5f, 0.7f, 0.9f);
        int cx = 32, cy = 32;

        for (int row = -2; row <= 2; row++)
        {
            int wy = cy + row * 5;
            for (int x = cx - 12; x <= cx + 12; x++)
            {
                int offset = (int)(Math.Sin((x - cx) * 0.5f + row) * 1.5f);
                tex.SetPixel(x, wy + offset, waveColor);
            }
        }
    }

    private static Tile CreateTile(string path, Sprite sprite)
    {
        DeleteOldTile(path);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
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
