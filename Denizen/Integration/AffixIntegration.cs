using System;
using System.Collections.Generic;
using Denizen.Loot;
using UnityEngine;

namespace Denizen.Integration
{
    /// <summary>
    /// Integration wrapper for Affix loot system.
    /// Safely calls Affix API only if the mod is loaded.
    /// </summary>
    public static class AffixIntegration
    {
        /// <summary>
        /// Spawns loot results with affixes applied.
        /// </summary>
        public static List<GameObject> SpawnLoot(List<LootResult> results, Vector3 position)
        {
            var spawned = new List<GameObject>();

            foreach (var result in results)
            {
                var obj = SpawnLootItem(result, position);
                if (obj != null)
                {
                    spawned.Add(obj);
                }
            }

            return spawned;
        }

        /// <summary>
        /// Spawns a single loot item with affixes applied if Affix is available.
        /// </summary>
        public static GameObject SpawnLootItem(LootResult result, Vector3 position)
        {
            if (result == null || string.IsNullOrEmpty(result.ItemName))
                return null;

            // Add some random spread
            var offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            var spawnPos = position + new Vector3(offset.x, 0.5f, offset.y);

            // Try to use Affix system if available and item should have affixes
            if (Plugin.HasAffix && result.HasAffixes)
            {
                try
                {
                    return SpawnWithAffixInternal(result, spawnPos);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to spawn with Affix: {ex.Message}");
                }
            }

            // Fallback to vanilla spawning
            return SpawnVanilla(result.ItemName, result.Count, spawnPos);
        }

        private static GameObject SpawnWithAffixInternal(LootResult result, Vector3 position)
        {
            // Convert Denizen rarity to Affix rarity
            var affixRarity = ConvertRarity(result.AffixRarity);

            // Create a DropResult for the Affix spawner
            var dropResult = new Affix.DropTables.DropResult
            {
                PrefabName = result.ItemName,
                Count = result.Count,
                HasAffixes = result.HasAffixes,
                Rarity = affixRarity
            };

            return Affix.Core.AffixItemSpawner.SpawnDrop(dropResult, position);
        }

        private static Affix.Core.Rarity ConvertRarity(Rarity denizenRarity)
        {
            return denizenRarity switch
            {
                Rarity.Common => Affix.Core.Rarity.Common,
                Rarity.Uncommon => Affix.Core.Rarity.Uncommon,
                Rarity.Rare => Affix.Core.Rarity.Rare,
                Rarity.Epic => Affix.Core.Rarity.Epic,
                Rarity.Legendary => Affix.Core.Rarity.Legendary,
                _ => Affix.Core.Rarity.Common
            };
        }

        /// <summary>
        /// Spawns an item without affixes (vanilla spawning).
        /// </summary>
        private static GameObject SpawnVanilla(string prefabName, int count, Vector3 position)
        {
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab == null)
            {
                Plugin.Log.LogWarning($"Could not find prefab: {prefabName}");
                return null;
            }

            var spawned = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            if (spawned == null)
                return null;

            if (spawned.TryGetComponent<ItemDrop>(out var itemDrop) && count > 1)
            {
                itemDrop.m_itemData.m_stack = count;
            }

            return spawned;
        }
    }
}
