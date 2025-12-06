using System.Collections.Generic;
using Denizen.Entities;
using Denizen.Integration;
using Jotunn.Managers;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// Handles spawning of denizen entities.
    /// </summary>
    public static class DenizenSpawner
    {
        private static readonly Dictionary<string, GameObject> _prefabCache = new();

        /// <summary>
        /// Spawn a denizen at a position.
        /// </summary>
        public static Character Spawn(string denizenId, Vector3 position, Quaternion rotation = default, int level = 0)
        {
            var config = DenizenRegistry.Instance.Get(denizenId);
            if (config == null)
            {
                Plugin.Log.LogWarning($"Denizen not found: {denizenId}");
                return null;
            }

            var prefab = GetOrCreatePrefab(config);
            if (prefab == null)
            {
                Plugin.Log.LogWarning($"Failed to get prefab for: {denizenId}");
                return null;
            }

            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            var instance = Object.Instantiate(prefab, position, rotation);
            var character = instance.GetComponent<Character>();

            if (character == null)
            {
                Plugin.Log.LogError($"Spawned prefab has no Character component: {denizenId}");
                Object.Destroy(instance);
                return null;
            }

            // Add Denizen component
            var denizenComponent = instance.GetComponent<DenizenComponent>();
            if (denizenComponent == null)
            {
                denizenComponent = instance.AddComponent<DenizenComponent>();
            }
            denizenComponent.Initialize(config);

            // Add appropriate AI component
            AddAIComponent(instance, config);

            // Set level if specified
            if (level > 0)
            {
                character.SetLevel(level);
            }

            // Apply scale
            if (config.Scale != 1f)
            {
                instance.transform.localScale = Vector3.one * config.Scale;
            }

            // Apply stats via Prime
            ApplyStats(character, config);

            // Trigger event
            DenizenEvents.RaiseSpawned(character, denizenId);

            Plugin.Log.LogDebug($"Spawned {denizenId} at {position}");
            return character;
        }

        /// <summary>
        /// Spawn a group of denizens.
        /// </summary>
        public static List<Character> SpawnGroup(string denizenId, Vector3 center, int count, float spread = 5f)
        {
            var spawned = new List<Character>();

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector3(
                    Random.Range(-spread, spread),
                    0,
                    Random.Range(-spread, spread)
                );

                var position = center + offset;

                // Find ground
                if (ZoneSystem.instance != null)
                {
                    ZoneSystem.instance.GetSolidHeight(position, out float height, 1000);
                    position.y = height;
                }

                var character = Spawn(denizenId, position);
                if (character != null)
                {
                    spawned.Add(character);
                }
            }

            return spawned;
        }

        /// <summary>
        /// Spawn a boss with setup.
        /// </summary>
        public static Character SpawnBoss(string denizenId, Vector3 position)
        {
            var config = DenizenRegistry.Instance.GetBoss(denizenId);
            if (config == null)
            {
                Plugin.Log.LogWarning($"Boss not found: {denizenId}");
                return null;
            }

            var character = Spawn(denizenId, position);
            if (character == null) return null;

            // Play boss music if configured
            if (!string.IsNullOrEmpty(config.Music))
            {
                SparkIntegration.PlayAttachedSound(config.Music, character.gameObject, loop: true);
            }

            // Show boss in Veneer UI
            VeneerIntegration.ShowBoss(character);

            // Attach boss aura via Spark
            SparkIntegration.AttachBossAura(character);

            // Trigger boss spawned event
            DenizenEvents.RaiseBossSpawned(character);

            return character;
        }

        /// <summary>
        /// Get or create the prefab for a denizen config.
        /// </summary>
        private static GameObject GetOrCreatePrefab(DenizenConfig config)
        {
            if (_prefabCache.TryGetValue(config.Id, out var cached))
            {
                return cached;
            }

            GameObject prefab = null;

            // Try custom prefab first
            if (!string.IsNullOrEmpty(config.Prefab))
            {
                prefab = PrefabManager.Instance.GetPrefab(config.Prefab);
            }

            // Fall back to cloning vanilla prefab
            if (prefab == null && !string.IsNullOrEmpty(config.BasedOn))
            {
                var basePrefab = PrefabManager.Instance.GetPrefab(config.BasedOn);
                if (basePrefab != null)
                {
                    prefab = PrefabManager.Instance.CreateClonedPrefab($"Denizen_{config.Id}", basePrefab);

                    // Set up the cloned prefab
                    SetupClonedPrefab(prefab, config);
                }
                else
                {
                    Plugin.Log.LogWarning($"Could not find base prefab '{config.BasedOn}' for denizen '{config.Id}'");
                }
            }

            if (prefab != null)
            {
                _prefabCache[config.Id] = prefab;
            }

            return prefab;
        }

        /// <summary>
        /// Set up a cloned prefab with denizen configuration.
        /// </summary>
        private static void SetupClonedPrefab(GameObject prefab, DenizenConfig config)
        {
            // Don't add components to prefab - add them to instance instead
            // Adding MonoBehaviours to prefabs can cause issues

            // Only set display name on prefab (this is safe)
            if (!string.IsNullOrEmpty(config.Name))
            {
                var humanoid = prefab.GetComponent<Humanoid>();
                if (humanoid != null)
                {
                    humanoid.m_name = config.Name;
                }
            }
        }

        /// <summary>
        /// Add appropriate AI component based on config.
        /// </summary>
        private static void AddAIComponent(GameObject instance, DenizenConfig config)
        {
            switch (config.Type)
            {
                case DenizenType.NPC:
                    if (instance.GetComponent<NPCAI>() == null)
                    {
                        instance.AddComponent<NPCAI>();
                    }
                    break;

                case DenizenType.Companion:
                    if (instance.GetComponent<CompanionAI>() == null)
                    {
                        instance.AddComponent<CompanionAI>();
                    }
                    break;

                case DenizenType.Boss:
                    if (instance.GetComponent<BossAI>() == null)
                    {
                        instance.AddComponent<BossAI>();
                    }
                    break;

                default:
                    if (instance.GetComponent<DenizenAI>() == null)
                    {
                        instance.AddComponent<DenizenAI>();
                    }
                    break;
            }
        }

        /// <summary>
        /// Apply stats to character via Prime.
        /// </summary>
        private static void ApplyStats(Character character, DenizenConfig config)
        {
            if (config.Stats == null) return;

            // Apply base stats via Prime integration
            PrimeIntegration.ApplyStats(character, config.Stats);

            // Apply level bonuses if character has levels
            int level = character.GetLevel();
            if (level > 1 && config.Stats.PerLevelBonus != null)
            {
                PrimeIntegration.ApplyLevelBonuses(character, config.Stats, level);
            }

            // Apply abilities if configured
            if (config.Abilities != null)
            {
                foreach (var ability in config.Abilities)
                {
                    if (!string.IsNullOrEmpty(ability.PrimeAbility))
                    {
                        PrimeIntegration.GrantAbility(character, ability.PrimeAbility);
                    }
                }
            }
        }

        /// <summary>
        /// Clear the prefab cache.
        /// </summary>
        public static void ClearCache()
        {
            _prefabCache.Clear();
        }
    }
}
