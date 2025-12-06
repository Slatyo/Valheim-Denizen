using System.Collections.Generic;
using Denizen.Core;

namespace Denizen.Entities
{
    /// <summary>
    /// Configuration for companion entities.
    /// </summary>
    public class CompanionConfig : DenizenConfig
    {
        /// <summary>Whether this companion can be tamed.</summary>
        public bool Tameable { get; set; } = true;

        /// <summary>Food items that can tame this companion.</summary>
        public List<string> TameFood { get; set; } = new();

        /// <summary>Time to tame in seconds.</summary>
        public float TameTime { get; set; } = 1800f;

        /// <summary>Companion-specific behavior.</summary>
        public FriendlyConfig FriendlyBehavior { get; set; }

        public CompanionConfig()
        {
            Type = DenizenType.Companion;
            FriendlyBehavior = new FriendlyConfig();
        }
    }

    /// <summary>
    /// Friendly AI behavior configuration.
    /// </summary>
    public class FriendlyConfig
    {
        /// <summary>Distance to maintain from owner.</summary>
        public float FollowDistance { get; set; } = 5f;

        /// <summary>Distance at which to teleport to owner.</summary>
        public float TeleportDistance { get; set; } = 40f;

        /// <summary>Combat behavior style.</summary>
        public FriendlyCombat CombatStyle { get; set; } = FriendlyCombat.Protective;

        /// <summary>Whether to protect owner from attacks.</summary>
        public bool ProtectOwner { get; set; } = true;

        /// <summary>Range to look for threats to owner.</summary>
        public float ProtectRange { get; set; } = 15f;

        /// <summary>Available commands for this companion.</summary>
        public List<CompanionCommand> Commands { get; set; } = new()
        {
            CompanionCommand.Follow,
            CompanionCommand.Stay,
            CompanionCommand.Attack
        };

        /// <summary>Hazards this companion will avoid.</summary>
        public List<string> AvoidHazards { get; set; } = new() { "fire", "smoke", "water", "tar" };

        /// <summary>Whether to return to owner when health is low.</summary>
        public bool ReturnOnLowHP { get; set; } = true;

        /// <summary>Health threshold for returning to owner.</summary>
        public float LowHPThreshold { get; set; } = 0.3f;

        /// <summary>Idle behavior when not commanded.</summary>
        public IdleBehavior IdleBehavior { get; set; } = IdleBehavior.Wander;

        /// <summary>Wander radius when idle.</summary>
        public float WanderRadius { get; set; } = 10f;
    }
}
