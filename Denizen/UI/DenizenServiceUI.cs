using System;
using System.Collections.Generic;
using Denizen.Core;
using Denizen.Entities;
using Denizen.Integration;
using Denizen.NPC;
using Jotunn.Managers;
using UnityEngine;

namespace Denizen.UI
{
    /// <summary>
    /// Manages service UI for NPC shops, gambling, etc.
    /// Uses Veneer if available, falls back to vanilla Haldor-style shop.
    /// </summary>
    public static class DenizenServiceUI
    {
        private static object _currentWindow;
        private static Character _currentNPC;
        private static Player _currentPlayer;
        private static INPCService _currentService;

        /// <summary>
        /// Opens the service menu for an NPC.
        /// </summary>
        public static void OpenServiceMenu(Character npc, Player player, List<INPCService> services)
        {
            if (npc == null || player == null || services == null || services.Count == 0)
                return;

            // If only one service, open it directly
            if (services.Count == 1)
            {
                OpenService(npc, player, services[0]);
                return;
            }

            _currentNPC = npc;
            _currentPlayer = player;

            if (Plugin.HasVeneer)
            {
                ShowServiceMenuVeneer(npc, services);
            }
            else
            {
                // Fallback: just open first service
                OpenService(npc, player, services[0]);
            }
        }

        private static void ShowServiceMenuVeneer(Character npc, List<INPCService> services)
        {
            try
            {
                CloseWindow();

                string npcName = Localization.instance.Localize(npc.m_name);
                var window = Veneer.Core.VeneerAPI.CreateWindow(
                    $"services_{npcName}",
                    npcName,
                    300,
                    200 + services.Count * 40
                );

                if (window == null) return;
                _currentWindow = window;

                // Add service buttons
                float buttonY = 0.85f;
                float buttonHeight = 0.15f;

                foreach (var service in services)
                {
                    string label = Localization.instance.Localize(service.Label);
                    var srv = service; // Capture for closure

                    var btn = Veneer.Core.VeneerAPI.CreateButton(
                        window.Content,
                        label,
                        () =>
                        {
                            CloseWindow();
                            OpenService(_currentNPC, _currentPlayer, srv);
                        }
                    );

                    btn.RectTransform.anchorMin = new Vector2(0.1f, buttonY - buttonHeight);
                    btn.RectTransform.anchorMax = new Vector2(0.9f, buttonY);
                    btn.RectTransform.offsetMin = Vector2.zero;
                    btn.RectTransform.offsetMax = Vector2.zero;

                    buttonY -= buttonHeight + 0.03f;
                }

                // Close button
                var closeBtn = Veneer.Core.VeneerAPI.CreateButton(
                    window.Content,
                    "Close",
                    CloseWindow
                );
                closeBtn.RectTransform.anchorMin = new Vector2(0.3f, 0.05f);
                closeBtn.RectTransform.anchorMax = new Vector2(0.7f, 0.15f);

                window.Show();
                GUIManager.BlockInput(true);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to show service menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens a specific service.
        /// </summary>
        public static void OpenService(Character npc, Player player, INPCService service)
        {
            if (npc == null || player == null || service == null)
                return;

            _currentNPC = npc;
            _currentPlayer = player;
            _currentService = service;

            // Trigger the service
            service.OnOpen(npc, player);

            // Handle specific service types
            if (service is TradeService tradeService)
            {
                OpenTradeUI(npc, player, tradeService);
            }
            else if (service is GambleService gambleService)
            {
                OpenGambleUI(npc, player, gambleService);
            }
            // CustomService handles itself via callback
        }

        /// <summary>
        /// Opens the trade UI.
        /// </summary>
        private static void OpenTradeUI(Character npc, Player player, TradeService service)
        {
            if (Plugin.HasVeneer)
            {
                try
                {
                    ShowTradeUIVeneer(npc, service);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to show trade UI: {ex.Message}");
                    ShowTradeUIVanilla(npc);
                }
            }
            else
            {
                ShowTradeUIVanilla(npc);
            }
        }

        private static void ShowTradeUIVeneer(Character npc, TradeService service)
        {
            CloseWindow();

            string npcName = Localization.instance.Localize(npc.m_name);
            var window = Veneer.Core.VeneerAPI.CreateWindow(
                $"trade_{npcName}",
                $"{npcName} - {Localization.instance.Localize(service.Label)}",
                600,
                450
            );

            if (window == null) return;
            _currentWindow = window;

            // Header with currency info
            int playerCurrency = TomeIntegration.GetPlayerCurrency(_currentPlayer, service.Currency);
            string currencyName = Localization.instance.Localize(service.Currency);

            var headerText = Veneer.Core.VeneerAPI.CreateText(
                window.Content,
                $"Your {currencyName}: {playerCurrency}",
                Veneer.Components.Primitives.TextStyle.Header
            );
            headerText.RectTransform.anchorMin = new Vector2(0, 0.9f);
            headerText.RectTransform.anchorMax = new Vector2(1, 1);
            headerText.RectTransform.offsetMin = new Vector2(10, 0);
            headerText.RectTransform.offsetMax = new Vector2(-10, -10);

            // TODO: Add actual shop inventory grid using VeneerItemGrid
            // For now just show placeholder
            var infoText = Veneer.Core.VeneerAPI.CreateText(
                window.Content,
                "Shop inventory coming soon...\nCheck back later!",
                Veneer.Components.Primitives.TextStyle.Body
            );
            infoText.RectTransform.anchorMin = new Vector2(0.1f, 0.3f);
            infoText.RectTransform.anchorMax = new Vector2(0.9f, 0.85f);

            // Close button
            var closeBtn = Veneer.Core.VeneerAPI.CreateButton(
                window.Content,
                "Close",
                CloseWindow
            );
            closeBtn.RectTransform.anchorMin = new Vector2(0.35f, 0.05f);
            closeBtn.RectTransform.anchorMax = new Vector2(0.65f, 0.15f);

            window.Show();
            GUIManager.BlockInput(true);
        }

        private static void ShowTradeUIVanilla(Character npc)
        {
            // Show message that trade is available
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, $"Trade with {Localization.instance.Localize(npc.m_name)}");
        }

        /// <summary>
        /// Opens the gamble UI.
        /// </summary>
        private static void OpenGambleUI(Character npc, Player player, GambleService service)
        {
            if (Plugin.HasVeneer)
            {
                try
                {
                    ShowGambleUIVeneer(npc, service);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to show gamble UI: {ex.Message}");
                }
            }
        }

        private static void ShowGambleUIVeneer(Character npc, GambleService service)
        {
            CloseWindow();

            string npcName = Localization.instance.Localize(npc.m_name);
            var window = Veneer.Core.VeneerAPI.CreateWindow(
                $"gamble_{npcName}",
                $"{npcName} - {Localization.instance.Localize(service.Label)}",
                400,
                300
            );

            if (window == null) return;
            _currentWindow = window;

            // Cost info
            int playerCurrency = TomeIntegration.GetPlayerCurrency(_currentPlayer, service.CostItem);
            string costItemName = Localization.instance.Localize(service.CostItem);
            bool canAfford = playerCurrency >= service.CostAmount;

            var costText = Veneer.Core.VeneerAPI.CreateText(
                window.Content,
                $"Cost: {service.CostAmount} {costItemName}\nYou have: {playerCurrency}",
                Veneer.Components.Primitives.TextStyle.Body
            );
            costText.RectTransform.anchorMin = new Vector2(0.1f, 0.5f);
            costText.RectTransform.anchorMax = new Vector2(0.9f, 0.9f);

            // Gamble button
            var gambleBtn = Veneer.Core.VeneerAPI.CreatePrimaryButton(
                window.Content,
                "Roll the Dice!",
                () => DoGamble(service)
            );
            gambleBtn.RectTransform.anchorMin = new Vector2(0.2f, 0.25f);
            gambleBtn.RectTransform.anchorMax = new Vector2(0.8f, 0.4f);

            if (!canAfford)
            {
                // Disable button visually
                gambleBtn.Interactable = false;
            }

            // Close button
            var closeBtn = Veneer.Core.VeneerAPI.CreateButton(
                window.Content,
                "Close",
                CloseWindow
            );
            closeBtn.RectTransform.anchorMin = new Vector2(0.35f, 0.05f);
            closeBtn.RectTransform.anchorMax = new Vector2(0.65f, 0.15f);

            window.Show();
            GUIManager.BlockInput(true);
        }

        private static void DoGamble(GambleService service)
        {
            if (_currentPlayer == null || service == null)
                return;

            // Check if player can afford
            if (!TomeIntegration.CanAfford(_currentPlayer, service.CostItem, service.CostAmount))
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Not enough currency!");
                return;
            }

            // Take payment
            TomeIntegration.ConsumeCurrency(_currentPlayer, service.CostItem, service.CostAmount);

            // Roll for success
            float roll = UnityEngine.Random.value;
            bool success = roll <= service.SuccessChance;

            if (success)
            {
                // Award random item based on gamble type
                // TODO: Implement proper gamble rewards
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "You won!");
                DenizenEvents.RaiseGambleWin(_currentNPC, _currentPlayer);
            }
            else
            {
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, "Better luck next time!");
                DenizenEvents.RaiseGambleLoss(_currentNPC, _currentPlayer);
            }

            // Refresh UI
            CloseWindow();
            OpenGambleUI(_currentNPC, _currentPlayer, service);
        }

        /// <summary>
        /// Closes the current window.
        /// </summary>
        public static void CloseWindow()
        {
            if (_currentService != null && _currentNPC != null && _currentPlayer != null)
            {
                _currentService.OnClose(_currentNPC, _currentPlayer);
            }

            if (Plugin.HasVeneer && _currentWindow != null)
            {
                try
                {
                    if (_currentWindow is Veneer.Components.Base.VeneerFrame frame)
                    {
                        frame.Hide();
                        UnityEngine.Object.Destroy(frame.gameObject);
                    }
                    GUIManager.BlockInput(false);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Failed to close window: {ex.Message}");
                }
            }

            _currentWindow = null;
            _currentService = null;
        }

        /// <summary>
        /// Whether service UI is currently open.
        /// </summary>
        public static bool IsOpen => _currentWindow != null;
    }
}
