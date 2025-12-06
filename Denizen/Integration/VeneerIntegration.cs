using System;
using System.Collections.Generic;
using UnityEngine;

namespace Denizen.Integration
{
    /// <summary>
    /// Integration wrapper for Veneer UI system.
    /// Safely calls Veneer API only if the mod is loaded.
    /// </summary>
    public static class VeneerIntegration
    {
        private static readonly List<Character> _trackedBosses = new List<Character>();
        private static Character _mainBoss;

        /// <summary>
        /// Shows a boss in the Veneer boss frame.
        /// </summary>
        public static void ShowBoss(Character boss)
        {
            if (!Plugin.HasVeneer || boss == null)
                return;

            try
            {
                ShowBossInternal(boss);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to show boss: {ex.Message}");
            }
        }

        private static void ShowBossInternal(Character boss)
        {
            // Add to tracked list
            if (!_trackedBosses.Contains(boss))
            {
                _trackedBosses.Add(boss);
            }

            // Set as main boss if first one
            if (_mainBoss == null)
            {
                _mainBoss = boss;
            }

            // Update the boss group
            UpdateBossGroup();
        }

        /// <summary>
        /// Hides a boss from the Veneer boss frame.
        /// </summary>
        public static void HideBoss(Character boss)
        {
            if (!Plugin.HasVeneer || boss == null)
                return;

            try
            {
                HideBossInternal(boss);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to hide boss: {ex.Message}");
            }
        }

        private static void HideBossInternal(Character boss)
        {
            // Remove from tracked list
            _trackedBosses.Remove(boss);

            // Update main boss if needed
            if (_mainBoss == boss)
            {
                _mainBoss = _trackedBosses.Count > 0 ? _trackedBosses[0] : null;
            }

            // Update the boss group
            UpdateBossGroup();
        }

        private static void UpdateBossGroup()
        {
            var bossGroup = Veneer.Components.Specialized.VeneerBossGroup.Instance;
            if (bossGroup != null)
            {
                bossGroup.UpdateBossList(_trackedBosses, _mainBoss);
            }
        }

        /// <summary>
        /// Creates an NPC dialogue window.
        /// </summary>
        public static object CreateDialogueWindow(string npcName, string title)
        {
            if (!Plugin.HasVeneer)
                return null;

            try
            {
                return CreateDialogueWindowInternal(npcName, title);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to create dialogue window: {ex.Message}");
                return null;
            }
        }

        private static object CreateDialogueWindowInternal(string npcName, string title)
        {
            var window = Veneer.Core.VeneerAPI.CreateWindow($"npc_dialogue_{npcName}", title, 500, 400);
            return window;
        }

        /// <summary>
        /// Shows a tooltip.
        /// </summary>
        public static void ShowTooltip(string title, string body)
        {
            if (!Plugin.HasVeneer)
                return;

            try
            {
                Veneer.Core.VeneerAPI.ShowTooltip(title, body);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to show tooltip: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public static void HideTooltip()
        {
            if (!Plugin.HasVeneer)
                return;

            try
            {
                Veneer.Core.VeneerAPI.HideTooltip();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to hide tooltip: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a rarity color from Veneer.
        /// </summary>
        public static Color GetRarityColor(int tier)
        {
            if (!Plugin.HasVeneer)
            {
                // Fallback colors
                return tier switch
                {
                    0 => new Color(0.6f, 0.6f, 0.6f), // Common - Gray
                    1 => new Color(0.2f, 0.8f, 0.2f), // Uncommon - Green
                    2 => new Color(0.2f, 0.4f, 1f),   // Rare - Blue
                    3 => new Color(0.6f, 0.2f, 0.8f), // Epic - Purple
                    4 => new Color(1f, 0.6f, 0f),     // Legendary - Orange
                    _ => Color.white
                };
            }

            try
            {
                return Veneer.Core.VeneerAPI.GetRarityColor(tier);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to get rarity color: {ex.Message}");
                return Color.white;
            }
        }

        /// <summary>
        /// Clears all tracked bosses.
        /// </summary>
        public static void ClearBosses()
        {
            _trackedBosses.Clear();
            _mainBoss = null;

            if (Plugin.HasVeneer)
            {
                try
                {
                    UpdateBossGroup();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to clear bosses: {ex.Message}");
                }
            }
        }
    }
}
