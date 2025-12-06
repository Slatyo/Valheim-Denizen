using System;
using UnityEngine;

namespace Denizen.Integration
{
    /// <summary>
    /// Integration wrapper for Spark VFX/Audio system.
    /// Safely calls Spark API only if the mod is loaded.
    /// </summary>
    public static class SparkIntegration
    {
        /// <summary>
        /// Attaches a boss aura to a creature.
        /// </summary>
        public static string AttachBossAura(Character creature)
        {
            if (!Plugin.HasSpark || creature == null)
                return null;

            try
            {
                return Spark.API.SparkAura.AttachBoss(creature);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to attach boss aura: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attaches an elite aura to a creature.
        /// </summary>
        public static string AttachEliteAura(Character creature)
        {
            if (!Plugin.HasSpark || creature == null)
                return null;

            try
            {
                return Spark.API.SparkAura.AttachElite(creature);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to attach elite aura: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attaches an enraged aura to a creature.
        /// </summary>
        public static string AttachEnragedAura(Character creature)
        {
            if (!Plugin.HasSpark || creature == null)
                return null;

            try
            {
                return Spark.API.SparkAura.AttachEnraged(creature);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to attach enraged aura: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attaches a shielded aura to a creature.
        /// </summary>
        public static string AttachShieldedAura(Character creature)
        {
            if (!Plugin.HasSpark || creature == null)
                return null;

            try
            {
                return Spark.API.SparkAura.AttachShielded(creature);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to attach shielded aura: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Removes an aura from a creature.
        /// </summary>
        public static void RemoveAura(Character creature, string auraId)
        {
            if (!Plugin.HasSpark || creature == null || string.IsNullOrEmpty(auraId))
                return;

            try
            {
                Spark.API.SparkAura.Remove(creature, auraId);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to remove aura: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes all auras from a creature.
        /// </summary>
        public static void RemoveAllAuras(Character creature)
        {
            if (!Plugin.HasSpark || creature == null)
                return;

            try
            {
                Spark.API.SparkAura.RemoveAll(creature);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to remove all auras: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a sound at a position.
        /// </summary>
        public static void PlaySound(string soundId, Vector3 position)
        {
            if (!Plugin.HasSpark || string.IsNullOrEmpty(soundId))
                return;

            try
            {
                Spark.API.SparkAudio.Play(soundId, position);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to play sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a looping sound attached to a game object.
        /// </summary>
        public static object PlayAttachedSound(string soundId, GameObject target, bool loop = false)
        {
            if (!Plugin.HasSpark || string.IsNullOrEmpty(soundId) || target == null)
                return null;

            try
            {
                return Spark.API.SparkAudio.PlayAttached(soundId, target, loop);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to play attached sound: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stops all sounds on a game object.
        /// </summary>
        public static void StopAllSounds(GameObject target)
        {
            if (!Plugin.HasSpark || target == null)
                return;

            try
            {
                Spark.API.SparkAudio.StopAll(target);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to stop sounds: {ex.Message}");
            }
        }
    }
}
