using System.Collections.Generic;
using Denizen.Core;

namespace Denizen.Entities
{
    /// <summary>
    /// Configuration for boss entities.
    /// </summary>
    public class BossConfig : DenizenConfig
    {
        /// <summary>Whether to show boss health bar.</summary>
        public bool BossBar { get; set; } = true;

        /// <summary>Music track ID for boss fight.</summary>
        public string Music { get; set; }

        /// <summary>Boss phases.</summary>
        public List<BossPhase> Phases { get; set; } = new();

        /// <summary>Trophy prefab dropped on kill.</summary>
        public string Trophy { get; set; }

        /// <summary>Power/ability unlocked on kill.</summary>
        public string PowerUnlock { get; set; }

        public BossConfig()
        {
            Type = DenizenType.Boss;
            Behavior = new BehaviorConfig
            {
                Aggression = Aggression.Aggressive,
                GroupBehavior = GroupBehavior.Solo,
                FleeThreshold = 0f
            };
        }
    }

    /// <summary>
    /// Configuration for a boss phase.
    /// </summary>
    public class BossPhase
    {
        /// <summary>Phase name for identification.</summary>
        public string Name { get; set; }

        /// <summary>Health threshold to enter this phase (1.0 = 100%).</summary>
        public float HealthThreshold { get; set; } = 1f;

        /// <summary>Abilities available in this phase.</summary>
        public List<string> Abilities { get; set; } = new();

        /// <summary>Behavior override for this phase.</summary>
        public BehaviorConfig Behavior { get; set; }

        /// <summary>Spark effect to play when entering phase.</summary>
        public string OnEnterEffect { get; set; }

        /// <summary>Message to display when entering phase.</summary>
        public string OnEnterMessage { get; set; }
    }
}
