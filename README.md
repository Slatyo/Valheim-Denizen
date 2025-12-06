# Denizen

Custom entities, NPCs, and creatures framework for Valheim. Create intelligent NPCs, enemies, bosses, and companions with advanced AI behaviors.

## Features

- **NPCs**: Friendly characters with dialogue, services (trading, gambling, enchanting), and schedules
- **Enemies**: Custom hostile creatures with configurable aggression, group tactics, and abilities
- **Bosses**: Multi-phase boss encounters with arenas, abilities, and unique mechanics
- **Companions**: Player-controlled allies that follow, guard, attack, or stay on command
- **AI System**: State machine with patrol, combat, flee, and group coordination behaviors
- **Loot Tables**: Configurable drop tables with Affix integration for rarity-based loot
- **Level Scaling**: Uses Vital for entity levels and stat scaling
- **Combat Integration**: Uses Prime for stats, abilities, and combat events

## Entity Types

| Type | Description | Features |
|------|-------------|----------|
| NPC | Friendly characters | Dialogue, services, schedules |
| Enemy | Hostile creatures | Aggression, group AI, abilities |
| Boss | Major encounters | Phases, arenas, unique mechanics |
| Companion | Player allies | Commands (follow, guard, attack, stay) |

## Console Commands

Requires [Munin](https://github.com/Slatyo/Valheim-Munin) (optional dependency):

```
munin denizen spawn <id> [level] [count] - Spawn a denizen
munin denizen list [type]               - List registered denizens
munin denizen kill <id|all|nearby>      - Kill denizen entities
munin denizen info <id>                 - Show denizen info
munin denizen loot <id> [rolls]         - Test loot table rolls
```

## Dependencies

- [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
- [Prime](https://github.com/Slatyo/Valheim-Prime) - Combat engine for stats and abilities
- [Vital](https://github.com/Slatyo/Valheim-Vital) - Entity levels and stat scaling
- [Munin](https://github.com/Slatyo/Valheim-Munin) (optional) - Console commands
- [Veneer](https://github.com/Slatyo/Valheim-Veneer) (optional) - UI for NPC dialogue
- [Spark](https://github.com/Slatyo/Valheim-Spark) (optional) - Boss VFX and auras
- [Affix](https://github.com/Slatyo/Valheim-Affix) (optional) - Rarity loot drops

## For Mod Developers

### Registering an NPC

```csharp
DenizenAPI.RegisterNPC("MyNPC", new NPCConfig
{
    Name = "$npc_myname",
    BasedOn = "Haldor",
    Services = new[] { NPCService.Trade, NPCService.Gamble },
    Dialogue = new DialogueConfig
    {
        Greeting = "$npc_myname_greeting",
        Farewell = "$npc_myname_farewell"
    }
});
```

### Registering an Enemy

```csharp
DenizenAPI.RegisterEnemy("MyEnemy", new EnemyConfig
{
    Name = "$enemy_myname",
    BasedOn = "Skeleton",
    Stats = new StatsConfig
    {
        Template = "MeleeWarrior",
        Overrides = { ["MaxHealth"] = 200f }
    },
    Behavior = new BehaviorConfig
    {
        Aggression = Aggression.Aggressive,
        GroupBehavior = GroupBehavior.Pack
    },
    DropTable = "MyEnemyLoot"
});
```

### Registering a Boss

```csharp
DenizenAPI.RegisterBoss("MyBoss", new BossConfig
{
    Name = "$boss_myname",
    BasedOn = "Eikthyr",
    Phases = new[]
    {
        new BossPhase { HealthThreshold = 0.75f, Abilities = new[] { "Ability1" } },
        new BossPhase { HealthThreshold = 0.50f, Abilities = new[] { "Ability2", "Ability3" } },
        new BossPhase { HealthThreshold = 0.25f, EnrageMultiplier = 1.5f }
    },
    Arena = new ArenaConfig { Radius = 30f, PreventEscape = true }
});
```

### Spawning Entities

```csharp
// Spawn single entity
Character enemy = DenizenAPI.Spawn("MyEnemy", position);

// Spawn with level
Character boss = DenizenAPI.Spawn("MyBoss", position, level: 5);

// Spawn group
List<Character> pack = DenizenAPI.SpawnGroup("MyEnemy", position, count: 5);

// Spawn boss with arena
Character bossWithArena = DenizenAPI.SpawnBoss("MyBoss", position);
```

### Companion Commands

```csharp
// Issue commands to companions
DenizenAPI.Command(companion, CompanionCommand.Follow);
DenizenAPI.Command(companion, CompanionCommand.Guard);
DenizenAPI.Command(companion, CompanionCommand.Attack, target);
DenizenAPI.Command(companion, CompanionCommand.Stay);

// Check companion state
bool following = DenizenAPI.IsFollowing(companion);
bool guarding = DenizenAPI.IsGuarding(companion);
```

### Loot Tables

```csharp
LootTable.Register("MyEnemyLoot", new LootTableConfig
{
    Entries = new[]
    {
        new LootEntry { ItemName = "Coins", Chance = 1.0f, MinCount = 5, MaxCount = 20 },
        new LootEntry { ItemName = "SwordIron", Chance = 0.1f, HasAffixes = true }
    }
});
```

## Installation

1. Install dependencies (BepInEx, Jotunn, Prime, Vital)
2. Download and extract to `BepInEx/plugins/Denizen/`

## License

MIT License
