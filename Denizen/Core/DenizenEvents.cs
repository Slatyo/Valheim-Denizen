using System;
using Denizen.Dialogue;

namespace Denizen.Core
{
    /// <summary>
    /// Events for the Denizen system.
    /// </summary>
    public static class DenizenEvents
    {
        // Spawning
        /// <summary>Fired when a denizen is spawned.</summary>
        public static event Action<Character, string> OnDenizenSpawned;

        /// <summary>Fired when a denizen is despawned/destroyed.</summary>
        public static event Action<Character, string> OnDenizenDespawned;

        // NPC Interaction
        /// <summary>Fired when a player interacts with an NPC.</summary>
        public static event Action<Character, Player> OnNPCInteract;

        /// <summary>Fired when a player opens an NPC service.</summary>
        public static event Action<Character, Player, string> OnServiceOpened;

        /// <summary>Fired when a dialogue node is shown.</summary>
        public static event Action<Character, Player, DialogueNode> OnDialogueNode;

        // Trading
        /// <summary>Fired when a trade is completed.</summary>
        public static event Action<Character, Player, ItemDrop.ItemData, int> OnTrade;

        /// <summary>Fired when a gamble is completed.</summary>
        public static event Action<Character, Player, ItemDrop.ItemData> OnGamble;

        /// <summary>Fired when a player wins a gamble.</summary>
        public static event Action<Character, Player> OnGambleWin;

        /// <summary>Fired when a player loses a gamble.</summary>
        public static event Action<Character, Player> OnGambleLoss;

        /// <summary>Fired when a dialogue action is triggered.</summary>
        public static event Action<Character, Player, string> OnDialogueAction;

        // Boss
        /// <summary>Fired when a boss spawns.</summary>
        public static event Action<Character> OnBossSpawned;

        /// <summary>Fired when a boss changes phase.</summary>
        public static event Action<Character, int> OnBossPhaseChange;

        /// <summary>Fired when a boss is defeated.</summary>
        public static event Action<Character, Player> OnBossDefeated;

        // Companion
        /// <summary>Fired when a companion is tamed.</summary>
        public static event Action<Character, Player> OnCompanionTamed;

        /// <summary>Fired when a companion receives a command.</summary>
        public static event Action<Character, Player, CompanionCommand> OnCompanionCommand;

        // AI Events
        /// <summary>Fired when AI state changes.</summary>
        public static event Action<Character, CreatureState, CreatureState> OnStateChanged;

        /// <summary>Fired when target changes.</summary>
        public static event Action<Character, Character, Character> OnTargetChanged;

        /// <summary>Fired when a reaction is triggered.</summary>
        public static event Action<Character, Reaction> OnReactionTriggered;

        /// <summary>Fired when an ability is used.</summary>
        public static event Action<Character, string> OnAbilityUsed;

        /// <summary>Fired when combat starts.</summary>
        public static event Action<Character, Character> OnCombatStart;

        /// <summary>Fired when combat ends.</summary>
        public static event Action<Character, string> OnCombatEnd;

        /// <summary>Fired when flee starts.</summary>
        public static event Action<Character> OnFleeStart;

        // Combat events (from Prime integration)
        /// <summary>Fired when a denizen takes damage.</summary>
        public static event Action<Character, Character, float> OnDamageTaken;

        /// <summary>Fired when a denizen is killed.</summary>
        public static event Action<Character, Character> OnDenizenKilled;

        // Internal raise methods
        internal static void RaiseSpawned(Character character, string denizenId)
        {
            OnDenizenSpawned?.Invoke(character, denizenId);
        }

        internal static void RaiseDespawned(Character character, string denizenId)
        {
            OnDenizenDespawned?.Invoke(character, denizenId);
        }

        internal static void RaiseNPCInteract(Character npc, Player player)
        {
            OnNPCInteract?.Invoke(npc, player);
        }

        internal static void RaiseServiceOpened(Character npc, Player player, string serviceId)
        {
            OnServiceOpened?.Invoke(npc, player, serviceId);
        }

        internal static void RaiseDialogueNode(Character npc, Player player, DialogueNode node)
        {
            OnDialogueNode?.Invoke(npc, player, node);
        }

        internal static void RaiseTrade(Character npc, Player player, ItemDrop.ItemData item, int price)
        {
            OnTrade?.Invoke(npc, player, item, price);
        }

        internal static void RaiseGamble(Character npc, Player player, ItemDrop.ItemData item)
        {
            OnGamble?.Invoke(npc, player, item);
        }

        internal static void RaiseGambleWin(Character npc, Player player)
        {
            OnGambleWin?.Invoke(npc, player);
        }

        internal static void RaiseGambleLoss(Character npc, Player player)
        {
            OnGambleLoss?.Invoke(npc, player);
        }

        internal static void RaiseDialogueAction(Character npc, Player player, string action)
        {
            OnDialogueAction?.Invoke(npc, player, action);
        }

        internal static void RaiseBossSpawned(Character boss)
        {
            OnBossSpawned?.Invoke(boss);
        }

        internal static void RaiseBossPhaseChange(Character boss, int phase)
        {
            OnBossPhaseChange?.Invoke(boss, phase);
        }

        internal static void RaiseBossDefeated(Character boss, Player killer)
        {
            OnBossDefeated?.Invoke(boss, killer);
        }

        internal static void RaiseCompanionTamed(Character companion, Player tamer)
        {
            OnCompanionTamed?.Invoke(companion, tamer);
        }

        internal static void RaiseCompanionCommand(Character companion, Player commander, CompanionCommand command)
        {
            OnCompanionCommand?.Invoke(companion, commander, command);
        }

        internal static void RaiseStateChanged(Character character, CreatureState oldState, CreatureState newState)
        {
            OnStateChanged?.Invoke(character, oldState, newState);
        }

        internal static void RaiseTargetChanged(Character character, Character oldTarget, Character newTarget)
        {
            OnTargetChanged?.Invoke(character, oldTarget, newTarget);
        }

        internal static void RaiseReactionTriggered(Character character, Reaction reaction)
        {
            OnReactionTriggered?.Invoke(character, reaction);
        }

        internal static void RaiseAbilityUsed(Character character, string abilityName)
        {
            OnAbilityUsed?.Invoke(character, abilityName);
        }

        internal static void RaiseCombatStart(Character character, Character target)
        {
            OnCombatStart?.Invoke(character, target);
        }

        internal static void RaiseCombatEnd(Character character, string reason)
        {
            OnCombatEnd?.Invoke(character, reason);
        }

        internal static void RaiseFleeStart(Character character)
        {
            OnFleeStart?.Invoke(character);
        }

        internal static void RaiseDamageTaken(Character victim, Character attacker, float damage)
        {
            OnDamageTaken?.Invoke(victim, attacker, damage);
        }

        internal static void RaiseDenizenKilled(Character victim, Character killer)
        {
            OnDenizenKilled?.Invoke(victim, killer);
        }
    }
}
