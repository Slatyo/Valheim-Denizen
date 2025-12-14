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

        /// <summary>Whether we're controlling movement (vs letting vanilla handle it).</summary>
        protected bool _controlMovement;

        /// <summary>Movement target position when we're controlling movement.</summary>
        protected Vector3 _moveTarget;

        /// <summary>Time until we release movement control back to vanilla.</summary>
        protected float _movementOverrideTime;

        protected virtual void Start()
        {
            if (_denizen?.Config?.Behavior != null)
            {
                _behavior = _denizen.Config.Behavior;

                // Smart AI types take control of movement
                _controlMovement = _behavior.KeepDistance ||
                                   _behavior.PreferredRange == CombatRange.Ranged ||
                                   _behavior.Aggression == Aggression.Tactical;
            }
            else
            {
                _behavior = new BehaviorConfig();
            }
        }

        protected virtual void Update()
        {
            try
            {
                // Safety: check if we're valid using Unity's implicit bool operator
                if (!this) return;
                if (!_character) return;
                if (_zNetView != null && !_zNetView.IsOwner()) return;

                _stateTime += Time.deltaTime;

                // Tick down movement override timer
                if (_movementOverrideTime > 0)
                {
                    _movementOverrideTime -= Time.deltaTime;
                }

                // Only run AI logic if behavior is configured
                if (_behavior == null) return;

                // Sync target from vanilla AI if available
                try
                {
                    if (_baseAI != null)
                    {
                        var vanillaTarget = _baseAI.GetTargetCreature();
                        if (vanillaTarget != null && vanillaTarget && !vanillaTarget.IsDead())
                        {
                            // Vanilla AI found a target - enter combat if not already
                            if (CurrentTarget != vanillaTarget)
                            {
                                CurrentTarget = vanillaTarget;
                                if (!_inCombat)
                                {
                                    _inCombat = true;
                                    CurrentState = CreatureState.Combat;
                                    _combatTime = 0f;
                                    DenizenEvents.RaiseCombatStart(_character, vanillaTarget);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log.LogWarning($"[DenizenAI] Target sync error: {ex.Message}");
                }

                // Run state-specific logic
                UpdateState();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[DenizenAI] Update error: {ex.Message}");
            }
        }

        /// <summary>
        /// LateUpdate runs AFTER MonsterAI.Update(), allowing us to override movement.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!_inCombat) return;
            if (!_character || !CurrentTarget) return;

            try
            {
                // If we have an active movement override (dodge, retreat), apply it
                if (_movementOverrideTime > 0)
                {
                    ApplyMovement(_moveTarget);
                    return;
                }

                // Check if player is attacking - this is key for reactive combat
                CheckForIncomingAttack();

                // Only control movement for smart AI types
                if (!_controlMovement)
                {
                    FaceTarget();
                    return;
                }

                float distance = GetDistanceToTarget();
                float idealDist = _behavior.IdealDistance > 0 ? _behavior.IdealDistance : 12f;
                float tooClose = 5f; // Melee danger zone

                // Only reposition if in danger zone - otherwise STAND AND FIGHT
                if (distance < tooClose)
                {
                    // Back up just enough to get out of melee range
                    Vector3 awayDir = (transform.position - CurrentTarget.transform.position).normalized;
                    Vector3 retreatPos = transform.position + awayDir * 3f;
                    ApplyMovement(retreatPos);
                }
                else
                {
                    // At good range - STOP MOVING and let vanilla AI handle attacks
                    _character.SetMoveDir(Vector3.zero);
                }

                // Always face target
                FaceTarget();
            }
            catch { }
        }

        /// <summary>
        /// Detect if the player is mid-attack and react.
        /// </summary>
        protected virtual void CheckForIncomingAttack()
        {
            if (!CurrentTarget) return;

            var player = CurrentTarget as Player;
            if (player == null) return;

            // Check if player is in attack animation
            bool playerAttacking = IsPlayerAttacking(player);

            if (playerAttacking && !_reactedToCurrentAttack)
            {
                float distance = GetDistanceToTarget();

                // Only react if player is close enough to hit us
                if (distance < 4f)
                {
                    _reactedToCurrentAttack = true;
                    TryReactToAttack();
                }
            }
            else if (!playerAttacking)
            {
                _reactedToCurrentAttack = false;
            }
        }

        /// <summary>Track if we already reacted to current attack.</summary>
        protected bool _reactedToCurrentAttack;

        /// <summary>
        /// Check if the player is currently attacking.
        /// </summary>
        protected bool IsPlayerAttacking(Player player)
        {
            if (player == null) return false;

            try
            {
                // Check animator for attack states
                var animator = player.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    // Common attack animation names/tags
                    if (stateInfo.IsTag("attack") ||
                        stateInfo.IsName("Attack") ||
                        stateInfo.IsName("swing") ||
                        stateInfo.normalizedTime < 0.5f && stateInfo.IsTag("weapon"))
                    {
                        return true;
                    }
                }

                // Fallback: check if player is in attack mode via their current action
                // Player.m_attack is true during attack windup
                return player.InAttack();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// React to an incoming attack - dodge, parry, or block.
        /// </summary>
        protected virtual void TryReactToAttack()
        {
            if (_behavior?.Reactions == null) return;

            // Find applicable reactions for incoming attacks
            foreach (var reaction in _behavior.Reactions)
            {
                if (reaction.Trigger != Trigger.OnIncomingAttack) continue;
                if (!CanTriggerReaction(reaction)) continue;
                if (Random.value > reaction.Chance) continue;

                // Execute the reaction
                TriggerReaction(reaction.Reaction, Trigger.OnIncomingAttack);
                _reactionCooldowns[reaction.Reaction] = Time.time + reaction.Cooldown;

                // Only one reaction per attack
                break;
            }
        }

        /// <summary>
        /// Apply movement by setting the character's move direction.
        /// </summary>
        protected void ApplyMovement(Vector3 targetPosition)
        {
            if (!_character) return;

            Vector3 direction = (targetPosition - transform.position);
            direction.y = 0; // Keep movement horizontal

            if (direction.magnitude > 0.5f)
            {
                direction = direction.normalized;
                _character.SetMoveDir(direction);
            }
            else
            {
                _character.SetMoveDir(Vector3.zero);
            }
        }

        /// <summary>
        /// Face the current target.
        /// </summary>
        protected void FaceTarget()
        {
            if (!CurrentTarget) return;

            Vector3 lookPos = CurrentTarget.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        /// <summary>
        /// Override movement for a duration (used by reactions like Dodge, Retreat).
        /// </summary>
        protected void OverrideMovement(Vector3 target, float duration)
        {
            _moveTarget = target;
            _movementOverrideTime = duration;
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
            // Use Unity's implicit bool operator for null checks
            if (CurrentTarget)
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
            try
            {
                _combatTime += Time.deltaTime;

                // Check if target is still valid - use Unity's implicit bool operator
                if (!CurrentTarget || CurrentTarget.IsDead())
                {
                    CurrentTarget = FindThreat();
                    if (!CurrentTarget)
                    {
                        ExitCombat("target_lost");
                        return;
                    }
                }

                float distanceToTarget = GetDistanceToTarget();

                // Check flee threshold
                float healthPercent = _character.GetHealthPercentage();
                if (healthPercent < _behavior.FleeThreshold && _behavior.FleeThreshold > 0)
                {
                    TriggerReaction(Reaction.Flee, Trigger.OnLowHealth);
                    return;
                }

                // Try to use abilities
                TryUseAbilities();

                // Check for reaction triggers
                CheckReactionTriggers();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[DenizenAI] UpdateCombat error: {ex.Message}\n{ex.StackTrace}");
            }
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
                // Re-engage if target still visible - use Unity's implicit bool operator
                if (CurrentTarget && !CurrentTarget.IsDead())
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

                // Cache our position once to avoid repeated transform access
                Vector3 myPosition = transform.position;

                foreach (var potential in nearby)
                {
                    // Comprehensive null/destroyed check using Unity's implicit bool
                    if (!potential) continue;
                    if (potential == _character) continue;

                    // Safe transform access check
                    Transform potentialTransform;
                    try
                    {
                        potentialTransform = potential.transform;
                        if (potentialTransform == null) continue;
                    }
                    catch
                    {
                        continue;
                    }

                    // Safe dead check
                    try
                    {
                        if (potential.IsDead()) continue;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!IsEnemy(potential)) continue;

                    float distance = Vector3.Distance(myPosition, potentialTransform.position);
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
            try
            {
                return _behavior.Targeting switch
                {
                    TargetPriority.Nearest => distance,
                    TargetPriority.LowestHealth => target.GetHealth(),
                    TargetPriority.HighestHealth => -target.GetHealth(),
                    TargetPriority.LowestArmor => GetTargetArmor(target),
                    TargetPriority.HighestThreat => -GetThreatLevel(target),
                    TargetPriority.Random => Random.value * 100f,
                    TargetPriority.LastAttacker => target == _lastAttacker ? 0f : distance,
                    TargetPriority.Healer => IsHealer(target) ? 0f : distance,
                    TargetPriority.Ranged => IsRanged(target) ? 0f : distance,
                    _ => distance
                };
            }
            catch
            {
                // Fallback to distance on any error
                return distance;
            }
        }

        /// <summary>
        /// Gets the armor value of a target. Uses Prime if available.
        /// </summary>
        protected float GetTargetArmor(Character target)
        {
            if (Plugin.HasPrime)
            {
                try
                {
                    return PrimeIntegration.GetStat(target, "Armor");
                }
                catch
                {
                    // Fall through to default
                }
            }
            // Default fallback - use health as rough proxy
            return target.GetHealth() * 0.1f;
        }

        /// <summary>
        /// Gets the threat level of a target (based on recent damage dealt to us).
        /// </summary>
        protected virtual float GetThreatLevel(Character target)
        {
            // For now, prioritize last attacker and nearby targets
            if (target == _lastAttacker) return 100f;

            try
            {
                return 1f / Mathf.Max(1f, Vector3.Distance(transform.position, target.transform.position));
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// Checks if target appears to be a healer/support.
        /// </summary>
        protected bool IsHealer(Character target)
        {
            // Could check for staff weapons, specific items, etc.
            // For now, just check if they're a player with low health (support players often have less)
            if (target is Player player)
            {
                return player.GetMaxHealth() < 100f;
            }
            return false;
        }

        /// <summary>
        /// Checks if target appears to be a ranged attacker.
        /// </summary>
        protected bool IsRanged(Character target)
        {
            // Check if they have a ranged weapon equipped
            if (target is Player player)
            {
                var rightItem = player.GetRightItem();
                if (rightItem != null)
                {
                    return rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
                }
            }
            return false;
        }

        /// <summary>
        /// Last character that attacked us.
        /// </summary>
        protected Character _lastAttacker;

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
            // Note: Don't call TriggerReaction here - it would cause infinite recursion!
            // Just raise the event and alert nearby allies
            DenizenEvents.RaiseReactionTriggered(_character, Reaction.CallForHelp);

            try
            {
                var nearby = Physics.OverlapSphere(transform.position, _behavior.CallHelpRange);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this && !ally._inCombat)
                    {
                        ally.OnHelpCalled(threat, this);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"CallForHelp error: {ex.Message}");
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
            if (ability == null) return false;

            // Check cooldown
            if (_abilityCooldowns.TryGetValue(ability.Name, out float cooldownEnd))
            {
                if (Time.time < cooldownEnd) return false;
            }

            // Check target (with null safety)
            if (!CurrentTarget) return false;

            // Safe transform access
            Transform targetTransform;
            try
            {
                targetTransform = CurrentTarget.transform;
                if (targetTransform == null) return false;
            }
            catch
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, targetTransform.position);

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
            try
            {
                // Use Unity's implicit bool for CurrentTarget checks
                bool hasTarget = CurrentTarget;

                return condition switch
                {
                    AbilityCondition.Always => true,
                    AbilityCondition.LowHealth => _character.GetHealthPercentage() < 0.3f,
                    AbilityCondition.HighHealth => _character.GetHealthPercentage() > 0.7f,
                    AbilityCondition.TargetInRange => hasTarget,
                    AbilityCondition.TargetOutOfMelee => hasTarget && GetDistanceToTarget() > 5f,
                    AbilityCondition.MultipleTargets => CountNearbyEnemies() > 1,
                    AbilityCondition.AllyNearby => HasNearbyAlly(),
                    AbilityCondition.AllyLowHealth => HasLowHealthAlly(),
                    AbilityCondition.NoAlliesNearby => !HasNearbyAlly(),
                    _ => true
                };
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely gets distance to current target.
        /// </summary>
        protected float GetDistanceToTarget()
        {
            if (!CurrentTarget) return float.MaxValue;

            try
            {
                return Vector3.Distance(transform.position, CurrentTarget.transform.position);
            }
            catch
            {
                return float.MaxValue;
            }
        }

        /// <summary>
        /// Counts the number of enemies within range.
        /// </summary>
        protected int CountNearbyEnemies()
        {
            int count = 0;
            try
            {
                var nearby = Character.GetAllCharacters();
                if (nearby == null) return 0;

                foreach (var character in nearby)
                {
                    if (!character) continue;
                    if (character == _character) continue;
                    if (!IsEnemy(character)) continue;

                    float dist = Vector3.Distance(transform.position, character.transform.position);
                    if (dist < 15f) count++;
                }
            }
            catch { }
            return count;
        }

        /// <summary>
        /// Checks if there's a nearby ally (same faction).
        /// </summary>
        protected bool HasNearbyAlly()
        {
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 15f);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this)
                        return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Checks if there's a nearby ally with low health.
        /// </summary>
        protected bool HasLowHealthAlly()
        {
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 15f);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this && ally._character != null)
                    {
                        if (ally._character.GetHealthPercentage() < 0.3f)
                            return true;
                    }
                }
            }
            catch { }
            return false;
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
            if (_behavior?.Reactions == null) return;

            float healthPercent = _character.GetHealthPercentage();

            // Use Unity's implicit bool for null check, and safely get distance
            float distanceToTarget = float.MaxValue;
            if (CurrentTarget)
            {
                try
                {
                    distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
                }
                catch
                {
                    // Target was destroyed between check and access
                    distanceToTarget = float.MaxValue;
                }
            }

            foreach (var reaction in _behavior.Reactions)
            {
                if (!CanTriggerReaction(reaction)) continue;

                // Use Unity's implicit bool for CurrentTarget checks
                bool hasTarget = CurrentTarget;

                bool shouldTrigger = reaction.Trigger switch
                {
                    Trigger.OnLowHealth => healthPercent < reaction.Threshold,
                    Trigger.OnTargetTooClose => hasTarget && distanceToTarget < reaction.Distance,
                    Trigger.OnTargetTooFar => hasTarget && distanceToTarget > reaction.Distance,
                    Trigger.OnTimer => ShouldTriggerTimer(reaction),
                    Trigger.OnCombatStart => _combatTime < 1f && _inCombat, // First second of combat
                    Trigger.OnAllyLowHealth => CheckAllyLowHealth(reaction.Threshold),
                    Trigger.OnAllyDeath => _recentAllyDeath,
                    Trigger.OnAllyDamaged => _recentAllyDamaged,
                    // OnDamaged and OnIncomingAttack are handled in OnDamaged() method
                    _ => false
                };

                if (shouldTrigger && Random.value <= reaction.Chance)
                {
                    TriggerReaction(reaction.Reaction, reaction.Trigger);
                    _reactionCooldowns[reaction.Reaction] = Time.time + reaction.Cooldown;
                }
            }

            // Reset frame-based flags
            _recentAllyDeath = false;
            _recentAllyDamaged = false;
        }

        /// <summary>
        /// Timer tracking for OnTimer triggers.
        /// </summary>
        private Dictionary<string, float> _timerTriggers = new();

        /// <summary>
        /// Checks if a timer-based trigger should fire.
        /// </summary>
        private bool ShouldTriggerTimer(ReactionConfig reaction)
        {
            string key = $"{reaction.Reaction}_{reaction.Trigger}";
            if (!_timerTriggers.TryGetValue(key, out float nextTime))
            {
                _timerTriggers[key] = Time.time + reaction.Cooldown;
                return false;
            }

            if (Time.time >= nextTime)
            {
                _timerTriggers[key] = Time.time + reaction.Cooldown;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Flag for recent ally death (set by ally death event).
        /// </summary>
        protected bool _recentAllyDeath;

        /// <summary>
        /// Flag for recent ally damage (set by ally damage event).
        /// </summary>
        protected bool _recentAllyDamaged;

        /// <summary>
        /// Checks if any nearby ally is below health threshold.
        /// </summary>
        private bool CheckAllyLowHealth(float threshold)
        {
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 20f);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this && ally._character != null)
                    {
                        if (ally._character.GetHealthPercentage() < threshold)
                            return true;
                    }
                }
            }
            catch { }
            return false;
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
                    // Move away from target for a duration
                    if (CurrentTarget)
                    {
                        Vector3 awayDir = (transform.position - CurrentTarget.transform.position).normalized;
                        Vector3 retreatPos = transform.position + awayDir * 10f;
                        OverrideMovement(retreatPos, 2f); // Retreat for 2 seconds
                    }
                    break;

                case Reaction.Dodge:
                    // Quick sidestep - pick a random perpendicular direction
                    if (CurrentTarget)
                    {
                        Vector3 toTarget = (CurrentTarget.transform.position - transform.position).normalized;
                        Vector3 sideDir = Vector3.Cross(toTarget, Vector3.up);
                        if (Random.value > 0.5f) sideDir = -sideDir;
                        Vector3 dodgePos = transform.position + sideDir * 4f;
                        OverrideMovement(dodgePos, 0.5f); // Quick dodge
                        TriggerAnimation("dodge");
                    }
                    break;

                case Reaction.Block:
                    // Trigger block animation if available
                    TriggerAnimation("block");
                    // Apply temporary damage reduction via Prime
                    PrimeIntegration.ApplyPercentBuff(_character, "DamageReduction", 50f, 2f, "Block");
                    break;

                case Reaction.Parry:
                    // Trigger parry animation
                    TriggerAnimation("parry");
                    _isParrying = true;
                    break;

                case Reaction.Enrage:
                    // Apply enrage buff via Prime (+30% damage for 30 seconds)
                    PrimeIntegration.ApplyPercentBuff(_character, "Damage", 30f, 30f, "Enrage");
                    // Visual effect via Spark
                    SparkIntegration.AttachEnragedAura(_character);
                    break;

                case Reaction.Charge:
                    // Rush toward target
                    if (CurrentTarget)
                    {
                        TriggerAnimation("charge");
                        OverrideMovement(CurrentTarget.transform.position, 2f);
                        // Apply temporary speed buff
                        PrimeIntegration.ApplyPercentBuff(_character, "MoveSpeed", 50f, 3f, "Charge");
                    }
                    break;

                case Reaction.CallForHelp:
                    // CallForHelp is handled directly in EnterCombat/OnDamaged
                    // This case is only for when triggered via CheckReactionTriggers
                    // Alert nearby allies without recursion
                    if (CurrentTarget)
                    {
                        try
                        {
                            var nearby = Physics.OverlapSphere(transform.position, _behavior.CallHelpRange);
                            foreach (var collider in nearby)
                            {
                                if (!collider) continue;
                                var ally = collider.GetComponent<DenizenAI>();
                                if (ally != null && ally != this && !ally._inCombat)
                                {
                                    ally.OnHelpCalled(CurrentTarget, this);
                                }
                            }
                        }
                        catch { }
                    }
                    break;

                case Reaction.Protect:
                    // Move to protect a nearby ally
                    ProtectNearbyAlly();
                    break;

                case Reaction.Avenge:
                    // Enrage on ally death
                    PrimeIntegration.ApplyPercentBuff(_character, "Damage", 50f, 60f, "Avenge");
                    PrimeIntegration.ApplyPercentBuff(_character, "AttackSpeed", 25f, 60f, "Avenge");
                    SparkIntegration.AttachEnragedAura(_character);
                    break;

                case Reaction.Heal:
                    // Use healing ability if we have one
                    TryUseHealingAbility();
                    break;

                case Reaction.Reposition:
                    // Move to ideal combat distance
                    RepositionToIdealRange();
                    break;

                case Reaction.Ambush:
                    // Wait silently for target to come closer
                    SetState(CreatureState.Alert);
                    break;
            }
        }

        /// <summary>
        /// Flag for parry state.
        /// </summary>
        protected bool _isParrying;

        /// <summary>
        /// Triggers an animation if the animator has it.
        /// </summary>
        protected void TriggerAnimation(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return;

            try
            {
                var animator = _character.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(animName);
                }
            }
            catch { }
        }

        /// <summary>
        /// Moves to protect a nearby ally that's being attacked.
        /// </summary>
        protected void ProtectNearbyAlly()
        {
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 20f);
                DenizenAI weakestAlly = null;
                float lowestHealth = float.MaxValue;

                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this && ally._character != null && ally._inCombat)
                    {
                        float health = ally._character.GetHealthPercentage();
                        if (health < lowestHealth)
                        {
                            lowestHealth = health;
                            weakestAlly = ally;
                        }
                    }
                }

                if (weakestAlly != null && weakestAlly.CurrentTarget != null)
                {
                    // Intercept the attacker
                    SetTarget(weakestAlly.CurrentTarget);
                    OverrideMovement(weakestAlly.CurrentTarget.transform.position, 2f);
                }
            }
            catch { }
        }

        /// <summary>
        /// Attempts to use a healing ability if one is configured.
        /// </summary>
        protected void TryUseHealingAbility()
        {
            if (_denizen?.Config?.Abilities == null) return;

            foreach (var ability in _denizen.Config.Abilities)
            {
                // Look for abilities with "heal" in the name or Prime ability ID
                if (ability.Name.ToLower().Contains("heal") ||
                    ability.PrimeAbility?.ToLower().Contains("heal") == true)
                {
                    if (CanUseAbility(ability))
                    {
                        UseAbility(ability);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Moves to maintain ideal combat distance.
        /// </summary>
        protected void RepositionToIdealRange()
        {
            if (!CurrentTarget) return;

            float currentDist = GetDistanceToTarget();
            float idealDist = _behavior.IdealDistance > 0 ? _behavior.IdealDistance : 12f;

            Vector3 direction = (transform.position - CurrentTarget.transform.position).normalized;
            Vector3 targetPos = CurrentTarget.transform.position + direction * idealDist;

            OverrideMovement(targetPos, 1.5f);
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
                // Track last attacker for targeting priority
                if (attacker != null)
                {
                    _lastAttacker = attacker;
                }

                if (attacker != null && CurrentTarget == null)
                {
                    EnterCombat(attacker);
                }

                // Check for damage reactions (OnDamaged and OnIncomingAttack)
                if (_behavior?.Reactions != null)
                {
                    foreach (var reaction in _behavior.Reactions)
                    {
                        bool shouldCheck = reaction.Trigger == Trigger.OnDamaged ||
                                           reaction.Trigger == Trigger.OnIncomingAttack;

                        if (shouldCheck && CanTriggerReaction(reaction))
                        {
                            if (Random.value <= reaction.Chance)
                            {
                                TriggerReaction(reaction.Reaction, reaction.Trigger);
                                _reactionCooldowns[reaction.Reaction] = Time.time + reaction.Cooldown;
                            }
                        }
                    }
                }

                // Notify nearby allies of damage for OnAllyDamaged trigger
                NotifyAlliesOfDamage();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"OnDamaged error: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies nearby allies that we took damage.
        /// </summary>
        protected void NotifyAlliesOfDamage()
        {
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 20f);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this)
                    {
                        ally._recentAllyDamaged = true;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Called when this entity dies.
        /// </summary>
        protected virtual void OnDeath()
        {
            // Notify nearby allies of death for OnAllyDeath trigger
            try
            {
                var nearby = Physics.OverlapSphere(transform.position, 30f);
                foreach (var collider in nearby)
                {
                    if (!collider) continue;
                    var ally = collider.GetComponent<DenizenAI>();
                    if (ally != null && ally != this)
                    {
                        ally._recentAllyDeath = true;
                    }
                }
            }
            catch { }
        }
    }
}
