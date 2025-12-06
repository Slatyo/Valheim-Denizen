using System.Collections.Generic;
using System.Linq;
using Denizen.Core;
using Denizen.Loot;
using UnityEngine;

namespace Denizen.Commands
{
    /// <summary>
    /// Munin command integration for Denizen.
    /// All commands use: munin denizen [command] [args]
    /// </summary>
    internal static class DenizenCommands
    {
        internal static void Register()
        {
            if (!Plugin.HasMunin)
            {
                Plugin.Log.LogDebug("Munin not available, skipping command registration");
                return;
            }

            try
            {
                RegisterWithMunin();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to register Munin commands: {ex.Message}");
            }
        }

        private static void RegisterWithMunin()
        {
            Munin.Command.Register("denizen", new Munin.CommandConfig
            {
                Name = "spawn",
                Description = "Spawn a denizen entity",
                Usage = "<id> [level] [count]",
                Permission = Munin.PermissionLevel.Admin,
                Examples = new[] { "ExampleEnemy", "ExampleBoss 2", "ExampleEnemy 1 5" },
                Handler = SpawnHandler
            });

            Munin.Command.Register("denizen", new Munin.CommandConfig
            {
                Name = "list",
                Description = "List registered denizens",
                Usage = "[npc|enemy|boss|companion]",
                Permission = Munin.PermissionLevel.Anyone,
                Examples = new[] { "", "npc", "boss" },
                Handler = ListHandler
            });

            Munin.Command.Register("denizen", new Munin.CommandConfig
            {
                Name = "kill",
                Description = "Kill denizen entities",
                Usage = "<id|all|nearby>",
                Permission = Munin.PermissionLevel.Admin,
                Examples = new[] { "all", "nearby", "ExampleEnemy" },
                Handler = KillHandler
            });

            Munin.Command.Register("denizen", new Munin.CommandConfig
            {
                Name = "info",
                Description = "Show denizen info",
                Usage = "<id>",
                Permission = Munin.PermissionLevel.Anyone,
                Examples = new[] { "ExampleEnemy", "ExampleBoss" },
                Handler = InfoHandler
            });

            Munin.Command.Register("denizen", new Munin.CommandConfig
            {
                Name = "loot",
                Description = "Test loot table rolls",
                Usage = "<creatureId> [rolls]",
                Permission = Munin.PermissionLevel.Admin,
                Examples = new[] { "ExampleEnemy", "ExampleBoss 10" },
                Handler = LootHandler
            });

            Plugin.Log.LogInfo("Denizen commands registered with Munin");
        }

        private static Munin.CommandResult SpawnHandler(Munin.CommandArgs args)
        {
            if (!args.HasRequired(1))
            {
                return Munin.CommandResult.Error("Usage: munin denizen spawn <id> [level] [count]");
            }

            string id = args.Get(0);
            int level = args.Get<int>(1, 0);
            int count = args.Get<int>(2, 1);

            if (!DenizenRegistry.Instance.Has(id))
            {
                var available = string.Join(", ", DenizenRegistry.Instance.GetAllIds().Take(10));
                return Munin.CommandResult.Error($"Denizen not found: {id}\nAvailable: {available}");
            }

            var player = args.Player;
            if (player == null)
            {
                return Munin.CommandResult.Error("No local player");
            }

            Vector3 position = player.transform.position + player.transform.forward * 5f;

            if (count == 1)
            {
                var spawned = DenizenSpawner.Spawn(id, position, Quaternion.identity, level);
                if (spawned != null)
                {
                    return Munin.CommandResult.Success($"Spawned {id} at {position:F0}");
                }
                return Munin.CommandResult.Error($"Failed to spawn {id}");
            }
            else
            {
                var spawned = DenizenSpawner.SpawnGroup(id, position, count);
                return Munin.CommandResult.Success($"Spawned {spawned.Count} x {id}");
            }
        }

        private static Munin.CommandResult ListHandler(Munin.CommandArgs args)
        {
            string filter = args.Get(0)?.ToLower();
            var lines = new List<string> { "=== Registered Denizens ===" };

            if (filter == null || filter == "npc")
            {
                var npcs = DenizenRegistry.Instance.GetNPCIds().ToList();
                lines.Add($"NPCs ({npcs.Count}): {string.Join(", ", npcs)}");
            }

            if (filter == null || filter == "enemy")
            {
                var enemies = DenizenRegistry.Instance.GetEnemyIds().ToList();
                lines.Add($"Enemies ({enemies.Count}): {string.Join(", ", enemies)}");
            }

            if (filter == null || filter == "boss")
            {
                var bosses = DenizenRegistry.Instance.GetBossIds().ToList();
                lines.Add($"Bosses ({bosses.Count}): {string.Join(", ", bosses)}");
            }

            if (filter == null || filter == "companion")
            {
                var companions = DenizenRegistry.Instance.GetCompanionIds().ToList();
                lines.Add($"Companions ({companions.Count}): {string.Join(", ", companions)}");
            }

            lines.Add($"Total: {DenizenRegistry.Instance.Count}");

            return Munin.CommandResult.Info(string.Join("\n", lines));
        }

        private static Munin.CommandResult KillHandler(Munin.CommandArgs args)
        {
            if (!args.HasRequired(1))
            {
                return Munin.CommandResult.Error("Usage: munin denizen kill <id|all|nearby>");
            }

            string target = args.Get(0).ToLower();
            int killed = 0;

            var player = args.Player;
            var allCharacters = Character.GetAllCharacters();

            foreach (var character in allCharacters)
            {
                if (character == null || character.IsDead()) continue;

                var denizen = character.GetComponent<DenizenComponent>();
                if (denizen == null) continue;

                bool shouldKill = target switch
                {
                    "all" => true,
                    "nearby" => player != null && Vector3.Distance(character.transform.position, player.transform.position) < 50f,
                    _ => denizen.DenizenId == target
                };

                if (shouldKill)
                {
                    character.SetHealth(0);
                    killed++;
                }
            }

            return Munin.CommandResult.Success($"Killed {killed} denizens");
        }

        private static Munin.CommandResult InfoHandler(Munin.CommandArgs args)
        {
            if (!args.HasRequired(1))
            {
                return Munin.CommandResult.Error("Usage: munin denizen info <id>");
            }

            string id = args.Get(0);
            var config = DenizenRegistry.Instance.Get(id);

            if (config == null)
            {
                return Munin.CommandResult.Error($"Denizen not found: {id}");
            }

            var lines = new List<string>
            {
                $"=== {id} ===",
                $"Type: {config.Type}",
                $"Name: {config.Name}",
                $"BasedOn: {config.BasedOn ?? "Custom"}",
                $"Scale: {config.Scale}"
            };

            if (config.Stats != null)
            {
                lines.Add($"Stats Template: {config.Stats.Template}");
                foreach (var stat in config.Stats.Overrides)
                {
                    lines.Add($"  {stat.Key}: {stat.Value}");
                }
            }

            if (config.Behavior != null)
            {
                lines.Add($"Aggression: {config.Behavior.Aggression}");
                lines.Add($"GroupBehavior: {config.Behavior.GroupBehavior}");
                lines.Add($"FleeThreshold: {config.Behavior.FleeThreshold}");
            }

            if (config.Abilities?.Count > 0)
            {
                lines.Add($"Abilities: {string.Join(", ", config.Abilities.Select(a => a.Name))}");
            }

            lines.Add($"DropTable: {config.DropTable ?? "None"}");

            return Munin.CommandResult.Info(string.Join("\n", lines));
        }

        private static Munin.CommandResult LootHandler(Munin.CommandArgs args)
        {
            if (!args.HasRequired(1))
            {
                var tables = string.Join(", ", LootTable.GetAllIds().Take(10));
                return Munin.CommandResult.Error($"Usage: munin denizen loot <creatureId> [rolls]\nAvailable: {tables}");
            }

            string creatureId = args.Get(0);
            int rolls = args.Get<int>(1, 1);

            if (!LootTable.Has(creatureId))
            {
                return Munin.CommandResult.Error($"No loot table for: {creatureId}");
            }

            var lines = new List<string> { $"=== Loot Test: {creatureId} ({rolls} rolls) ===" };
            var totals = new Dictionary<string, int>();

            for (int i = 0; i < rolls; i++)
            {
                var results = LootTable.Roll(creatureId);
                foreach (var result in results)
                {
                    string key = result.HasAffixes
                        ? $"{result.ItemName} ({result.AffixRarity})"
                        : result.ItemName;

                    if (!totals.ContainsKey(key))
                        totals[key] = 0;
                    totals[key] += result.Count;
                }
            }

            foreach (var kvp in totals.OrderByDescending(x => x.Value))
            {
                lines.Add($"  {kvp.Key}: {kvp.Value}");
            }

            if (totals.Count == 0)
            {
                lines.Add("  (no drops)");
            }

            return Munin.CommandResult.Info(string.Join("\n", lines));
        }
    }
}
