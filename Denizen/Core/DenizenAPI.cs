using System.Collections.Generic;
using Denizen.Entities;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// Public API for Denizen mod.
    /// </summary>
    public static class DenizenAPI
    {
        /// <summary>
        /// Register an NPC.
        /// </summary>
        public static void RegisterNPC(string id, NPCConfig config)
        {
            DenizenRegistry.Instance.RegisterNPC(id, config);
        }

        /// <summary>
        /// Register an enemy.
        /// </summary>
        public static void RegisterEnemy(string id, EnemyConfig config)
        {
            DenizenRegistry.Instance.RegisterEnemy(id, config);
        }

        /// <summary>
        /// Register a boss.
        /// </summary>
        public static void RegisterBoss(string id, BossConfig config)
        {
            DenizenRegistry.Instance.RegisterBoss(id, config);
        }

        /// <summary>
        /// Register a companion.
        /// </summary>
        public static void RegisterCompanion(string id, CompanionConfig config)
        {
            DenizenRegistry.Instance.RegisterCompanion(id, config);
        }

        /// <summary>
        /// Get a denizen config by ID.
        /// </summary>
        public static DenizenConfig GetConfig(string id)
        {
            return DenizenRegistry.Instance.Get(id);
        }

        /// <summary>
        /// Check if a denizen is registered.
        /// </summary>
        public static bool IsDenizen(string id)
        {
            return DenizenRegistry.Instance.Has(id);
        }

        /// <summary>
        /// Get the denizen type for a character.
        /// </summary>
        public static DenizenType? GetType(Character character)
        {
            if (character == null) return null;

            var component = character.GetComponent<DenizenComponent>();
            if (component != null)
            {
                return component.Config?.Type;
            }

            return null;
        }

        /// <summary>
        /// Get the AI state for a character.
        /// </summary>
        public static CreatureState GetState(Character character)
        {
            if (character == null) return CreatureState.Idle;

            var ai = character.GetComponent<DenizenAI>();
            return ai != null ? ai.CurrentState : CreatureState.Idle;
        }

        /// <summary>
        /// Set the AI state for a character.
        /// </summary>
        public static void SetState(Character character, CreatureState state)
        {
            if (character == null) return;

            var ai = character.GetComponent<DenizenAI>();
            if (ai != null)
            {
                ai.SetState(state);
            }
        }

        /// <summary>
        /// Get the current target for a character.
        /// </summary>
        public static Character GetTarget(Character character)
        {
            if (character == null) return null;

            var ai = character.GetComponent<DenizenAI>();
            return ai?.CurrentTarget;
        }

        /// <summary>
        /// Set the target for a character.
        /// </summary>
        public static void SetTarget(Character character, Character target)
        {
            if (character == null) return;

            var ai = character.GetComponent<DenizenAI>();
            ai?.SetTarget(target);
        }

        /// <summary>
        /// Clear the target for a character.
        /// </summary>
        public static void ClearTarget(Character character)
        {
            SetTarget(character, null);
        }

        /// <summary>
        /// Issue a command to a companion.
        /// </summary>
        public static void Command(Character companion, CompanionCommand command, object param = null)
        {
            if (companion == null) return;

            var ai = companion.GetComponent<CompanionAI>();
            ai?.ExecuteCommand(command, param);
        }

        /// <summary>
        /// Check if a companion is following its owner.
        /// </summary>
        public static bool IsFollowing(Character companion)
        {
            if (companion == null) return false;

            var ai = companion.GetComponent<CompanionAI>();
            return ai != null && ai.CurrentCommand == CompanionCommand.Follow;
        }

        /// <summary>
        /// Check if a companion is guarding a position.
        /// </summary>
        public static bool IsGuarding(Character companion)
        {
            if (companion == null) return false;

            var ai = companion.GetComponent<CompanionAI>();
            return ai != null && ai.CurrentCommand == CompanionCommand.Guard;
        }

        /// <summary>
        /// Spawn a denizen at a position.
        /// </summary>
        public static Character Spawn(string denizenId, Vector3 position, Quaternion rotation = default)
        {
            return DenizenSpawner.Spawn(denizenId, position, rotation);
        }

        /// <summary>
        /// Spawn a denizen with a specific level.
        /// </summary>
        public static Character Spawn(string denizenId, Vector3 position, int level)
        {
            return DenizenSpawner.Spawn(denizenId, position, Quaternion.identity, level);
        }

        /// <summary>
        /// Spawn a group of denizens.
        /// </summary>
        public static List<Character> SpawnGroup(string denizenId, Vector3 position, int count)
        {
            return DenizenSpawner.SpawnGroup(denizenId, position, count);
        }

        /// <summary>
        /// Spawn a boss with arena setup.
        /// </summary>
        public static Character SpawnBoss(string denizenId, Vector3 position)
        {
            return DenizenSpawner.SpawnBoss(denizenId, position);
        }
    }
}
