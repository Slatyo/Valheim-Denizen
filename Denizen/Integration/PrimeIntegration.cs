using System;
using System.Collections.Generic;
using Denizen.Core;
using Denizen.Entities;
using UnityEngine;

namespace Denizen.Integration
{
    /// <summary>
    /// Integration wrapper for Prime stat system.
    /// Safely calls Prime API only if the mod is loaded.
    /// </summary>
    public static class PrimeIntegration
    {
        /// <summary>
        /// Initialize Prime integration - subscribe to events.
        /// </summary>
        public static void Initialize()
        {
            if (!Plugin.HasPrime)
            {
                Plugin.Log.LogDebug("Prime not available, skipping integration");
                return;
            }

            try
            {
                SubscribeToEvents();
                Plugin.Log.LogInfo("Prime integration initialized");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to initialize Prime integration: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup Prime integration.
        /// </summary>
        public static void Cleanup()
        {
            if (!Plugin.HasPrime) return;

            try
            {
                UnsubscribeFromEvents();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to cleanup Prime integration: {ex.Message}");
            }
        }

        private static void SubscribeToEvents()
        {
            Prime.Events.PrimeEvents.OnPostDamage += OnPostDamage;
            Prime.Events.PrimeEvents.OnKill += OnKill;
            Prime.Events.PrimeEvents.OnPreDamage += OnPreDamage;
        }

        private static void UnsubscribeFromEvents()
        {
            Prime.Events.PrimeEvents.OnPostDamage -= OnPostDamage;
            Prime.Events.PrimeEvents.OnKill -= OnKill;
            Prime.Events.PrimeEvents.OnPreDamage -= OnPreDamage;
        }

        /// <summary>
        /// Called before damage is applied - can modify for denizen resistance.
        /// </summary>
        private static void OnPreDamage(Prime.Combat.DamageInfo damageInfo)
        {
            if (damageInfo?.Target == null) return;

            var denizen = damageInfo.Target.GetComponent<DenizenComponent>();
            if (denizen == null) return;

            // Denizen-specific damage modifications could go here
            // e.g., boss immunity phases, special resistances
        }

        /// <summary>
        /// Called after damage calculation - notify DenizenAI.
        /// </summary>
        private static void OnPostDamage(Prime.Combat.DamageInfo damageInfo)
        {
            if (damageInfo?.Target == null) return;

            var denizenAI = damageInfo.Target.GetComponent<DenizenAI>();
            if (denizenAI != null && damageInfo.FinalDamage > 0)
            {
                // Create HitData-like info for the AI
                var hit = new HitData();
                hit.m_damage.m_damage = damageInfo.FinalDamage;
                hit.m_point = damageInfo.HitPoint;

                denizenAI.OnDamaged(hit, damageInfo.Attacker);
            }

            // Raise Denizen event
            if (damageInfo.Target.GetComponent<DenizenComponent>() != null)
            {
                DenizenEvents.RaiseDamageTaken(damageInfo.Target, damageInfo.Attacker, damageInfo.FinalDamage);
            }
        }

        /// <summary>
        /// Called when something is killed - handle denizen death.
        /// </summary>
        private static void OnKill(Character killer, Character victim, Prime.Combat.DamageInfo damageInfo)
        {
            if (victim == null) return;

            var denizen = victim.GetComponent<DenizenComponent>();
            if (denizen == null) return;

            // Handle denizen death
            DenizenEvents.RaiseDenizenKilled(victim, killer);

            // Spawn loot from denizen's drop table
            SpawnDenizenLoot(victim, denizen);

            // Handle boss-specific death
            var bossAI = victim.GetComponent<BossAI>();
            if (bossAI != null)
            {
                bossAI.OnDeath(killer);
            }
        }

        /// <summary>
        /// Spawns loot for a killed denizen.
        /// </summary>
        private static void SpawnDenizenLoot(Character victim, DenizenComponent denizen)
        {
            if (denizen.Config == null) return;

            string dropTable = denizen.Config.DropTable;
            if (string.IsNullOrEmpty(dropTable)) return;

            // Count nearby players for bonus loot
            int playerCount = 1;
            var nearbyPlayers = Player.GetAllPlayers();
            foreach (var player in nearbyPlayers)
            {
                if (player != null && Vector3.Distance(player.transform.position, victim.transform.position) < 50f)
                {
                    playerCount++;
                }
            }
            playerCount = Mathf.Max(1, playerCount - 1); // Don't double count

            // Roll and spawn loot
            var spawned = Loot.LootTable.RollAndSpawn(dropTable, victim.transform.position, playerCount);

            Plugin.Log.LogDebug($"Spawned {spawned.Count} loot items from {dropTable}");
        }

        /// <summary>
        /// Sets base stat values for a character from config.
        /// Also syncs with vanilla values where applicable.
        /// </summary>
        public static void ApplyStats(Character character, StatConfig statConfig)
        {
            if (character == null || statConfig == null)
                return;

            // Apply to vanilla first (health is special)
            if (statConfig.Overrides != null)
            {
                foreach (var kvp in statConfig.Overrides)
                {
                    ApplyVanillaStat(character, kvp.Key, kvp.Value);
                }
            }

            // Then apply to Prime if available
            if (!Plugin.HasPrime) return;

            try
            {
                ApplyStatsInternal(character, statConfig);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to apply Prime stats: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply a stat to vanilla character properties.
        /// </summary>
        private static void ApplyVanillaStat(Character character, string statId, float value)
        {
            switch (statId.ToLower())
            {
                case "health":
                case "maxhealth":
                    // Set max health via Humanoid if available
                    if (character is Humanoid humanoid)
                    {
                        humanoid.m_health = value;
                    }
                    character.SetMaxHealth(value);
                    character.SetHealth(value);
                    Plugin.Log.LogDebug($"Set vanilla health = {value} on {character.m_name}");
                    break;

                case "damage":
                    // Can't easily set vanilla damage without modifying attacks
                    break;
            }
        }

        private static void ApplyStatsInternal(Character character, StatConfig statConfig)
        {
            // Apply stat overrides to Prime
            if (statConfig.Overrides != null)
            {
                foreach (var kvp in statConfig.Overrides)
                {
                    Prime.PrimeAPI.SetBase(character, kvp.Key, kvp.Value);
                    Plugin.Log.LogDebug($"Set Prime {kvp.Key} = {kvp.Value} on {character.m_name}");
                }
            }
        }

        /// <summary>
        /// Applies per-level stat bonuses to a character.
        /// </summary>
        public static void ApplyLevelBonuses(Character character, StatConfig statConfig, int level)
        {
            if (character == null || statConfig?.PerLevelBonus == null || level <= 1)
                return;

            int bonusLevels = level - 1;

            // Apply vanilla level bonuses
            if (statConfig.PerLevelBonus.TryGetValue("Health", out float healthPerLevel))
            {
                float bonus = healthPerLevel * bonusLevels;
                float newHealth = character.GetMaxHealth() + bonus;
                character.SetMaxHealth(newHealth);
                character.SetHealth(newHealth);
            }

            // Apply Prime level bonuses
            if (!Plugin.HasPrime) return;

            try
            {
                foreach (var kvp in statConfig.PerLevelBonus)
                {
                    float bonus = kvp.Value * bonusLevels;
                    string modId = $"denizen_level_{kvp.Key}";

                    Prime.PrimeAPI.AddModifier(character, new Prime.Modifiers.Modifier(
                        modId, kvp.Key, Prime.Modifiers.ModifierType.Flat, bonus)
                    {
                        Source = "DenizenLevel",
                        Order = Prime.Modifiers.ModifierOrder.Inherent
                    });

                    Plugin.Log.LogDebug($"Added level bonus +{bonus} {kvp.Key} on {character.m_name}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to apply level bonuses: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a timed buff to a character.
        /// </summary>
        public static void ApplyBuff(Character character, string statId, float value, float duration, string source)
        {
            if (!Plugin.HasPrime || character == null)
                return;

            try
            {
                Prime.PrimeAPI.ApplyTimedFlat(character, statId, value, duration, source);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to apply buff: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a percentage buff to a character.
        /// </summary>
        public static void ApplyPercentBuff(Character character, string statId, float percent, float duration, string source)
        {
            if (!Plugin.HasPrime || character == null)
                return;

            try
            {
                Prime.PrimeAPI.ApplyTimedPercent(character, statId, percent, duration, source);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to apply percent buff: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a stat value from a character.
        /// </summary>
        public static float GetStat(Character character, string statId)
        {
            if (!Plugin.HasPrime || character == null)
                return 0f;

            try
            {
                return Prime.PrimeAPI.Get(character, statId);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to get stat: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Grants an ability to a character.
        /// </summary>
        public static bool GrantAbility(Character character, string abilityId)
        {
            if (!Plugin.HasPrime || character == null || string.IsNullOrEmpty(abilityId))
                return false;

            try
            {
                return Prime.PrimeAPI.GrantAbility(character, abilityId);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to grant ability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uses an ability on a character.
        /// </summary>
        public static bool UseAbility(Character character, string abilityId, Character target = null)
        {
            if (!Plugin.HasPrime || character == null || string.IsNullOrEmpty(abilityId))
                return false;

            try
            {
                return Prime.PrimeAPI.UseAbility(character, abilityId, target);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to use ability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes an entity from Prime tracking.
        /// </summary>
        public static void RemoveEntity(Character character)
        {
            if (!Plugin.HasPrime || character == null)
                return;

            try
            {
                Prime.PrimeAPI.RemoveEntity(character);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to remove entity: {ex.Message}");
            }
        }
    }
}
