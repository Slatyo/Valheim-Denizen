using System.Collections.Generic;
using Denizen.Core;
using Denizen.Entities;
using Denizen.Integration;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// Base AI component for Denizen entities.
    /// Manages state machine and behavior.
    /// </summary>
    public class DenizenAI : MonoBehaviour
    {
        protected DenizenComponent _denizen;
        protected Character _character;
        protected MonsterAI _baseAI;
        protected ZNetView _zNetView;

        /// <summary>Current AI state.</summary>
        public CreatureState CurrentState { get; protected set; } = CreatureState.Idle;

        /// <summary>Current target.</summary>
        public Character CurrentTarget { get; protected set; }

        /// <summary>Behavior configuration.</summary>
        protected BehaviorConfig _behavior;

        /// <summary>Ability cooldowns.</summary>
        protected Dictionary<string, float> _abilityCooldowns = new();

        /// <summary>Reaction cooldowns.</summary>
        protected Dictionary<Reaction, float> _reactionCooldowns = new();

        /// <summary>Time since last state change.</summary>
        protected float _stateTime;

        /// <summary>Time since combat started.</summary>
        protected float _combatTime;

        /// <summary>Whether currently in combat.</summary>
        protected bool _inCombat;

        protected virtual void Awake()
        {
            _denizen = GetComponent<DenizenComponent>();
            _character = GetComponent<Character>();
            _baseAI = GetComponent<MonsterAI>();
            _zNetView = GetComponent<ZNetView>();
        }

        protected virtual void Start()
        {
            if (_denizen?.Config?.Behavior != null)
            {
                _behavior = _denizen.Config.Behavior;
            }
            else
            {
                _behavior = new BehaviorConfig();
            }
        }

        protected virtual void Update()
        {
            // Safety: check if we're valid
            if (this == null) return;
            if (_character == null || !_character) return;
            if (_zNetView != null && !_zNetView.IsOwner()) return;

            _stateTime += Time.deltaTime;

            // Only run AI logic if behavior is configured
            if (_behavior == null) return;

            // Simple state update without complex targeting
            if (CurrentState == CreatureState.Idle)
            {
                // Just check if we should be in combat based on vanilla AI target
                if (_baseAI != null && _baseAI.GetTargetCreature() != null)
                {
                    CurrentTarget = _baseAI.GetTargetCreature();
                    CurrentState = CreatureState.Combat;
                    _inCombat = true;
                }
            }
            else if (CurrentState == CreatureState.Combat)
            {
                // Sync target from vanilla AI
                if (_baseAI != null)
                {
                    CurrentTarget = _baseAI.GetTargetCreature();
                }

                // Check if we should exit combat
                if (CurrentTarget == null || !CurrentTarget || CurrentTarget.IsDead())
                {
                    CurrentState = CreatureState.Idle;
                    CurrentTarget = null;
                    _inCombat = false;
                }
            }
        }

        /// <summary>
        /// Update the current state.
        /// </summary>
        protected virtual void UpdateState()
        {
            switch (CurrentState)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Patrol:
                    UpdatePatrol();
                    break;
                case CreatureState.Alert:
                    UpdateAlert();
                    break;
                case CreatureState.Investigate:
                    UpdateInvestigate();
                    break;
                case CreatureState.Combat:
                    UpdateCombat();
                    break;
                case CreatureState.Fleeing:
                    UpdateFleeing();
                    break;
                case CreatureState.Retreating:
                    UpdateRetreating();
                    break;
            }
        }

        protected virtual void UpdateIdle()
        {
            // Check for threats
            var threat = FindThreat();
            if (threat != null)
            {
                SetTarget(threat);
                EnterCombat(threat);
            }
        }

        protected virtual void UpdatePatrol()
        {
            // Check for threats
            var threat = FindThreat();
            if (threat != null)
            {
                SetTarget(threat);
                SetState(CreatureState.Alert);
            }
        }

        protected virtual void UpdateAlert()
        {
            if (CurrentTarget != null)
            {
                SetState(CreatureState.Combat);
            }
            else if (_stateTime > 5f)
            {
                SetState(CreatureState.Idle);
            }
        }

        protected virtual void UpdateInvestigate()
        {
            var threat = FindThreat();
            if (threat != null)
            {
                SetTarget(threat);
                EnterCombat(threat);
            }
            else if (_stateTime > 10f)
            {
                SetState(CreatureState.Idle);
            }
        }

        protected virtual void UpdateCombat()
        {
            _combatTime += Time.deltaTime;

            // Check if target is still valid
            if (CurrentTarget == null || CurrentTarget.IsDead())
            {
                CurrentTarget = FindThreat();
                if (CurrentTarget == null)
                {
                    ExitCombat("target_lost");
                    return;
                }
            }

            // Check flee threshold
            float healthPercent = _character.GetHealthPercentage();
            if (healthPercent < _behavior.FleeThreshold && _behavior.FleeThreshold > 0)
            {
                TriggerReaction(Reaction.Flee, Trigger.OnLowHealth);
            }

            // Try to use abilities
            TryUseAbilities();

            // Check for reaction triggers
            CheckReactionTriggers();
        }

        protected virtual void UpdateFleeing()
        {
            // Check if we can stop fleeing
            if (_stateTime > 10f)
            {
                float healthPercent = _character.GetHealthPercentage();
                if (healthPercent > _behavior.FleeThreshold * 1.5f)
                {
                    SetState(CreatureState.Idle);
                }
            }
        }

        protected virtual void UpdateRetreating()
        {
            if (_stateTime > 5f)
            {
                // Re-engage if target still visible
                if (CurrentTarget != null && !CurrentTarget.IsDead())
                {
                    SetState(CreatureState.Combat);
                }
                else
                {
                    SetState(CreatureState.Idle);
                }
            }
        }

        /// <summary>
        /// Set the AI state.
        /// </summary>
        public void SetState(CreatureState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;
            _stateTime = 0f;

            OnStateExit(oldState);
            OnStateEnter(newState);

            DenizenEvents.RaiseStateChanged(_character, oldState, newState);
        }

        protected virtual void OnStateEnter(CreatureState state)
        {
            switch (state)
            {
                case CreatureState.Fleeing:
                    DenizenEvents.RaiseFleeStart(_character);
                    break;
            }
        }

        protected virtual void OnStateExit(CreatureState state)
        {
            switch (state)
            {
                case CreatureState.Combat:
                    _inCombat = false;
                    _combatTime = 0f;
                    break;
            }
        }

        /// <summary>
        /// Set the current target.
        /// </summary>
        public void SetTarget(Character target)
        {
            if (CurrentTarget == target) return;

            var oldTarget = CurrentTarget;
            CurrentTarget = target;

            DenizenEvents.RaiseTargetChanged(_character, oldTarget, target);
        }

        /// <summary>
        /// Enter combat with a target.
        /// </summary>
        protected void EnterCombat(Character target)
        {
            if (_inCombat) return;

            _inCombat = true;
            _combatTime = 0f;
            SetTarget(target);
            SetState(CreatureState.Combat);

            DenizenEvents.RaiseCombatStart(_character, target);

            // Call for help if configured
            if (_behavior.CallHelpRange > 0 && _behavior.GroupBehavior != GroupBehavior.Solo)
            {
                CallForHelp(target);
            }
        }

        /// <summary>
        /// Exit combat.
        /// </summary>
        protected void ExitCombat(string reason)
        {
            _inCombat = false;
            _combatTime = 0f;
            CurrentTarget = null;
            SetState(CreatureState.Idle);

            DenizenEvents.RaiseCombatEnd(_character, reason);
        }

        /// <summary>
        /// Find a threat to engage.
        /// </summary>
        protected virtual Character FindThreat()
        {
            if (_behavior.Aggression == Aggression.Passive)
                return null;

            // Use targeting priority to find best target
            return FindBestTarget();
        }

        /// <summary>
        /// Find best target based on priority.
        /// </summary>
        protected Character FindBestTarget()
        {
            Character bestTarget = null;
            float bestScore = float.MaxValue;

            try
            {
                var nearby = Character.GetAllCharacters();
                if (nearby == null) return null;

                foreach (var potential in nearby)
                {
                    // Null/destroyed check
                    if (potential == null) continue;
                    if (potential == _character) continue;

                    // Safe dead check
                    try { if (potential.IsDead()) continue; }
                    catch { continue; }

                    if (!IsEnemy(potential)) continue;

                    float distance = Vector3.Distance(transform.position, potential.transform.position);
                    if (distance > 50f) continue; // Max detection range

                    float score = CalculateTargetScore(potential, distance);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = potential;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"FindBestTarget error: {ex.Message}");
            }

            return bestTarget;
        }

        /// <summary>
        /// Calculate targeting score (lower is better).
        /// </summary>
        protected virtual float CalculateTargetScore(Character target, float distance)
        {
            return _behavior.Targeting switch
            {
                TargetPriority.Nearest => distance,
                TargetPriority.LowestHealth => target.GetHealth(),
                TargetPriority.HighestHealth => -target.GetHealth(),
                TargetPriority.Random => Random.value * 100f,
                _ => distance
            };
        }

        /// <summary>
        /// Check if a character is an enemy.
        /// </summary>
        protected virtual bool IsEnemy(Character other)
        {
            if (other is Player) return true;

            // Check faction (using base AI if available)
            if (_baseAI != null)
            {
                return BaseAI.IsEnemy(_character, other);
            }

            return false;
        }

        /// <summary>
        /// Call nearby allies for help.
        /// </summary>
        protected void CallForHelp(Character threat)
        {
            TriggerReaction(Reaction.CallForHelp, Trigger.OnDamaged);

            var nearby = Physics.OverlapSphere(transform.position, _behavior.CallHelpRange);
            foreach (var collider in nearby)
            {
                var ally = collider.GetComponent<DenizenAI>();
                if (ally != null && ally != this && ally._inCombat == false)
                {
                    ally.OnHelpCalled(threat, this);
                }
            }
        }

        /// <summary>
        /// Called when an ally calls for help.
        /// </summary>
        public virtual void OnHelpCalled(Character threat, DenizenAI caller)
        {
            if (_behavior.GroupBehavior == GroupBehavior.Solo) return;

            SetTarget(threat);
            EnterCombat(threat);
        }

        /// <summary>
        /// Try to use available abilities.
        /// </summary>
        protected virtual void TryUseAbilities()
        {
            if (_denizen?.Config?.Abilities == null) return;

            foreach (var ability in _denizen.Config.Abilities)
            {
                if (CanUseAbility(ability))
                {
                    UseAbility(ability);
                    break; // Only one ability per frame
                }
            }
        }

        /// <summary>
        /// Check if an ability can be used.
        /// </summary>
        protected bool CanUseAbility(AbilityConfig ability)
        {
            // Check cooldown
            if (_abilityCooldowns.TryGetValue(ability.Name, out float cooldownEnd))
            {
                if (Time.time < cooldownEnd) return false;
            }

            // Check target
            if (CurrentTarget == null) return false;

            float distance = Vector3.Distance(transform.position, CurrentTarget.transform.position);

            // Check range
            if (distance > ability.Range) return false;
            if (distance < ability.MinRange) return false;

            // Check condition
            return CheckAbilityCondition(ability.Condition);
        }

        /// <summary>
        /// Check ability condition.
        /// </summary>
        protected bool CheckAbilityCondition(AbilityCondition condition)
        {
            return condition switch
            {
                AbilityCondition.Always => true,
                AbilityCondition.LowHealth => _character.GetHealthPercentage() < 0.3f,
                AbilityCondition.HighHealth => _character.GetHealthPercentage() > 0.7f,
                AbilityCondition.TargetInRange => CurrentTarget != null,
                _ => true
            };
        }

        /// <summary>
        /// Use an ability.
        /// </summary>
        protected virtual void UseAbility(AbilityConfig ability)
        {
            // Set cooldown
            _abilityCooldowns[ability.Name] = Time.time + ability.Cooldown;

            // Play animation if configured
            if (!string.IsNullOrEmpty(ability.Animation))
            {
                var animator = _character.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(ability.Animation);
                }
            }

            // Execute Prime ability if configured
            if (!string.IsNullOrEmpty(ability.PrimeAbility))
            {
                PrimeIntegration.UseAbility(_character, ability.PrimeAbility, CurrentTarget);
            }

            // Play effect if configured
            if (!string.IsNullOrEmpty(ability.Effect))
            {
                SparkIntegration.PlaySound(ability.Effect, transform.position);
            }

            DenizenEvents.RaiseAbilityUsed(_character, ability.Name);
        }

        /// <summary>
        /// Check for reaction triggers.
        /// </summary>
        protected virtual void CheckReactionTriggers()
        {
            if (_behavior.Reactions == null) return;

            float healthPercent = _character.GetHealthPercentage();

            foreach (var reaction in _behavior.Reactions)
            {
                if (!CanTriggerReaction(reaction)) continue;

                bool shouldTrigger = reaction.Trigger switch
                {
                    Trigger.OnLowHealth => healthPercent < reaction.Threshold,
                    _ => false
                };

                if (shouldTrigger && Random.value <= reaction.Chance)
                {
                    TriggerReaction(reaction.Reaction, reaction.Trigger);
                    _reactionCooldowns[reaction.Reaction] = Time.time + reaction.Cooldown;
                }
            }
        }

        /// <summary>
        /// Check if a reaction can be triggered.
        /// </summary>
        protected bool CanTriggerReaction(ReactionConfig config)
        {
            if (_reactionCooldowns.TryGetValue(config.Reaction, out float cooldownEnd))
            {
                return Time.time >= cooldownEnd;
            }
            return true;
        }

        /// <summary>
        /// Trigger a reaction.
        /// </summary>
        protected virtual void TriggerReaction(Reaction reaction, Trigger trigger)
        {
            DenizenEvents.RaiseReactionTriggered(_character, reaction);

            switch (reaction)
            {
                case Reaction.Flee:
                    SetState(CreatureState.Fleeing);
                    break;
                case Reaction.Retreat:
                    SetState(CreatureState.Retreating);
                    break;
                case Reaction.Enrage:
                    // Apply enrage buff via Prime (+30% damage for 30 seconds)
                    PrimeIntegration.ApplyPercentBuff(_character, "Damage", 30f, 30f, "Enrage");
                    // Visual effect via Spark
                    SparkIntegration.AttachEnragedAura(_character);
                    break;
                case Reaction.CallForHelp:
                    if (CurrentTarget != null)
                    {
                        CallForHelp(CurrentTarget);
                    }
                    break;
            }
        }

        /// <summary>
        /// Update ability cooldowns.
        /// </summary>
        protected void UpdateAbilityCooldowns()
        {
            // Cleanup old cooldowns (optional optimization)
        }

        /// <summary>
        /// Update reaction cooldowns.
        /// </summary>
        protected void UpdateReactionCooldowns()
        {
            // Cleanup old cooldowns (optional optimization)
        }

        /// <summary>
        /// Called when this entity takes damage.
        /// </summary>
        public virtual void OnDamaged(HitData hit, Character attacker)
        {
            try
            {
                if (attacker != null && CurrentTarget == null)
                {
                    EnterCombat(attacker);
                }

                // Check for damage reactions
                if (_behavior?.Reactions != null)
                {
                    foreach (var reaction in _behavior.Reactions)
                    {
                        if (reaction.Trigger == Trigger.OnDamaged && CanTriggerReaction(reaction))
                        {
                            if (Random.value <= reaction.Chance)
                            {
                                TriggerReaction(reaction.Reaction, reaction.Trigger);
                                _reactionCooldowns[reaction.Reaction] = Time.time + reaction.Cooldown;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"OnDamaged error: {ex.Message}");
            }
        }
    }
}
