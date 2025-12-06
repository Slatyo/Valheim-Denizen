using Denizen.Entities;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// AI component for companion entities.
    /// Handles following, commands, and protection.
    /// </summary>
    public class CompanionAI : DenizenAI
    {
        private CompanionConfig _companionConfig;
        private FriendlyConfig _friendlyConfig;
        private Player _owner;
        private Vector3 _guardPosition;

        /// <summary>Current command.</summary>
        public CompanionCommand CurrentCommand { get; private set; } = CompanionCommand.Follow;

        protected override void Start()
        {
            base.Start();
            _companionConfig = _denizen?.GetCompanionConfig();
            _friendlyConfig = _companionConfig?.FriendlyBehavior ?? new FriendlyConfig();

            // Find owner from taming data
            var tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                // Get owner from tameable
                // Note: Actual implementation depends on how Valheim stores tame data
            }
        }

        protected override void Update()
        {
            if (_zNetView != null && !_zNetView.IsOwner())
                return;

            base.Update();

            // Update command behavior
            UpdateCommandBehavior();

            // Check for teleport
            CheckTeleport();
        }

        /// <summary>
        /// Update behavior based on current command.
        /// </summary>
        private void UpdateCommandBehavior()
        {
            switch (CurrentCommand)
            {
                case CompanionCommand.Follow:
                    UpdateFollow();
                    break;
                case CompanionCommand.Stay:
                    UpdateStay();
                    break;
                case CompanionCommand.Guard:
                    UpdateGuard();
                    break;
                case CompanionCommand.Passive:
                    // Do nothing - no combat
                    break;
            }
        }

        /// <summary>
        /// Update follow behavior.
        /// </summary>
        private void UpdateFollow()
        {
            if (_owner == null) return;

            float distance = Vector3.Distance(transform.position, _owner.transform.position);

            // Move towards owner if too far
            if (distance > _friendlyConfig.FollowDistance)
            {
                MoveTowards(_owner.transform.position);
            }
        }

        /// <summary>
        /// Update stay behavior.
        /// </summary>
        private void UpdateStay()
        {
            // Just stay put, but still protect if configured
            if (_friendlyConfig.ProtectOwner && _owner != null)
            {
                CheckForThreatsToOwner();
            }
        }

        /// <summary>
        /// Update guard behavior.
        /// </summary>
        private void UpdateGuard()
        {
            float distance = Vector3.Distance(transform.position, _guardPosition);

            // Return to guard position if strayed too far
            if (distance > 5f && CurrentState != CreatureState.Combat)
            {
                MoveTowards(_guardPosition);
            }
        }

        /// <summary>
        /// Check if owner needs protection.
        /// </summary>
        private void CheckForThreatsToOwner()
        {
            if (_owner == null || !_friendlyConfig.ProtectOwner) return;

            // Find threats attacking owner
            var nearby = Character.GetAllCharacters();
            foreach (var character in nearby)
            {
                if (character == _character || character == _owner) continue;
                if (character.IsDead()) continue;

                float distance = Vector3.Distance(_owner.transform.position, character.transform.position);
                if (distance > _friendlyConfig.ProtectRange) continue;

                // Check if this character is attacking owner
                var ai = character.GetComponent<BaseAI>();
                if (ai != null && ai.GetTargetCreature() == _owner)
                {
                    EnterCombat(character);
                    break;
                }
            }
        }

        /// <summary>
        /// Check if should teleport to owner.
        /// </summary>
        private void CheckTeleport()
        {
            if (_owner == null) return;
            if (CurrentCommand != CompanionCommand.Follow) return;

            float distance = Vector3.Distance(transform.position, _owner.transform.position);
            if (distance > _friendlyConfig.TeleportDistance)
            {
                TeleportToOwner();
            }
        }

        /// <summary>
        /// Teleport to owner's position.
        /// </summary>
        private void TeleportToOwner()
        {
            if (_owner == null) return;

            Vector3 targetPos = _owner.transform.position;
            targetPos += Random.insideUnitSphere * _friendlyConfig.FollowDistance;
            targetPos.y = _owner.transform.position.y;

            transform.position = targetPos;
            Plugin.Log.LogDebug($"Companion teleported to owner");
        }

        /// <summary>
        /// Move towards a position.
        /// </summary>
        private void MoveTowards(Vector3 position)
        {
            // Use base MonsterAI if available
            if (_baseAI != null)
            {
                _baseAI.MoveTo(Time.deltaTime, position, 0f, false);
            }
        }

        /// <summary>
        /// Execute a command.
        /// </summary>
        public void ExecuteCommand(CompanionCommand command, object param = null)
        {
            CurrentCommand = command;

            switch (command)
            {
                case CompanionCommand.Follow:
                    // Clear any combat state
                    if (CurrentState == CreatureState.Combat)
                    {
                        ExitCombat("command");
                    }
                    break;

                case CompanionCommand.Stay:
                    // Stay at current position
                    break;

                case CompanionCommand.Attack:
                    if (param is Character target)
                    {
                        SetTarget(target);
                        EnterCombat(target);
                    }
                    break;

                case CompanionCommand.Guard:
                    if (param is Vector3 position)
                    {
                        _guardPosition = position;
                    }
                    else
                    {
                        _guardPosition = transform.position;
                    }
                    break;

                case CompanionCommand.Passive:
                    // Exit combat and don't attack
                    if (CurrentState == CreatureState.Combat)
                    {
                        ExitCombat("command");
                    }
                    CurrentTarget = null;
                    break;
            }

            // Trigger event
            if (_owner != null)
            {
                DenizenEvents.RaiseCompanionCommand(_character, _owner, command);
            }
        }

        /// <summary>
        /// Set the owner of this companion.
        /// </summary>
        public void SetOwner(Player owner)
        {
            _owner = owner;
        }

        protected override Character FindThreat()
        {
            // Only find threats if not passive
            if (CurrentCommand == CompanionCommand.Passive)
                return null;

            // If protecting owner, prioritize threats to owner
            if (_friendlyConfig.ProtectOwner && _owner != null)
            {
                var threat = FindThreatToOwner();
                if (threat != null) return threat;
            }

            // Otherwise use normal threat detection based on combat style
            if (_friendlyConfig.CombatStyle == FriendlyCombat.Passive)
                return null;

            if (_friendlyConfig.CombatStyle == FriendlyCombat.Defensive)
            {
                // Only respond to attacks
                return null;
            }

            return base.FindThreat();
        }

        /// <summary>
        /// Find a threat to the owner.
        /// </summary>
        private Character FindThreatToOwner()
        {
            if (_owner == null) return null;

            var nearby = Character.GetAllCharacters();
            foreach (var character in nearby)
            {
                if (character == _character || character == _owner) continue;
                if (character.IsDead()) continue;

                float distance = Vector3.Distance(_owner.transform.position, character.transform.position);
                if (distance > _friendlyConfig.ProtectRange) continue;

                var ai = character.GetComponent<BaseAI>();
                if (ai != null && ai.GetTargetCreature() == _owner)
                {
                    return character;
                }
            }

            return null;
        }

        protected override bool IsEnemy(Character other)
        {
            // Players are not enemies for companions
            if (other is Player) return false;

            return base.IsEnemy(other);
        }

        protected override void UpdateCombat()
        {
            base.UpdateCombat();

            // Check if should return on low HP
            if (_friendlyConfig.ReturnOnLowHP)
            {
                float healthPercent = _character.GetHealthPercentage();
                if (healthPercent < _friendlyConfig.LowHPThreshold)
                {
                    ExitCombat("low_health");
                    ExecuteCommand(CompanionCommand.Follow);
                }
            }
        }
    }
}
