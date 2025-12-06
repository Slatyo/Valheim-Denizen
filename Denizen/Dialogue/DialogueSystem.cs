using System;
using System.Collections.Generic;

namespace Denizen.Dialogue
{
    /// <summary>
    /// Dialogue tree system for NPCs.
    /// </summary>
    public static class DialogueSystem
    {
        private static readonly Dictionary<string, DialogueTree> _trees = new();

        /// <summary>
        /// Register a dialogue tree.
        /// </summary>
        public static void Register(string npcId, DialogueTree tree)
        {
            _trees[npcId] = tree;
            Plugin.Log.LogInfo($"Registered dialogue tree: {npcId}");
        }

        /// <summary>
        /// Get a dialogue tree for an NPC.
        /// </summary>
        public static DialogueTree Get(string npcId)
        {
            return _trees.TryGetValue(npcId, out var tree) ? tree : null;
        }

        /// <summary>
        /// Check if an NPC has a dialogue tree.
        /// </summary>
        public static bool Has(string npcId)
        {
            return _trees.ContainsKey(npcId);
        }

        /// <summary>
        /// Clear all dialogue trees.
        /// </summary>
        public static void Clear()
        {
            _trees.Clear();
        }
    }

    /// <summary>
    /// A complete dialogue tree.
    /// </summary>
    public class DialogueTree
    {
        /// <summary>Root node of the tree.</summary>
        public DialogueNode Root { get; set; }

        /// <summary>Named branches for navigation.</summary>
        public Dictionary<string, DialogueNode> Branches { get; set; } = new();

        /// <summary>Get a node by branch name.</summary>
        public DialogueNode GetBranch(string name)
        {
            if (name == "root") return Root;
            return Branches.TryGetValue(name, out var node) ? node : null;
        }
    }

    /// <summary>
    /// A single node in a dialogue tree.
    /// </summary>
    public class DialogueNode
    {
        /// <summary>Localization key for the text to display.</summary>
        public string Text { get; set; }

        /// <summary>Available response options.</summary>
        public List<DialogueOption> Options { get; set; } = new();

        /// <summary>Event triggered when this node is shown.</summary>
        public Action OnShow { get; set; }
    }

    /// <summary>
    /// A dialogue option/response.
    /// </summary>
    public class DialogueOption
    {
        /// <summary>Localization key for the option text.</summary>
        public string Text { get; set; }

        /// <summary>Branch name to navigate to when selected.</summary>
        public string Next { get; set; }

        /// <summary>Action to perform when selected.</summary>
        public DialogueAction Action { get; set; }

        /// <summary>Condition that must be met to show this option.</summary>
        public Func<bool> Condition { get; set; }

        /// <summary>Check if this option should be shown.</summary>
        public bool IsAvailable()
        {
            return Condition == null || Condition();
        }
    }

    /// <summary>
    /// Actions that can be performed from dialogue.
    /// </summary>
    public class DialogueAction
    {
        /// <summary>Type of action.</summary>
        public DialogueActionType Type { get; set; }

        /// <summary>Parameter for the action.</summary>
        public string Parameter { get; set; }

        /// <summary>Create a close dialogue action.</summary>
        public static DialogueAction Close => new() { Type = DialogueActionType.Close };

        /// <summary>Create an open service action.</summary>
        public static DialogueAction OpenService(string serviceId) =>
            new() { Type = DialogueActionType.OpenService, Parameter = serviceId };

        /// <summary>Create a give item action.</summary>
        public static DialogueAction GiveItem(string itemId) =>
            new() { Type = DialogueActionType.GiveItem, Parameter = itemId };

        /// <summary>Create a start quest action.</summary>
        public static DialogueAction StartQuest(string questId) =>
            new() { Type = DialogueActionType.StartQuest, Parameter = questId };

        /// <summary>Create a custom action.</summary>
        public static DialogueAction Custom(string id) =>
            new() { Type = DialogueActionType.Custom, Parameter = id };
    }

    /// <summary>
    /// Types of dialogue actions.
    /// </summary>
    public enum DialogueActionType
    {
        /// <summary>Close the dialogue.</summary>
        Close,
        /// <summary>Navigate to a branch.</summary>
        Navigate,
        /// <summary>Open an NPC service.</summary>
        OpenService,
        /// <summary>Give an item to the player.</summary>
        GiveItem,
        /// <summary>Start a quest.</summary>
        StartQuest,
        /// <summary>Custom action handled by event.</summary>
        Custom
    }

    /// <summary>
    /// Common condition helpers for dialogue.
    /// </summary>
    public static class Conditions
    {
        /// <summary>Check if player has killed a boss.</summary>
        public static Func<bool> HasKilledBoss(string bossKey) =>
            () => ZoneSystem.instance?.GetGlobalKey(bossKey) ?? false;

        /// <summary>Check if player has an item.</summary>
        public static Func<bool> HasItem(string itemName, int count = 1) =>
            () => Player.m_localPlayer?.GetInventory()?.CountItems(itemName) >= count;

        /// <summary>Check if a global key is set.</summary>
        public static Func<bool> HasGlobalKey(string key) =>
            () => ZoneSystem.instance?.GetGlobalKey(key) ?? false;

        /// <summary>Combine multiple conditions with AND.</summary>
        public static Func<bool> All(params Func<bool>[] conditions) =>
            () =>
            {
                foreach (var condition in conditions)
                {
                    if (!condition()) return false;
                }
                return true;
            };

        /// <summary>Combine multiple conditions with OR.</summary>
        public static Func<bool> Any(params Func<bool>[] conditions) =>
            () =>
            {
                foreach (var condition in conditions)
                {
                    if (condition()) return true;
                }
                return false;
            };

        /// <summary>Negate a condition.</summary>
        public static Func<bool> Not(Func<bool> condition) =>
            () => !condition();
    }
}
