using System;
using System.Collections.Generic;
using UnityEngine;

namespace Denizen.Integration
{
    /// <summary>
    /// Integration wrapper for Tome currency/item system.
    /// Safely calls Tome API only if the mod is loaded.
    /// </summary>
    public static class TomeIntegration
    {
        /// <summary>
        /// Gets the amount of a currency a player has.
        /// </summary>
        public static int GetPlayerCurrency(Player player, string currencyPrefab)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab))
                return 0;

            if (Plugin.HasTome)
            {
                try
                {
                    return Tome.Registry.TomeAPI.GetPlayerCurrency(player, currencyPrefab);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to get currency: {ex.Message}");
                }
            }

            // Fallback: Count vanilla items
            return player.GetInventory()?.CountItems(currencyPrefab) ?? 0;
        }

        /// <summary>
        /// Checks if a player can afford a cost.
        /// </summary>
        public static bool CanAfford(Player player, string currencyPrefab, int amount)
        {
            return GetPlayerCurrency(player, currencyPrefab) >= amount;
        }

        /// <summary>
        /// Checks if a player can afford multiple costs.
        /// </summary>
        public static bool CanAfford(Player player, Dictionary<string, int> costs)
        {
            if (player == null || costs == null)
                return false;

            foreach (var kvp in costs)
            {
                if (!CanAfford(player, kvp.Key, kvp.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Consumes currency from a player.
        /// </summary>
        public static bool ConsumeCurrency(Player player, string currencyPrefab, int amount)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab) || amount <= 0)
                return false;

            if (!CanAfford(player, currencyPrefab, amount))
                return false;

            if (Plugin.HasTome)
            {
                try
                {
                    return Tome.Registry.TomeAPI.ConsumeCurrency(player, currencyPrefab, amount);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to consume currency: {ex.Message}");
                }
            }

            // Fallback: Remove vanilla items
            player.GetInventory()?.RemoveItem(currencyPrefab, amount);
            return true;
        }

        /// <summary>
        /// Awards currency to a player.
        /// </summary>
        public static bool AwardCurrency(Player player, string currencyPrefab, int amount)
        {
            if (player == null || string.IsNullOrEmpty(currencyPrefab) || amount <= 0)
                return false;

            if (Plugin.HasTome)
            {
                try
                {
                    return Tome.Registry.TomeAPI.AwardCurrency(player, currencyPrefab, amount);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to award currency: {ex.Message}");
                }
            }

            // Fallback: Add vanilla items
            var prefab = ZNetScene.instance?.GetPrefab(currencyPrefab);
            if (prefab == null)
                return false;

            return player.GetInventory()?.AddItem(prefab, amount) ?? false;
        }

        /// <summary>
        /// Spawns a Tome item at a position.
        /// </summary>
        public static GameObject SpawnItem(string prefabName, Vector3 position, int amount = 1)
        {
            if (string.IsNullOrEmpty(prefabName))
                return null;

            if (Plugin.HasTome)
            {
                try
                {
                    return Tome.Registry.TomeAPI.SpawnItem(prefabName, position, amount);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to spawn Tome item: {ex.Message}");
                }
            }

            // Fallback: Vanilla spawn
            var prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab == null)
                return null;

            var spawned = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            if (spawned != null && spawned.TryGetComponent<ItemDrop>(out var itemDrop))
            {
                itemDrop.m_itemData.m_stack = Math.Max(1, amount);
            }

            return spawned;
        }
    }
}
