using Denizen.Core;
using Denizen.Entities;
using Denizen.Loot;

namespace Denizen.Examples
{
    /// <summary>
    /// Example Enemy: Frost Golem.
    /// A tough enemy with frost abilities that enrages at low health.
    /// </summary>
    public static class ExampleEnemy
    {
        public static void Register()
        {
            // Register the Frost Golem
            DenizenAPI.RegisterEnemy("Frost_Golem", new EnemyConfig
            {
                Name = "$enemy_frost_golem",

                // Clone from Stone Golem
                BasedOn = "StoneGolem",
                Scale = 1.5f,

                // Natural spawn in Mountain biome
                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Natural,
                    Biomes = new() { Heightmap.Biome.Mountain, Heightmap.Biome.DeepNorth },
                    SpawnChance = 0.08f,
                    GroupMin = 1,
                    GroupMax = 2,
                    LevelChance = 0.15f,
                    MinAltitude = 100f
                },

                // Stats (via Prime)
                Stats = new StatConfig
                {
                    Template = "Creature",
                    Overrides = new()
                    {
                        ["Health"] = 800,
                        ["Damage"] = 90,
                        ["Armor"] = 30,
                        ["FrostDamage"] = 40,
                        ["FrostResist"] = 0.8f,   // 80% frost resist
                        ["FireResist"] = -0.5f,   // Weak to fire (takes 50% more)
                        ["LightningResist"] = 0f, // No lightning resist
                        ["PoisonResist"] = 0f,    // No poison resist
                        ["SpiritResist"] = -0.25f // Slightly weak to spirit
                    }
                },

                // Aggressive solo fighter that enrages
                Behavior = new BehaviorConfig
                {
                    Aggression = Aggression.Aggressive,
                    GroupBehavior = GroupBehavior.Solo,
                    FleeThreshold = 0f, // Never flees
                    Targeting = TargetPriority.Nearest,
                    PreferredRange = CombatRange.Melee,
                    Reactions = new()
                    {
                        new ReactionConfig
                        {
                            Reaction = Reaction.Enrage,
                            Trigger = Trigger.OnLowHealth,
                            Threshold = 0.3f,
                            Effect = "damage_multiply:1.5"
                        }
                    }
                },

                // Abilities
                Abilities = new()
                {
                    new AbilityConfig
                    {
                        Name = "FrostBreath",
                        Cooldown = 8f,
                        Range = 10f,
                        MinRange = 3f,
                        Animation = "breath_attack",
                        Effect = "frost_breath_vfx",
                        Condition = AbilityCondition.TargetInRange
                    },
                    new AbilityConfig
                    {
                        Name = "IceSpikes",
                        Cooldown = 15f,
                        Range = 20f,
                        Animation = "ground_slam",
                        Effect = "ice_spikes_vfx",
                        Condition = AbilityCondition.MultipleTargets
                    }
                },

                // Loot
                DropTable = "frost_golem_drops",

                // VFX
                AmbientEffect = "frost_aura"
            });

            // Register loot table
            LootTable.Register("frost_golem_drops", new LootTableConfig
            {
                MinDrops = 2,
                MaxDrops = 4,
                BonusDropsPerPlayer = 0.5f,
                Entries = new()
                {
                    new LootEntry { Item = "Stone", Chance = 1f, Min = 5, Max = 15 },
                    new LootEntry { Item = "Crystal", Chance = 0.5f, Min = 1, Max = 3 },
                    new LootEntry { Item = "FreezeGland", Chance = 0.3f, Min = 1, Max = 2 },
                    // Guaranteed epic/legendary sword drop for testing
                    new LootEntry
                    {
                        Item = "SwordIron",
                        Chance = 1f, // 100% drop
                        Min = 1,
                        Max = 1,
                        AlwaysAffix = true,
                        Rarity = new RarityRoll
                        {
                            Minimum = Rarity.Epic, // At least Epic
                            CommonWeight = 0f,
                            UncommonWeight = 0f,
                            RareWeight = 0f,
                            EpicWeight = 50f,
                            LegendaryWeight = 50f // 50/50 Epic or Legendary
                        }
                    }
                }
            });

            // Also register a smarter archer variant
            RegisterSmartArcher();

            Plugin.Log.LogDebug("Registered Frost_Golem enemy");
        }

        private static void RegisterSmartArcher()
        {
            DenizenAPI.RegisterEnemy("Smart_Archer", new EnemyConfig
            {
                Name = "$enemy_smart_archer",

                // Clone from Draugr_Ranged for bow attacks
                BasedOn = "Draugr_Ranged",

                // Natural spawn
                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Natural,
                    Biomes = new() { Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp },
                    SpawnChance = 0.1f,
                    GroupMin = 2,
                    GroupMax = 4,
                    LevelChance = 0.1f
                },

                // Stats - tougher than regular draugr
                Stats = new StatConfig
                {
                    Template = "Creature",
                    Overrides = new()
                    {
                        ["Health"] = 120,
                        ["Damage"] = 35,
                        ["Armor"] = 10,
                        ["AttackSpeed"] = 1.3f
                    }
                },

                // Smart tactical behavior
                Behavior = new BehaviorConfig
                {
                    Aggression = Aggression.Tactical,
                    GroupBehavior = GroupBehavior.Pack,
                    FleeThreshold = 0f,  // Never flee - stand and fight
                    Targeting = TargetPriority.Nearest,
                    PreferredRange = CombatRange.Ranged,
                    KeepDistance = true,
                    IdealDistance = 10f,
                    CallHelpRange = 25f,
                    Reactions = new()
                    {
                        // High chance to dodge player attacks
                        new ReactionConfig
                        {
                            Reaction = Reaction.Dodge,
                            Trigger = Trigger.OnIncomingAttack,
                            Chance = 0.6f,  // 60% dodge chance
                            Cooldown = 1.5f
                        },
                        // Block if dodge on cooldown
                        new ReactionConfig
                        {
                            Reaction = Reaction.Block,
                            Trigger = Trigger.OnIncomingAttack,
                            Chance = 0.4f,
                            Cooldown = 3f
                        },
                        // Back off if player gets too close
                        new ReactionConfig
                        {
                            Reaction = Reaction.Retreat,
                            Trigger = Trigger.OnTargetTooClose,
                            Distance = 4f,
                            Cooldown = 3f
                        },
                        // Call allies when hit
                        new ReactionConfig
                        {
                            Reaction = Reaction.CallForHelp,
                            Trigger = Trigger.OnDamaged,
                            Cooldown = 10f
                        }
                    }
                },

                // Basic loot
                DropTable = "skeleton_basic"
            });

            // Simple skeleton loot
            LootTable.Register("skeleton_basic", new LootTableConfig
            {
                MinDrops = 1,
                MaxDrops = 2,
                Entries = new()
                {
                    new LootEntry { Item = "BoneFragments", Chance = 1f, Min = 1, Max = 3 },
                    new LootEntry { Item = "ArrowWood", Chance = 0.5f, Min = 5, Max = 10 }
                }
            });
        }
    }
}
