using System;
using System.Collections.Generic;
using Denizen.Core;
using Denizen.Dialogue;
using Denizen.Entities;
using Denizen.Integration;
using Jotunn.Managers;
using UnityEngine;

namespace Denizen.UI
{
    /// <summary>
    /// Manages dialogue UI for NPC interactions.
    /// Uses Veneer if available, falls back to vanilla messages.
    /// </summary>
    public static class DenizenDialogueUI
    {
        private static object _currentWindow;
        private static DialogueTree _currentTree;
        private static DialogueNode _currentNode;
        private static Character _currentNPC;
        private static Player _currentPlayer;

        /// <summary>
        /// Opens dialogue with an NPC.
        /// </summary>
        public static void OpenDialogue(Character npc, Player player, string dialogueTreeId)
        {
            if (npc == null || player == null || string.IsNullOrEmpty(dialogueTreeId))
                return;

            var tree = DialogueSystem.Get(dialogueTreeId);
            if (tree == null)
            {
                Plugin.Log.LogWarning($"Dialogue tree not found: {dialogueTreeId}");
                return;
            }

            _currentTree = tree;
            _currentNPC = npc;
            _currentPlayer = player;

            ShowNode(tree.Root);
        }

        /// <summary>
        /// Opens a simple greeting dialogue for an NPC.
        /// </summary>
        public static void OpenSimpleDialogue(Character npc, Player player, DialogueConfig dialogue)
        {
            if (npc == null || player == null)
                return;

            _currentNPC = npc;
            _currentPlayer = player;

            // Show greeting if available
            if (!string.IsNullOrEmpty(dialogue?.Greeting))
            {
                ShowMessage(dialogue.Greeting);
            }
        }

        /// <summary>
        /// Shows a dialogue node.
        /// </summary>
        private static void ShowNode(DialogueNode node)
        {
            if (node == null)
            {
                Close();
                return;
            }

            _currentNode = node;

            // Trigger OnShow callback if set
            node.OnShow?.Invoke();

            // Show the dialogue text
            string text = Localization.instance.Localize(node.Text);

            if (Plugin.HasVeneer)
            {
                ShowNodeVeneer(text, node.Options);
            }
            else
            {
                // Fallback: Show in chat and close
                ShowMessage(node.Text);

                // If there are options, just take the first one
                if (node.Options?.Count > 0)
                {
                    var firstOption = node.Options[0];
                    if (firstOption.IsAvailable())
                    {
                        SelectOption(firstOption);
                    }
                }
                else
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Shows a dialogue node using Veneer UI.
        /// </summary>
        private static void ShowNodeVeneer(string text, List<DialogueOption> options)
        {
            try
            {
                ShowNodeVeneerInternal(text, options);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to show Veneer dialogue: {ex.Message}");
                // Fallback to vanilla
                ShowMessage(text);
            }
        }

        private static void ShowNodeVeneerInternal(string text, List<DialogueOption> options)
        {
            // Close existing window if any
            CloseVeneerWindow();

            string npcName = _currentNPC != null
                ? Localization.instance.Localize(_currentNPC.m_name)
                : "NPC";

            // Create window using Veneer
            var window = Veneer.Core.VeneerAPI.CreateWindow(
                $"dialogue_{npcName}",
                npcName,
                500,
                300
            );

            if (window == null)
            {
                Plugin.Log.LogWarning("Failed to create Veneer window");
                return;
            }

            _currentWindow = window;

            // Add dialogue text to the content area
            var textElem = Veneer.Core.VeneerAPI.CreateText(window.Content, text, Veneer.Components.Primitives.TextStyle.Body);
            textElem.RectTransform.anchorMin = new Vector2(0, 0.4f);
            textElem.RectTransform.anchorMax = new Vector2(1, 1);
            textElem.RectTransform.offsetMin = new Vector2(10, 10);
            textElem.RectTransform.offsetMax = new Vector2(-10, -10);

            // Add response buttons
            if (options != null)
            {
                float buttonY = 0.35f;
                float buttonHeight = 0.12f;

                foreach (var option in options)
                {
                    if (!option.IsAvailable())
                        continue;

                    string optionText = Localization.instance.Localize(option.Text);
                    var opt = option; // Capture for closure
                    var btn = Veneer.Core.VeneerAPI.CreateButton(window.Content, optionText, () => SelectOption(opt));

                    btn.RectTransform.anchorMin = new Vector2(0.1f, buttonY - buttonHeight);
                    btn.RectTransform.anchorMax = new Vector2(0.9f, buttonY);
                    btn.RectTransform.offsetMin = Vector2.zero;
                    btn.RectTransform.offsetMax = Vector2.zero;

                    buttonY -= buttonHeight + 0.02f;
                }
            }

            // Show the window
            window.Show();

            // Block player input
            GUIManager.BlockInput(true);
        }

        /// <summary>
        /// Selects a dialogue option.
        /// </summary>
        public static void SelectOption(DialogueOption option)
        {
            if (option == null)
                return;

            // Execute action if any
            if (option.Action != null)
            {
                ExecuteAction(option.Action);
            }

            // Navigate to next node
            if (!string.IsNullOrEmpty(option.Next) && _currentTree != null)
            {
                var nextNode = _currentTree.GetBranch(option.Next);
                ShowNode(nextNode);
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Executes a dialogue action.
        /// </summary>
        private static void ExecuteAction(DialogueAction action)
        {
            if (action == null || _currentPlayer == null)
                return;

            switch (action.Type)
            {
                case DialogueActionType.Close:
                    Close();
                    break;

                case DialogueActionType.OpenService:
                    if (_currentNPC != null)
                    {
                        var npcAI = _currentNPC.GetComponent<NPCAI>();
                        npcAI?.OpenService(action.Parameter, _currentPlayer);
                    }
                    break;

                case DialogueActionType.GiveItem:
                    GiveItem(action.Parameter);
                    break;

                case DialogueActionType.StartQuest:
                    // Quest system integration (future)
                    Plugin.Log.LogInfo($"Would start quest: {action.Parameter}");
                    break;

                case DialogueActionType.Custom:
                    DenizenEvents.RaiseDialogueAction(_currentNPC, _currentPlayer, action.Parameter);
                    break;
            }
        }

        /// <summary>
        /// Gives an item to the current player.
        /// </summary>
        private static void GiveItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || _currentPlayer == null)
                return;

            // Try Tome first, then vanilla
            var prefab = ZNetScene.instance?.GetPrefab(itemName);
            if (prefab == null)
            {
                Plugin.Log.LogWarning($"Item not found: {itemName}");
                return;
            }

            if (_currentPlayer.GetInventory()?.AddItem(prefab, 1) == true)
            {
                Plugin.Log.LogInfo($"Gave {itemName} to player");
            }
        }

        /// <summary>
        /// Shows a message from the NPC.
        /// </summary>
        private static void ShowMessage(string messageKey)
        {
            if (string.IsNullOrEmpty(messageKey))
                return;

            string message = Localization.instance.Localize(messageKey);
            Chat.instance?.SendText(Talker.Type.Normal, message);
        }

        /// <summary>
        /// Closes the dialogue.
        /// </summary>
        public static void Close()
        {
            CloseVeneerWindow();

            _currentTree = null;
            _currentNode = null;
            _currentNPC = null;
            _currentPlayer = null;
        }

        private static void CloseVeneerWindow()
        {
            if (!Plugin.HasVeneer || _currentWindow == null)
                return;

            try
            {
                if (_currentWindow is Veneer.Components.Base.VeneerFrame frame)
                {
                    frame.Hide();
                    UnityEngine.Object.Destroy(frame.gameObject);
                }

                // Unblock input
                GUIManager.BlockInput(false);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to close dialogue window: {ex.Message}");
            }

            _currentWindow = null;
        }

        /// <summary>
        /// Whether dialogue is currently open.
        /// </summary>
        public static bool IsOpen => _currentWindow != null || _currentNode != null;
    }
}
