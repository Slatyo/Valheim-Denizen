namespace Denizen.Core
{
    /// <summary>
    /// Type of denizen entity.
    /// </summary>
    public enum DenizenType
    {
        /// <summary>Friendly, interactable NPC (traders, quest givers).</summary>
        NPC,
        /// <summary>Hostile creature.</summary>
        Enemy,
        /// <summary>Special enemy with mechanics and phases.</summary>
        Boss,
        /// <summary>Tamed/follower entity.</summary>
        Companion,
        /// <summary>Non-hostile, non-interactive ambient creature.</summary>
        Neutral
    }

    /// <summary>
    /// Aggression behavior type.
    /// </summary>
    public enum Aggression
    {
        /// <summary>Won't attack unless attacked.</summary>
        Passive,
        /// <summary>Approaches carefully, retreats if hurt.</summary>
        Cautious,
        /// <summary>Standard Valheim behavior.</summary>
        Normal,
        /// <summary>Attacks on sight, pursues far.</summary>
        Aggressive,
        /// <summary>All-in, never retreats, enrages when hurt.</summary>
        Berserker,
        /// <summary>Uses abilities smartly, kites, flanks.</summary>
        Tactical
    }

    /// <summary>
    /// Group behavior type.
    /// </summary>
    public enum GroupBehavior
    {
        /// <summary>Fights alone, ignores allies.</summary>
        Solo,
        /// <summary>Attacks together, shares aggro.</summary>
        Pack,
        /// <summary>Surrounds target, attacks from all sides.</summary>
        Swarm,
        /// <summary>Maintains positions, tanks front.</summary>
        Formation,
        /// <summary>Stays back, buffs/heals allies.</summary>
        Support
    }

    /// <summary>
    /// Reaction type for AI triggers.
    /// </summary>
    public enum Reaction
    {
        // Defensive
        /// <summary>Run away from combat.</summary>
        Flee,
        /// <summary>Sidestep incoming attacks.</summary>
        Dodge,
        /// <summary>Raise shield/guard.</summary>
        Block,
        /// <summary>Timed block for counter attack.</summary>
        Parry,

        // Social
        /// <summary>Alert nearby allies.</summary>
        CallForHelp,
        /// <summary>Defend weaker allies.</summary>
        Protect,
        /// <summary>Enrage when ally dies.</summary>
        Avenge,

        // Offensive
        /// <summary>Boost damage when hurt.</summary>
        Enrage,
        /// <summary>Rush at target.</summary>
        Charge,
        /// <summary>Wait for opportunity.</summary>
        Ambush,

        // Utility
        /// <summary>Use healing if available.</summary>
        Heal,
        /// <summary>Move to better position.</summary>
        Reposition,
        /// <summary>Tactical retreat, return later.</summary>
        Retreat
    }

    /// <summary>
    /// Trigger conditions for reactions.
    /// </summary>
    public enum Trigger
    {
        /// <summary>When entity takes damage.</summary>
        OnDamaged,
        /// <summary>When health drops below threshold.</summary>
        OnLowHealth,
        /// <summary>When ally takes damage.</summary>
        OnAllyDamaged,
        /// <summary>When ally health drops below threshold.</summary>
        OnAllyLowHealth,
        /// <summary>When ally dies.</summary>
        OnAllyDeath,
        /// <summary>When target gets too close.</summary>
        OnTargetTooClose,
        /// <summary>When target gets too far.</summary>
        OnTargetTooFar,
        /// <summary>When detecting incoming attack.</summary>
        OnIncomingAttack,
        /// <summary>When entering combat.</summary>
        OnCombatStart,
        /// <summary>When combat ends.</summary>
        OnCombatEnd,
        /// <summary>At regular intervals.</summary>
        OnTimer,
        /// <summary>When entity spawns.</summary>
        OnSpawn
    }

    /// <summary>
    /// Target priority for AI.
    /// </summary>
    public enum TargetPriority
    {
        /// <summary>Closest target.</summary>
        Nearest,
        /// <summary>Finish off weak targets.</summary>
        LowestHealth,
        /// <summary>Focus tanks.</summary>
        HighestHealth,
        /// <summary>Easiest to damage.</summary>
        LowestArmor,
        /// <summary>Who's dealing most damage.</summary>
        HighestThreat,
        /// <summary>Unpredictable targeting.</summary>
        Random,
        /// <summary>Revenge targeting.</summary>
        LastAttacker,
        /// <summary>Prioritize support classes.</summary>
        Healer,
        /// <summary>Prioritize ranged attackers.</summary>
        Ranged
    }

    /// <summary>
    /// Creature AI state.
    /// </summary>
    public enum CreatureState
    {
        /// <summary>Idle, not doing anything.</summary>
        Idle,
        /// <summary>Walking a patrol route.</summary>
        Patrol,
        /// <summary>Heard something, on guard.</summary>
        Alert,
        /// <summary>Going to check something out.</summary>
        Investigate,
        /// <summary>Actively fighting.</summary>
        Combat,
        /// <summary>Running away.</summary>
        Fleeing,
        /// <summary>Tactical retreat, will return.</summary>
        Retreating,
        /// <summary>Dead.</summary>
        Dead
    }

    /// <summary>
    /// Preferred combat range.
    /// </summary>
    public enum CombatRange
    {
        /// <summary>Close quarters combat.</summary>
        Melee,
        /// <summary>Mid-range combat.</summary>
        Medium,
        /// <summary>Long-range combat.</summary>
        Ranged
    }

    /// <summary>
    /// Spawn type for entities.
    /// </summary>
    public enum SpawnType
    {
        /// <summary>Fixed location in world.</summary>
        Fixed,
        /// <summary>Random natural spawning.</summary>
        Natural,
        /// <summary>Spawns inside structures.</summary>
        Structure,
        /// <summary>Only spawned via API.</summary>
        Manual
    }

    /// <summary>
    /// Combat style for companion AI.
    /// </summary>
    public enum FriendlyCombat
    {
        /// <summary>Never attacks.</summary>
        Passive,
        /// <summary>Only attacks if owner/self attacked.</summary>
        Defensive,
        /// <summary>Attacks anything that attacks owner.</summary>
        Protective,
        /// <summary>Attacks any hostile in range.</summary>
        Aggressive,
        /// <summary>Actively seeks out prey.</summary>
        Hunt
    }

    /// <summary>
    /// Commands for companions.
    /// </summary>
    public enum CompanionCommand
    {
        /// <summary>Follow the owner.</summary>
        Follow,
        /// <summary>Stay at current position.</summary>
        Stay,
        /// <summary>Attack a target.</summary>
        Attack,
        /// <summary>Guard a position.</summary>
        Guard,
        /// <summary>Don't attack anything.</summary>
        Passive
    }

    /// <summary>
    /// Idle behavior for companions.
    /// </summary>
    public enum IdleBehavior
    {
        /// <summary>Stand still.</summary>
        Stationary,
        /// <summary>Wander around.</summary>
        Wander,
        /// <summary>Play idle animations.</summary>
        Animate
    }

    /// <summary>
    /// Ability use condition.
    /// </summary>
    public enum AbilityCondition
    {
        /// <summary>Always use when off cooldown.</summary>
        Always,
        /// <summary>Target is in ability range.</summary>
        TargetInRange,
        /// <summary>Target is out of melee range.</summary>
        TargetOutOfMelee,
        /// <summary>Multiple targets nearby.</summary>
        MultipleTargets,
        /// <summary>Own health is low.</summary>
        LowHealth,
        /// <summary>Own health is high.</summary>
        HighHealth,
        /// <summary>Ally is nearby.</summary>
        AllyNearby,
        /// <summary>Ally health is low.</summary>
        AllyLowHealth,
        /// <summary>No allies nearby.</summary>
        NoAlliesNearby
    }

    /// <summary>
    /// Gamble service types for NPCs.
    /// </summary>
    public enum GambleType
    {
        /// <summary>Reroll item affixes.</summary>
        RerollAffixes,
        /// <summary>Get a random item.</summary>
        RandomItem,
        /// <summary>Upgrade item rarity.</summary>
        Enchant
    }
}
