using Denizen.Core;
using HarmonyLib;
using UnityEngine;

namespace Denizen.Patches
{
    /// <summary>
    /// Harmony patches for Character class.
    /// </summary>
    [HarmonyPatch(typeof(Character))]
    internal static class CharacterPatches
    {
        /// <summary>
        /// Patch to intercept damage for Denizen AI reactions.
        /// </summary>
        [HarmonyPatch(nameof(Character.Damage))]
        [HarmonyPostfix]
        private static void Damage_Postfix(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null) return;

            var denizenAI = __instance.GetComponent<DenizenAI>();
            if (denizenAI != null)
            {
                denizenAI.OnDamaged(hit, hit.GetAttacker());
            }
        }

        /// <summary>
        /// Patch to handle Denizen death.
        /// </summary>
        [HarmonyPatch(nameof(Character.OnDeath))]
        [HarmonyPostfix]
        private static void OnDeath_Postfix(Character __instance)
        {
            if (__instance == null) return;

            var bossAI = __instance.GetComponent<BossAI>();
            if (bossAI != null)
            {
                bossAI.OnDeath(__instance);
            }
        }
    }

    /// <summary>
    /// Harmony patches for Character class (NPC hover text).
    /// </summary>
    [HarmonyPatch(typeof(Character))]
    internal static class CharacterHoverPatches
    {
        /// <summary>
        /// Allow NPC interaction via hover text.
        /// </summary>
        [HarmonyPatch(nameof(Character.GetHoverText))]
        [HarmonyPostfix]
        private static void GetHoverText_Postfix(Character __instance, ref string __result)
        {
            if (__instance == null) return;

            var npcAI = __instance.GetComponent<NPCAI>();
            if (npcAI != null)
            {
                var denizen = __instance.GetComponent<DenizenComponent>();
                if (denizen?.Config is Entities.NPCConfig npcConfig)
                {
                    string name = Localization.instance.Localize(npcConfig.Name);
                    string title = !string.IsNullOrEmpty(npcConfig.Title)
                        ? Localization.instance.Localize(npcConfig.Title)
                        : "";

                    __result = $"{name}";
                    if (!string.IsNullOrEmpty(title))
                    {
                        __result += $"\n<color=yellow>{title}</color>";
                    }
                    __result += "\n[<color=yellow><b>$KEY_Use</b></color>] Talk";
                }
            }
        }
    }

    /// <summary>
    /// Harmony patches for Player class (NPC interaction trigger).
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatches
    {
        /// <summary>
        /// Intercept interact to handle NPC interaction.
        /// </summary>
        [HarmonyPatch(nameof(Player.Interact))]
        [HarmonyPrefix]
        private static bool Interact_Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            if (go == null || hold) return true;

            var npcAI = go.GetComponent<NPCAI>();
            if (npcAI != null)
            {
                npcAI.OnInteract(__instance);
                return false; // Skip vanilla interact
            }

            return true; // Continue to vanilla
        }
    }

    /// <summary>
    /// Harmony patches for Tameable class (companion taming).
    /// </summary>
    [HarmonyPatch(typeof(Tameable))]
    internal static class TameablePatches
    {
        /// <summary>
        /// Intercept tame completion for companions.
        /// </summary>
        [HarmonyPatch(nameof(Tameable.Tame))]
        [HarmonyPostfix]
        private static void Tame_Postfix(Tameable __instance)
        {
            if (__instance == null) return;

            var character = __instance.GetComponent<Character>();
            var companionAI = __instance.GetComponent<CompanionAI>();

            if (companionAI != null && character != null)
            {
                // Find the player who tamed (nearest player)
                var tamer = Player.GetClosestPlayer(__instance.transform.position, 20f);
                if (tamer != null)
                {
                    companionAI.SetOwner(tamer);
                    DenizenEvents.RaiseCompanionTamed(character, tamer);
                }
            }
        }
    }
}
