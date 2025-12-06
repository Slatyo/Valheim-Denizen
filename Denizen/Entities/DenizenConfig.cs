using System.Collections.Generic;
using Denizen.Core;
using UnityEngine;

namespace Denizen.Entities
{
    /// <summary>
    /// Base configuration for all denizen entities.
    /// </summary>
    public abstract class DenizenConfig
    {
        /// <summary>Unique identifier for this denizen.</summary>
        public string Id { get; set; }

        /// <summary>Localization key for display name.</summary>
        public string Name { get; set; }

        /// <summary>Entity type.</summary>
        public DenizenType Type { get; set; }

        /// <summary>Custom prefab name, if using a custom prefab.</summary>
        public string Prefab { get; set; }

        /// <summary>Vanilla prefab to clone from.</summary>
        public string BasedOn { get; set; }

        /// <summary>Scale multiplier for the entity.</summary>
        public float Scale { get; set; } = 1f;

        /// <summary>Spawning configuration.</summary>
        public SpawnConfig Spawning { get; set; }

        /// <summary>Stats configuration for Prime integration.</summary>
        public StatConfig Stats { get; set; }

        /// <summary>Behavior/AI configuration.</summary>
        public BehaviorConfig Behavior { get; set; }

        /// <summary>Abilities this entity can use.</summary>
        public List<AbilityConfig> Abilities { get; set; } = new();

        /// <summary>Drop table ID for loot.</summary>
        public string DropTable { get; set; }

        /// <summary>Ambient VFX effect ID (Spark).</summary>
        public string AmbientEffect { get; set; }
    }

    /// <summary>
    /// Spawn configuration.
    /// </summary>
    public class SpawnConfig
    {
        /// <summary>How this entity spawns.</summary>
        public SpawnType Type { get; set; } = SpawnType.Manual;

        /// <summary>Fixed spawn locations (for Fixed type).</summary>
        public List<Vector3> Locations { get; set; } = new();

        /// <summary>Biomes where this entity can spawn.</summary>
        public List<Heightmap.Biome> Biomes { get; set; } = new();

        /// <summary>Spawn chance per spawn check (0.0-1.0).</summary>
        public float SpawnChance { get; set; } = 0.1f;

        /// <summary>Minimum group size.</summary>
        public int GroupMin { get; set; } = 1;

        /// <summary>Maximum group size.</summary>
        public int GroupMax { get; set; } = 1;

        /// <summary>Chance for starred version (per star level).</summary>
        public float LevelChance { get; set; } = 0.1f;

        /// <summary>If true, only one instance can exist in the world.</summary>
        public bool Unique { get; set; }

        /// <summary>Minimum altitude for spawning.</summary>
        public float MinAltitude { get; set; } = -1000f;

        /// <summary>Maximum altitude for spawning.</summary>
        public float MaxAltitude { get; set; } = 1000f;

        /// <summary>Required global keys for spawning.</summary>
        public List<string> RequiredGlobalKeys { get; set; } = new();
    }

    /// <summary>
    /// Stats configuration for Prime integration.
    /// </summary>
    public class StatConfig
    {
        /// <summary>Prime stat template to use as base.</summary>
        public string Template { get; set; } = "Creature";

        /// <summary>Stat overrides applied on top of template.</summary>
        public Dictionary<string, float> Overrides { get; set; } = new();

        /// <summary>Per-level stat bonuses (for companions).</summary>
        public Dictionary<string, float> PerLevelBonus { get; set; } = new();
    }

    /// <summary>
    /// Behavior/AI configuration.
    /// </summary>
    public class BehaviorConfig
    {
        /// <summary>Aggression type.</summary>
        public Aggression Aggression { get; set; } = Aggression.Normal;

        /// <summary>Group behavior type.</summary>
        public GroupBehavior GroupBehavior { get; set; } = GroupBehavior.Solo;

        /// <summary>Health threshold to start fleeing (0.0-1.0).</summary>
        public float FleeThreshold { get; set; } = 0f;

        /// <summary>Targeting priority.</summary>
        public TargetPriority Targeting { get; set; } = TargetPriority.Nearest;

        /// <summary>Preferred combat range.</summary>
        public CombatRange PreferredRange { get; set; } = CombatRange.Melee;

        /// <summary>If true, tries to maintain distance from target.</summary>
        public bool KeepDistance { get; set; }

        /// <summary>Ideal distance to maintain (if KeepDistance is true).</summary>
        public float IdealDistance { get; set; } = 15f;

        /// <summary>Range to call for help when attacked.</summary>
        public float CallHelpRange { get; set; } = 20f;

        /// <summary>Reactions to various triggers.</summary>
        public List<ReactionConfig> Reactions { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a reaction trigger.
    /// </summary>
    public class ReactionConfig
    {
        /// <summary>The reaction to perform.</summary>
        public Reaction Reaction { get; set; }

        /// <summary>What triggers this reaction.</summary>
        public Trigger Trigger { get; set; }

        /// <summary>Threshold value (e.g., health percent for OnLowHealth).</summary>
        public float Threshold { get; set; } = 0.3f;

        /// <summary>Chance for this reaction to trigger (0.0-1.0).</summary>
        public float Chance { get; set; } = 1f;

        /// <summary>Cooldown between uses of this reaction.</summary>
        public float Cooldown { get; set; }

        /// <summary>Distance parameter (for Retreat, etc.).</summary>
        public float Distance { get; set; } = 10f;

        /// <summary>Effect to apply (Prime modifier ID or Spark effect).</summary>
        public string Effect { get; set; }

        public ReactionConfig() { }

        public ReactionConfig(Reaction reaction, Trigger trigger)
        {
            Reaction = reaction;
            Trigger = trigger;
        }
    }

    /// <summary>
    /// Configuration for an ability.
    /// </summary>
    public class AbilityConfig
    {
        /// <summary>Ability name/ID.</summary>
        public string Name { get; set; }

        /// <summary>Cooldown between uses.</summary>
        public float Cooldown { get; set; } = 10f;

        /// <summary>Maximum range for this ability.</summary>
        public float Range { get; set; } = 10f;

        /// <summary>Minimum range (only use at distance).</summary>
        public float MinRange { get; set; }

        /// <summary>Animation to play.</summary>
        public string Animation { get; set; }

        /// <summary>Spark VFX effect to play.</summary>
        public string Effect { get; set; }

        /// <summary>Condition for using this ability.</summary>
        public AbilityCondition Condition { get; set; } = AbilityCondition.Always;

        /// <summary>Prime ability ID to execute.</summary>
        public string PrimeAbility { get; set; }
    }
}
