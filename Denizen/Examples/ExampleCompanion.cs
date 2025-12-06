using Denizen.Core;
using Denizen.Entities;

namespace Denizen.Examples
{
    /// <summary>
    /// Example Companion: Battle Wolf.
    /// A tameable wolf companion with enhanced combat abilities.
    /// </summary>
    public static class ExampleCompanion
    {
        public static void Register()
        {
            DenizenAPI.RegisterCompanion("Battle_Wolf", new CompanionConfig
            {
                Name = "$companion_battle_wolf",

                // Clone from Wolf
                BasedOn = "Wolf",
                Scale = 1.2f,

                // Taming configuration
                Tameable = true,
                TameFood = new() { "RawMeat", "NeckTail", "DeerMeat", "LoxMeat" },
                TameTime = 1200f, // 20 minutes

                // Stats scale with level
                Stats = new StatConfig
                {
                    Template = "Creature",
                    Overrides = new()
                    {
                        ["Health"] = 200,
                        ["Damage"] = 40,
                        ["MoveSpeed"] = 1.3f,
                        ["Armor"] = 10
                    },
                    PerLevelBonus = new()
                    {
                        ["Health"] = 50,
                        ["Damage"] = 10,
                        ["Armor"] = 5
                    }
                },

                // Companion AI behavior
                FriendlyBehavior = new FriendlyConfig
                {
                    // Following
                    FollowDistance = 4f,
                    TeleportDistance = 40f,

                    // Combat
                    CombatStyle = FriendlyCombat.Aggressive,
                    ProtectOwner = true,
                    ProtectRange = 20f,

                    // Commands
                    Commands = new()
                    {
                        CompanionCommand.Follow,
                        CompanionCommand.Stay,
                        CompanionCommand.Attack,
                        CompanionCommand.Guard,
                        CompanionCommand.Passive
                    },

                    // Survival
                    AvoidHazards = new() { "fire", "smoke", "tar", "poison" },
                    ReturnOnLowHP = true,
                    LowHPThreshold = 0.25f,

                    // Idle
                    IdleBehavior = IdleBehavior.Wander,
                    WanderRadius = 8f
                },

                // Special abilities
                Abilities = new()
                {
                    new AbilityConfig
                    {
                        Name = "Howl",
                        Cooldown = 60f,
                        Range = 30f,
                        Animation = "howl",
                        Effect = "wolf_howl",
                        Condition = AbilityCondition.AllyNearby
                        // Buffs nearby allies (would link to Prime)
                    },
                    new AbilityConfig
                    {
                        Name = "Pounce",
                        Cooldown = 10f,
                        Range = 8f,
                        MinRange = 3f,
                        Animation = "jump_attack",
                        Effect = "wolf_pounce",
                        Condition = AbilityCondition.TargetInRange
                    }
                },

                // No natural spawning - only tamed
                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Manual
                },

                // General behavior (base class)
                Behavior = new BehaviorConfig
                {
                    Aggression = Aggression.Normal,
                    GroupBehavior = GroupBehavior.Pack,
                    FleeThreshold = 0f, // Companions don't flee
                    Targeting = TargetPriority.Nearest,
                    PreferredRange = CombatRange.Melee,
                    Reactions = new()
                    {
                        new ReactionConfig
                        {
                            Reaction = Reaction.Protect,
                            Trigger = Trigger.OnAllyDamaged,
                            Cooldown = 5f
                        },
                        new ReactionConfig
                        {
                            Reaction = Reaction.Avenge,
                            Trigger = Trigger.OnAllyDeath,
                            Effect = "damage_multiply:1.3",
                            Cooldown = 30f
                        }
                    }
                },

                // Ambient effect
                AmbientEffect = "wolf_companion_aura"
            });

            // Also register a Raven companion (non-combat)
            RegisterRavenCompanion();

            Plugin.Log.LogDebug("Registered Battle_Wolf companion");
        }

        private static void RegisterRavenCompanion()
        {
            DenizenAPI.RegisterCompanion("Scout_Raven", new CompanionConfig
            {
                Name = "Scout Raven",

                // Would need custom raven prefab or clone from Odin's ravens
                BasedOn = "Crow",
                Scale = 1.5f,

                // Can't be tamed normally - granted by quest/item
                Tameable = false,

                // Low combat stats - it's a scout
                Stats = new StatConfig
                {
                    Template = "Creature",
                    Overrides = new()
                    {
                        ["Health"] = 50,
                        ["Damage"] = 5,
                        ["MoveSpeed"] = 2f
                    }
                },

                // Non-combat companion behavior
                FriendlyBehavior = new FriendlyConfig
                {
                    FollowDistance = 8f,
                    TeleportDistance = 50f,

                    CombatStyle = FriendlyCombat.Passive, // Never fights
                    ProtectOwner = false,

                    Commands = new()
                    {
                        CompanionCommand.Follow,
                        CompanionCommand.Stay
                    },

                    AvoidHazards = new() { "fire", "smoke" },
                    ReturnOnLowHP = true,
                    LowHPThreshold = 0.5f,

                    IdleBehavior = IdleBehavior.Wander,
                    WanderRadius = 15f
                },

                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Manual
                },

                Behavior = new BehaviorConfig
                {
                    Aggression = Aggression.Passive,
                    FleeThreshold = 0.8f,
                    Reactions = new()
                    {
                        new ReactionConfig
                        {
                            Reaction = Reaction.Flee,
                            Trigger = Trigger.OnDamaged
                        }
                    }
                }
            });
        }
    }
}
