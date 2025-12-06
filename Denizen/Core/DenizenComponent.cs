using Denizen.Entities;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// Component attached to all Denizen entities.
    /// Stores runtime data and provides access to configuration.
    /// </summary>
    public class DenizenComponent : MonoBehaviour
    {
        /// <summary>Denizen configuration.</summary>
        public DenizenConfig Config { get; private set; }

        /// <summary>Denizen ID.</summary>
        public string DenizenId => Config?.Id;

        /// <summary>Denizen type.</summary>
        public DenizenType Type => Config?.Type ?? DenizenType.Enemy;

        /// <summary>The Character component.</summary>
        public Character Character { get; private set; }

        /// <summary>The ZNetView for networking.</summary>
        public ZNetView ZNetView { get; private set; }

        /// <summary>Time when this entity was spawned.</summary>
        public float SpawnTime { get; private set; }

        /// <summary>
        /// Initialize the component with a configuration.
        /// </summary>
        public void Initialize(DenizenConfig config)
        {
            Config = config;
            SpawnTime = Time.time;

            Character = GetComponent<Character>();
            ZNetView = GetComponent<ZNetView>();

            if (ZNetView != null && ZNetView.IsValid())
            {
                // Store denizen ID in ZDO for network sync
                ZNetView.GetZDO().Set("denizen_id", config.Id);
            }
        }

        private void Awake()
        {
            Character = GetComponent<Character>();
            ZNetView = GetComponent<ZNetView>();
        }

        private void Start()
        {
            // If we don't have config yet, try to load from ZDO
            if (Config == null && ZNetView != null && ZNetView.IsValid())
            {
                var denizenId = ZNetView.GetZDO().GetString("denizen_id");
                if (!string.IsNullOrEmpty(denizenId))
                {
                    Config = DenizenRegistry.Instance.Get(denizenId);
                }
            }
        }

        private void OnDestroy()
        {
            if (Config != null)
            {
                DenizenEvents.RaiseDespawned(Character, Config.Id);
            }
        }

        /// <summary>
        /// Get the NPC config if this is an NPC.
        /// </summary>
        public NPCConfig GetNPCConfig()
        {
            return Config as NPCConfig;
        }

        /// <summary>
        /// Get the enemy config if this is an enemy.
        /// </summary>
        public EnemyConfig GetEnemyConfig()
        {
            return Config as EnemyConfig;
        }

        /// <summary>
        /// Get the boss config if this is a boss.
        /// </summary>
        public BossConfig GetBossConfig()
        {
            return Config as BossConfig;
        }

        /// <summary>
        /// Get the companion config if this is a companion.
        /// </summary>
        public CompanionConfig GetCompanionConfig()
        {
            return Config as CompanionConfig;
        }
    }
}
