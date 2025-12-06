using Denizen.Core;
using Denizen.Dialogue;
using Denizen.Entities;
using Denizen.NPC;
using UnityEngine;

namespace Denizen.Examples
{
    /// <summary>
    /// Example NPC: Eldric the Enchanter.
    /// A friendly NPC that offers trading and enchanting services.
    /// </summary>
    public static class ExampleNPC
    {
        public static void Register()
        {
            // Register the NPC
            DenizenAPI.RegisterNPC("Enchanter_Eldric", new NPCConfig
            {
                Name = "$npc_enchanter_name",
                Title = "$npc_enchanter_title",

                // Clone from Haldor (the vanilla trader) - internal name is Haldor
                BasedOn = "Haldor",
                // If Haldor fails, we can fall back to a humanoid
                Prefab = null,

                // Spawn configuration
                Spawning = new SpawnConfig
                {
                    Type = SpawnType.Fixed,
                    Biomes = new() { Heightmap.Biome.Meadows },
                    Unique = true
                },

                // Stats (via Prime)
                Stats = new StatConfig
                {
                    Template = "NPC",
                    Overrides = new()
                    {
                        ["Health"] = 500,
                        ["Armor"] = 50
                    }
                },

                // Passive behavior, flees when hurt
                Behavior = new BehaviorConfig
                {
                    Aggression = Aggression.Passive,
                    FleeThreshold = 0.5f,
                    Reactions = new()
                    {
                        new ReactionConfig(Reaction.Flee, Trigger.OnDamaged)
                    }
                },

                // Services offered
                Services = new()
                {
                    new TradeService
                    {
                        Label = "$service_trade",
                        ShopTable = "shop_enchanter",
                        Currency = "Coins",
                        PriceMultiplier = 1.5f
                    },
                    new CustomService("enchant", "$service_enchant", OnEnchantOpen)
                },

                // Dialogue
                Dialogue = new DialogueConfig
                {
                    Greeting = "$npc_enchanter_greeting",
                    Farewell = "$npc_enchanter_farewell",
                    Idle = new()
                    {
                        "$npc_enchanter_idle_1",
                        "$npc_enchanter_idle_2"
                    }
                }
            });

            // Register dialogue tree for advanced conversation
            RegisterDialogue();

            // Register loot table for shop inventory
            RegisterShopInventory();

            Plugin.Log.LogDebug("Registered Enchanter_Eldric NPC");
        }

        private static void OnEnchantOpen(Character npc, Player player)
        {
            // TODO: Open enchanting UI via Veneer
            Plugin.Log.LogDebug("Opening enchant service");

            // Show message for now
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center,
                "Enchanting service coming soon!");
        }

        private static void RegisterDialogue()
        {
            DialogueSystem.Register("Enchanter_Eldric", new DialogueTree
            {
                Root = new DialogueNode
                {
                    Text = "$npc_enchanter_greeting",
                    Options = new()
                    {
                        new DialogueOption
                        {
                            Text = "$service_trade",
                            Action = DialogueAction.OpenService("trade")
                        },
                        new DialogueOption
                        {
                            Text = "$service_enchant",
                            Action = DialogueAction.OpenService("enchant")
                        },
                        new DialogueOption
                        {
                            Text = "Tell me about enchanting",
                            Next = "lore_enchanting",
                            Condition = Conditions.HasKilledBoss("defeated_eikthyr")
                        },
                        new DialogueOption
                        {
                            Text = "Goodbye",
                            Action = DialogueAction.Close
                        }
                    }
                },
                Branches = new()
                {
                    ["lore_enchanting"] = new DialogueNode
                    {
                        Text = "The runes hold ancient power. With the right materials, I can bind them to your weapons and armor.",
                        Options = new()
                        {
                            new DialogueOption
                            {
                                Text = "Back",
                                Next = "root"
                            }
                        }
                    }
                }
            });
        }

        private static void RegisterShopInventory()
        {
            Loot.LootTable.Register("shop_enchanter", new Loot.LootTableConfig
            {
                MinDrops = 5,
                MaxDrops = 10,
                Entries = new()
                {
                    new Loot.LootEntry { Item = "Ruby", Chance = 1f, Min = 1, Max = 3 },
                    new Loot.LootEntry { Item = "Amber", Chance = 1f, Min = 2, Max = 5 },
                    new Loot.LootEntry { Item = "AmberPearl", Chance = 0.8f, Min = 1, Max = 2 },
                    new Loot.LootEntry { Item = "SurtlingCore", Chance = 0.5f, Min = 1, Max = 2 }
                }
            });
        }
    }
}
