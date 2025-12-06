using Denizen.Core;
using Denizen.Entities;
using Denizen.Loot;

namespace Denizen.Examples
{
    /// <summary>
    /// Example Boss: The Frost Lord.
    /// A multi-phase boss with escalating abilities.
    /// </summary>
    public static class ExampleBoss
    {
        public static void Register()
        {
            DenizenAPI.RegisterBoss("Frost_Lord", new BossConfig
            {
                Name = "$boss_frost_lord",

                // Custom prefab or clone from Troll/Golem
                BasedOn = "StoneGolem",
                Scale = 3f,

                // Manual spawn only (for Rift dungeons, etc.)
                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Manual,
                    Unique = true
                },

                // Boss stats
                Stats = new StatConfig
                {
                    Template = "Boss",
                    Overrides = new()
                    {
                        ["Health"] = 10000,
                        ["Damage"] = 150,
                        ["Armor"] = 50,
                        ["FrostDamage"] = 100,
                        ["FrostResist"] = 1f, // Immune to frost
                        ["FireResist"] = -1f  // Very weak to fire
                    }
                },

                // Show boss health bar
                BossBar = true,
                Music = "boss_frost_lord",

                // Phases
                Phases = new()
                {
                    // Phase 1: 100% - 50% health
                    new BossPhase
                    {
                        Name = "Phase1",
                        HealthThreshold = 1f,
                        Abilities = new() { "FrostBreath", "IceSpikes" },
                        Behavior = new BehaviorConfig
                        {
                            Aggression = Aggression.Aggressive,
                            Targeting = TargetPriority.HighestThreat
                        }
                    },

                    // Phase 2: 50% - 20% health - more aggressive
                    new BossPhase
                    {
                        Name = "Phase2",
                        HealthThreshold = 0.5f,
                        Abilities = new() { "FrostBreath", "IceSpikes", "Blizzard" },
                        Behavior = new BehaviorConfig
                        {
                            Aggression = Aggression.Berserker,
                            Targeting = TargetPriority.Random
                        },
                        OnEnterEffect = "frost_enrage",
                        OnEnterMessage = "$boss_frost_lord_phase2"
                    },

                    // Phase 3: 20% - 0% health - desperate
                    new BossPhase
                    {
                        Name = "FinalStand",
                        HealthThreshold = 0.2f,
                        Abilities = new() { "IceTomb", "Shatter", "Blizzard" },
                        Behavior = new BehaviorConfig
                        {
                            Aggression = Aggression.Berserker,
                            Targeting = TargetPriority.LowestHealth // Focus kills
                        },
                        OnEnterEffect = "frost_desperate",
                        OnEnterMessage = "$boss_frost_lord_phase3"
                    }
                },

                // All abilities
                Abilities = new()
                {
                    new AbilityConfig
                    {
                        Name = "FrostBreath",
                        Cooldown = 6f,
                        Range = 15f,
                        MinRange = 3f,
                        Animation = "breath_attack",
                        Effect = "frost_breath_boss",
                        Condition = AbilityCondition.TargetInRange
                    },
                    new AbilityConfig
                    {
                        Name = "IceSpikes",
                        Cooldown = 12f,
                        Range = 25f,
                        Animation = "ground_slam",
                        Effect = "ice_spikes_aoe",
                        Condition = AbilityCondition.MultipleTargets
                    },
                    new AbilityConfig
                    {
                        Name = "Blizzard",
                        Cooldown = 30f,
                        Range = 50f,
                        Animation = "channel",
                        Effect = "blizzard_aoe",
                        Condition = AbilityCondition.Always
                    },
                    new AbilityConfig
                    {
                        Name = "IceTomb",
                        Cooldown = 20f,
                        Range = 30f,
                        Animation = "cast",
                        Effect = "ice_tomb",
                        Condition = AbilityCondition.TargetInRange
                    },
                    new AbilityConfig
                    {
                        Name = "Shatter",
                        Cooldown = 15f,
                        Range = 10f,
                        Animation = "stomp",
                        Effect = "ice_shatter",
                        Condition = AbilityCondition.LowHealth
                    }
                },

                // Boss loot
                DropTable = "boss_frost_lord",
                Trophy = "TrophyFrostLord",
                PowerUnlock = "FrostLordPower",

                // Boss aura
                AmbientEffect = "boss_frost_aura"
            });

            // Register boss loot table
            LootTable.Register("boss_frost_lord", new LootTableConfig
            {
                MinDrops = 5,
                MaxDrops = 8,
                BonusDropsPerPlayer = 1f,
                Entries = new()
                {
                    // Guaranteed drops
                    new LootEntry { Item = "Crystal", Chance = 1f, Min = 20, Max = 40 },
                    new LootEntry { Item = "FreezeGland", Chance = 1f, Min = 10, Max = 20 },

                    // Rare materials
                    new LootEntry { Item = "DragonTear", Chance = 0.5f, Min = 1, Max = 3 },

                    // Epic/Legendary gear
                    new LootEntry
                    {
                        Item = "SwordIron",
                        Chance = 0.3f,
                        Min = 1,
                        Max = 1,
                        AlwaysAffix = true,
                        Rarity = new RarityRoll
                        {
                            Minimum = Rarity.Epic,
                            EpicWeight = 80f,
                            LegendaryWeight = 20f
                        }
                    },
                    new LootEntry
                    {
                        Item = "ArmorIronChest",
                        Chance = 0.3f,
                        Min = 1,
                        Max = 1,
                        AlwaysAffix = true,
                        Rarity = new RarityRoll
                        {
                            Minimum = Rarity.Epic,
                            EpicWeight = 80f,
                            LegendaryWeight = 20f
                        }
                    }
                }
            });

            Plugin.Log.LogDebug("Registered Frost_Lord boss");
        }
    }
}
