using System.Collections.Generic;
using Denizen.Integration;
using UnityEngine;

namespace Denizen.Loot
{
    /// <summary>
    /// Loot table system for creature drops.
    /// </summary>
    public static class LootTable
    {
        private static readonly Dictionary<string, LootTableConfig> _tables = new();

        /// <summary>
        /// Register a loot table for a creature.
        /// </summary>
        public static void Register(string creatureId, LootTableConfig config)
        {
            config.Id = creatureId;
            _tables[creatureId] = config;
            Plugin.Log.LogInfo($"Registered loot table: {creatureId}");
        }

        /// <summary>
        /// Check if a creature has a custom loot table.
        /// </summary>
        public static bool Has(string creatureId)
        {
            return _tables.ContainsKey(creatureId);
        }

        /// <summary>
        /// Get a loot table config.
        /// </summary>
        public static LootTableConfig Get(string creatureId)
        {
            return _tables.TryGetValue(creatureId, out var config) ? config : null;
        }

        /// <summary>
        /// Roll drops from a creature's loot table.
        /// </summary>
        public static List<LootResult> Roll(string creatureId, int playerCount = 1)
        {
            var results = new List<LootResult>();

            if (!_tables.TryGetValue(creatureId, out var config))
            {
                return results;
            }

            // Handle inheritance
            if (!string.IsNullOrEmpty(config.Inherits) && _tables.TryGetValue(config.Inherits, out var parentConfig))
            {
                results.AddRange(Roll(config.Inherits, playerCount));
            }

            // Calculate number of drops
            float bonusDrops = (playerCount - 1) * config.BonusDropsPerPlayer;
            int totalDrops = Mathf.RoundToInt(Random.Range(config.MinDrops, config.MaxDrops + 1) + bonusDrops);

            // Build weighted entry list
            var weightedEntries = new List<(LootEntry entry, float weight)>();
            float totalWeight = 0f;

            foreach (var entry in config.Entries)
            {
                weightedEntries.Add((entry, entry.Chance));
                totalWeight += entry.Chance;
            }

            // Roll for each drop
            for (int i = 0; i < totalDrops; i++)
            {
                var entry = RollEntry(weightedEntries, totalWeight);
                if (entry != null)
                {
                    var result = new LootResult
                    {
                        ItemName = entry.Item,
                        Count = Random.Range(entry.Min, entry.Max + 1),
                        HasAffixes = entry.AlwaysAffix || entry.Rarity != null,
                        AffixRarity = entry.Rarity?.Roll() ?? Rarity.Common
                    };
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Roll a single entry from weighted list.
        /// </summary>
        private static LootEntry RollEntry(List<(LootEntry entry, float weight)> entries, float totalWeight)
        {
            if (entries.Count == 0 || totalWeight <= 0)
                return null;

            float roll = Random.value * totalWeight;
            float cumulative = 0f;

            foreach (var (entry, weight) in entries)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    return entry;
                }
            }

            return entries[entries.Count - 1].entry;
        }

        /// <summary>
        /// Clear all registered loot tables.
        /// </summary>
        public static void Clear()
        {
            _tables.Clear();
        }

        /// <summary>
        /// Get all registered table IDs.
        /// </summary>
        public static IEnumerable<string> GetAllIds()
        {
            return _tables.Keys;
        }

        /// <summary>
        /// Roll and spawn loot at a position.
        /// </summary>
        public static List<GameObject> RollAndSpawn(string creatureId, Vector3 position, int playerCount = 1)
        {
            var results = Roll(creatureId, playerCount);
            if (results.Count == 0)
                return new List<GameObject>();

            // Use Affix integration to spawn loot with affixes
            return AffixIntegration.SpawnLoot(results, position);
        }
    }

    /// <summary>
    /// Configuration for a loot table.
    /// </summary>
    public class LootTableConfig
    {
        /// <summary>Unique ID for this table.</summary>
        public string Id { get; set; }

        /// <summary>ID of table to inherit from.</summary>
        public string Inherits { get; set; }

        /// <summary>Minimum drops per kill.</summary>
        public int MinDrops { get; set; } = 1;

        /// <summary>Maximum drops per kill.</summary>
        public int MaxDrops { get; set; } = 3;

        /// <summary>Bonus drops per additional player.</summary>
        public float BonusDropsPerPlayer { get; set; } = 0.5f;

        /// <summary>Loot entries in this table.</summary>
        public List<LootEntry> Entries { get; set; } = new();
    }

    /// <summary>
    /// A single entry in a loot table.
    /// </summary>
    public class LootEntry
    {
        /// <summary>Item prefab name.</summary>
        public string Item { get; set; }

        /// <summary>Drop chance (0.0-1.0, also used as weight).</summary>
        public float Chance { get; set; } = 1f;

        /// <summary>Minimum quantity.</summary>
        public int Min { get; set; } = 1;

        /// <summary>Maximum quantity.</summary>
        public int Max { get; set; } = 1;

        /// <summary>Rarity configuration for affix generation.</summary>
        public RarityRoll Rarity { get; set; }

        /// <summary>If true, always generate affixes on this item.</summary>
        public bool AlwaysAffix { get; set; }
    }

    /// <summary>
    /// Configuration for rolling rarity.
    /// </summary>
    public class RarityRoll
    {
        /// <summary>Minimum rarity to roll.</summary>
        public Rarity Minimum { get; set; } = Rarity.Common;

        /// <summary>Weight for common rarity.</summary>
        public float CommonWeight { get; set; } = 100f;

        /// <summary>Weight for uncommon rarity.</summary>
        public float UncommonWeight { get; set; } = 50f;

        /// <summary>Weight for rare rarity.</summary>
        public float RareWeight { get; set; } = 20f;

        /// <summary>Weight for epic rarity.</summary>
        public float EpicWeight { get; set; } = 5f;

        /// <summary>Weight for legendary rarity.</summary>
        public float LegendaryWeight { get; set; } = 1f;

        /// <summary>Roll a rarity.</summary>
        public Rarity Roll()
        {
            float total = CommonWeight + UncommonWeight + RareWeight + EpicWeight + LegendaryWeight;
            float roll = Random.value * total;

            if (roll < CommonWeight && Minimum <= Rarity.Common)
                return Rarity.Common;

            roll -= CommonWeight;
            if (roll < UncommonWeight && Minimum <= Rarity.Uncommon)
                return Rarity.Uncommon;

            roll -= UncommonWeight;
            if (roll < RareWeight && Minimum <= Rarity.Rare)
                return Rarity.Rare;

            roll -= RareWeight;
            if (roll < EpicWeight && Minimum <= Rarity.Epic)
                return Rarity.Epic;

            return Rarity.Legendary;
        }
    }

    /// <summary>
    /// Result of a loot roll.
    /// </summary>
    public class LootResult
    {
        /// <summary>Item prefab name.</summary>
        public string ItemName { get; set; }

        /// <summary>Quantity to drop.</summary>
        public int Count { get; set; }

        /// <summary>Whether to apply affixes.</summary>
        public bool HasAffixes { get; set; }

        /// <summary>Rarity for affix generation.</summary>
        public Rarity AffixRarity { get; set; }
    }

    /// <summary>
    /// Item rarity tiers.
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
