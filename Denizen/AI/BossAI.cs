using System.Collections.Generic;
using Denizen.Entities;
using Denizen.Integration;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// AI component for boss entities.
    /// Handles phases, special abilities, and boss mechanics.
    /// </summary>
    public class BossAI : DenizenAI
    {
        private BossConfig _bossConfig;
        private int _currentPhase;
        private BossPhase _activePhase;
        private List<string> _availableAbilities = new();

        /// <summary>Current phase index.</summary>
        public int CurrentPhase => _currentPhase;

        /// <summary>Active phase configuration.</summary>
        public BossPhase ActivePhase => _activePhase;

        protected override void Start()
        {
            base.Start();
            _bossConfig = _denizen?.GetBossConfig();

            if (_bossConfig?.Phases?.Count > 0)
            {
                SetPhase(0);
            }
        }

        protected override void UpdateCombat()
        {
            base.UpdateCombat();

            // Check for phase transitions
            CheckPhaseTransition();
        }

        /// <summary>
        /// Check if boss should transition to next phase.
        /// </summary>
        private void CheckPhaseTransition()
        {
            if (_bossConfig?.Phases == null || _bossConfig.Phases.Count == 0)
                return;

            float healthPercent = _character.GetHealthPercentage();

            // Check each phase in reverse order (highest threshold first)
            for (int i = _bossConfig.Phases.Count - 1; i >= 0; i--)
            {
                var phase = _bossConfig.Phases[i];
                if (healthPercent <= phase.HealthThreshold && i > _currentPhase)
                {
                    SetPhase(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Set the current phase.
        /// </summary>
        private void SetPhase(int phaseIndex)
        {
            if (phaseIndex < 0 || phaseIndex >= _bossConfig.Phases.Count)
                return;

            int oldPhase = _currentPhase;
            _currentPhase = phaseIndex;
            _activePhase = _bossConfig.Phases[phaseIndex];

            // Update available abilities
            _availableAbilities.Clear();
            if (_activePhase.Abilities != null)
            {
                _availableAbilities.AddRange(_activePhase.Abilities);
            }

            // Update behavior if phase has override
            if (_activePhase.Behavior != null)
            {
                _behavior = _activePhase.Behavior;
            }

            // Play phase transition effect
            if (!string.IsNullOrEmpty(_activePhase.OnEnterEffect))
            {
                SparkIntegration.PlaySound(_activePhase.OnEnterEffect, transform.position);
            }

            // Apply phase-specific aura
            if (_activePhase.Behavior?.Aggression == Aggression.Aggressive)
            {
                SparkIntegration.AttachEnragedAura(_character);
            }

            // Show phase transition message
            if (!string.IsNullOrEmpty(_activePhase.OnEnterMessage))
            {
                ShowBossMessage(_activePhase.OnEnterMessage);
            }

            Plugin.Log.LogInfo($"Boss {_bossConfig.Id} entered phase {_currentPhase}: {_activePhase.Name}");

            // Trigger event
            DenizenEvents.RaiseBossPhaseChange(_character, _currentPhase);
        }

        /// <summary>
        /// Show a boss message to all players.
        /// </summary>
        private void ShowBossMessage(string messageKey)
        {
            string message = Localization.instance.Localize(messageKey);
            // TODO: Use Veneer for boss messages
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);
        }

        protected override void TryUseAbilities()
        {
            if (_denizen?.Config?.Abilities == null) return;

            foreach (var ability in _denizen.Config.Abilities)
            {
                // Only use abilities available in current phase
                if (_availableAbilities.Count > 0 && !_availableAbilities.Contains(ability.Name))
                    continue;

                if (CanUseAbility(ability))
                {
                    UseAbility(ability);
                    break;
                }
            }
        }

        protected override float CalculateTargetScore(Character target, float distance)
        {
            // Bosses may have different targeting logic per phase
            if (_activePhase?.Behavior?.Targeting != null)
            {
                return _activePhase.Behavior.Targeting switch
                {
                    TargetPriority.Random => Random.value * 100f,
                    TargetPriority.LowestHealth => target.GetHealth(),
                    TargetPriority.HighestThreat => -GetThreat(target),
                    _ => distance
                };
            }

            return base.CalculateTargetScore(target, distance);
        }

        /// <summary>
        /// Get threat level for a target (placeholder).
        /// </summary>
        private float GetThreat(Character target)
        {
            // TODO: Implement threat tracking
            // For now, use distance as inverse threat
            return 1f / Vector3.Distance(transform.position, target.transform.position);
        }

        /// <summary>
        /// Called when boss dies.
        /// </summary>
        public void OnDeath(Character killer)
        {
            // Find the player who killed the boss
            Player killerPlayer = killer as Player;
            if (killerPlayer == null && killer != null)
            {
                // Try to find owner if it was a companion/summon
                var companion = killer.GetComponent<CompanionAI>();
                // killerPlayer = companion?._owner; // Would need public accessor
            }

            // If no specific killer found, credit nearest player
            if (killerPlayer == null)
            {
                killerPlayer = Player.GetClosestPlayer(transform.position, 100f);
            }

            // Trigger event
            DenizenEvents.RaiseBossDefeated(_character, killerPlayer);

            // Stop boss music and effects
            if (!string.IsNullOrEmpty(_bossConfig?.Music))
            {
                SparkIntegration.StopAllSounds(gameObject);
            }

            // Remove all auras
            SparkIntegration.RemoveAllAuras(_character);

            // Hide from Veneer UI
            VeneerIntegration.HideBoss(_character);

            Plugin.Log.LogInfo($"Boss {_bossConfig?.Id} defeated by {killerPlayer?.GetPlayerName() ?? "unknown"}");
        }
    }
}
