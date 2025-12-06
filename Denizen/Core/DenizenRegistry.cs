using System;
using System.Collections.Generic;
using Denizen.Entities;

namespace Denizen.Core
{
    /// <summary>
    /// Central registry for all denizen entities.
    /// </summary>
    public class DenizenRegistry
    {
        private static DenizenRegistry _instance;
        public static DenizenRegistry Instance => _instance ??= new DenizenRegistry();

        private readonly Dictionary<string, DenizenConfig> _configs = new();
        private readonly Dictionary<string, NPCConfig> _npcs = new();
        private readonly Dictionary<string, EnemyConfig> _enemies = new();
        private readonly Dictionary<string, BossConfig> _bosses = new();
        private readonly Dictionary<string, CompanionConfig> _companions = new();

        /// <summary>
        /// Register an NPC.
        /// </summary>
        public void RegisterNPC(string id, NPCConfig config)
        {
            config.Id = id;
            _npcs[id] = config;
            _configs[id] = config;
            Plugin.Log.LogInfo($"Registered NPC: {id}");
        }

        /// <summary>
        /// Register an enemy.
        /// </summary>
        public void RegisterEnemy(string id, EnemyConfig config)
        {
            config.Id = id;
            _enemies[id] = config;
            _configs[id] = config;
            Plugin.Log.LogInfo($"Registered Enemy: {id}");
        }

        /// <summary>
        /// Register a boss.
        /// </summary>
        public void RegisterBoss(string id, BossConfig config)
        {
            config.Id = id;
            _bosses[id] = config;
            _configs[id] = config;
            Plugin.Log.LogInfo($"Registered Boss: {id}");
        }

        /// <summary>
        /// Register a companion.
        /// </summary>
        public void RegisterCompanion(string id, CompanionConfig config)
        {
            config.Id = id;
            _companions[id] = config;
            _configs[id] = config;
            Plugin.Log.LogInfo($"Registered Companion: {id}");
        }

        /// <summary>
        /// Get any denizen config by ID.
        /// </summary>
        public DenizenConfig Get(string id)
        {
            return _configs.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// Get NPC config by ID.
        /// </summary>
        public NPCConfig GetNPC(string id)
        {
            return _npcs.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// Get enemy config by ID.
        /// </summary>
        public EnemyConfig GetEnemy(string id)
        {
            return _enemies.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// Get boss config by ID.
        /// </summary>
        public BossConfig GetBoss(string id)
        {
            return _bosses.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// Get companion config by ID.
        /// </summary>
        public CompanionConfig GetCompanion(string id)
        {
            return _companions.TryGetValue(id, out var config) ? config : null;
        }

        /// <summary>
        /// Get all registered denizen IDs.
        /// </summary>
        public IEnumerable<string> GetAllIds() => _configs.Keys;

        /// <summary>
        /// Get all NPC IDs.
        /// </summary>
        public IEnumerable<string> GetNPCIds() => _npcs.Keys;

        /// <summary>
        /// Get all enemy IDs.
        /// </summary>
        public IEnumerable<string> GetEnemyIds() => _enemies.Keys;

        /// <summary>
        /// Get all boss IDs.
        /// </summary>
        public IEnumerable<string> GetBossIds() => _bosses.Keys;

        /// <summary>
        /// Get all companion IDs.
        /// </summary>
        public IEnumerable<string> GetCompanionIds() => _companions.Keys;

        /// <summary>
        /// Get all configs of a specific type.
        /// </summary>
        public IEnumerable<DenizenConfig> GetByType(DenizenType type)
        {
            foreach (var config in _configs.Values)
            {
                if (config.Type == type)
                    yield return config;
            }
        }

        /// <summary>
        /// Check if a denizen is registered.
        /// </summary>
        public bool Has(string id) => _configs.ContainsKey(id);

        /// <summary>
        /// Total count of registered denizens.
        /// </summary>
        public int Count => _configs.Count;

        /// <summary>
        /// Clear all registrations.
        /// </summary>
        public void Clear()
        {
            _configs.Clear();
            _npcs.Clear();
            _enemies.Clear();
            _bosses.Clear();
            _companions.Clear();
        }
    }
}
