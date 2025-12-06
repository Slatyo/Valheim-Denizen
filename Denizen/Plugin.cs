using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using Denizen.Commands;
using Denizen.Core;
using Denizen.Examples;
using Denizen.Integration;

namespace Denizen
{
    /// <summary>
    /// Denizen - Custom Entities, NPCs & Creatures for Valheim.
    /// The complete entity framework for the mod ecosystem.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.slatyo.prime", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.slatyo.vital", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.slatyo.munin", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.slatyo.veneer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.slatyo.spark", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.slatyo.affix", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.slatyo.tome", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>Plugin GUID for BepInEx.</summary>
        public const string PluginGUID = "com.slatyo.denizen";
        /// <summary>Plugin display name.</summary>
        public const string PluginName = "Denizen";
        /// <summary>Plugin version.</summary>
        public const string PluginVersion = "1.0.0";

        /// <summary>Logger instance for Denizen.</summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>Plugin instance.</summary>
        public static Plugin Instance { get; private set; }

        /// <summary>Whether Prime is available.</summary>
        public static bool HasPrime { get; private set; }

        /// <summary>Whether Vital is available.</summary>
        public static bool HasVital { get; private set; }

        /// <summary>Whether Munin is available.</summary>
        public static bool HasMunin { get; private set; }

        /// <summary>Whether Veneer is available.</summary>
        public static bool HasVeneer { get; private set; }

        /// <summary>Whether Spark is available.</summary>
        public static bool HasSpark { get; private set; }

        /// <summary>Whether Affix is available.</summary>
        public static bool HasAffix { get; private set; }

        /// <summary>Whether Tome is available.</summary>
        public static bool HasTome { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // Check for dependencies
            CheckDependencies();

            // Initialize integrations
            PrimeIntegration.Initialize();

            // Register localizations
            AddLocalizations();

            // Register commands
            DenizenCommands.Register();

            // Initialize Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            // Register example entities when prefabs are available
            PrefabManager.OnVanillaPrefabsAvailable += RegisterExampleEntities;

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
        }

        private void OnDestroy()
        {
            PrimeIntegration.Cleanup();
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterExampleEntities;
            DenizenRegistry.Instance.Clear();
            Loot.LootTable.Clear();
            Dialogue.DialogueSystem.Clear();
            _harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Check which optional dependencies are available.
        /// </summary>
        private void CheckDependencies()
        {
            var plugins = BepInEx.Bootstrap.Chainloader.PluginInfos;

            HasPrime = plugins.ContainsKey("com.slatyo.prime");
            HasVital = plugins.ContainsKey("com.slatyo.vital");
            HasMunin = plugins.ContainsKey("com.slatyo.munin");
            HasVeneer = plugins.ContainsKey("com.slatyo.veneer");
            HasSpark = plugins.ContainsKey("com.slatyo.spark");
            HasAffix = plugins.ContainsKey("com.slatyo.affix");
            HasTome = plugins.ContainsKey("com.slatyo.tome");

            Log.LogInfo($"Dependencies: Prime={HasPrime}, Vital={HasVital}, Munin={HasMunin}, " +
                       $"Veneer={HasVeneer}, Spark={HasSpark}, Affix={HasAffix}, Tome={HasTome}");
        }

        /// <summary>
        /// Register example entities.
        /// </summary>
        private void RegisterExampleEntities()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterExampleEntities;

            // Register example entities
            ExampleNPC.Register();
            ExampleEnemy.Register();
            ExampleBoss.Register();
            ExampleCompanion.Register();

            Log.LogInfo($"Registered {DenizenRegistry.Instance.Count} denizens");
        }

        /// <summary>
        /// Add localizations.
        /// </summary>
        private void AddLocalizations()
        {
            var loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", new System.Collections.Generic.Dictionary<string, string>
            {
                // NPC
                { "npc_enchanter_name", "Eldric" },
                { "npc_enchanter_title", "The Enchanter" },
                { "npc_enchanter_greeting", "Ah, a seeker of power! What enchantments do you desire?" },
                { "npc_enchanter_farewell", "May your weapons strike true!" },
                { "npc_enchanter_idle_1", "The runes whisper of great battles to come..." },
                { "npc_enchanter_idle_2", "Power flows through all things..." },

                // Services
                { "service_trade", "Trade" },
                { "service_gamble", "Gamble" },
                { "service_enchant", "Enchant" },

                // Enemies
                { "enemy_frost_golem", "Frost Golem" },
                { "enemy_smart_archer", "Skeleton Archer" },

                // Bosses
                { "boss_frost_lord", "The Frost Lord" },
                { "boss_frost_lord_phase2", "The Frost Lord awakens!" },
                { "boss_frost_lord_phase3", "You will freeze for eternity!" },

                // Companions
                { "companion_battle_wolf", "Battle Wolf" },

                // UI
                { "ui_denizen_health", "Health" },
                { "ui_denizen_level", "Level" },
            });

            loc.AddTranslation("German", new System.Collections.Generic.Dictionary<string, string>
            {
                // NPC
                { "npc_enchanter_name", "Eldric" },
                { "npc_enchanter_title", "Der Verzauberer" },
                { "npc_enchanter_greeting", "Ah, ein Sucher der Macht! Welche Verzauberungen begehrst du?" },
                { "npc_enchanter_farewell", "Mögen deine Waffen wahr treffen!" },
                { "npc_enchanter_idle_1", "Die Runen flüstern von großen Schlachten..." },
                { "npc_enchanter_idle_2", "Macht fließt durch alle Dinge..." },

                // Services
                { "service_trade", "Handeln" },
                { "service_gamble", "Glücksspiel" },
                { "service_enchant", "Verzaubern" },

                // Enemies
                { "enemy_frost_golem", "Frostgolem" },
                { "enemy_smart_archer", "Skelettbogenschütze" },

                // Bosses
                { "boss_frost_lord", "Der Frostlord" },
                { "boss_frost_lord_phase2", "Der Frostlord erwacht!" },
                { "boss_frost_lord_phase3", "Ihr werdet für die Ewigkeit gefrieren!" },

                // Companions
                { "companion_battle_wolf", "Kampfwolf" },

                // UI
                { "ui_denizen_health", "Gesundheit" },
                { "ui_denizen_level", "Stufe" },
            });
        }
    }
}
