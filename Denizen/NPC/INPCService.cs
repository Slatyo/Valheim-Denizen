using System;
using Denizen.Core;
using UnityEngine;

namespace Denizen.NPC
{
    /// <summary>
    /// Interface for NPC services.
    /// </summary>
    public interface INPCService
    {
        /// <summary>Service ID.</summary>
        string Id { get; }

        /// <summary>Localization key for service label.</summary>
        string Label { get; }

        /// <summary>Icon for service button.</summary>
        Sprite Icon { get; }

        /// <summary>Called when player opens this service.</summary>
        void OnOpen(Character npc, Player player);

        /// <summary>Called when player closes this service.</summary>
        void OnClose(Character npc, Player player);
    }

    /// <summary>
    /// Trading service for NPCs.
    /// </summary>
    public class TradeService : INPCService
    {
        public string Id => "trade";
        public string Label { get; set; } = "$service_trade";
        public Sprite Icon { get; set; }

        /// <summary>Loot table for shop inventory.</summary>
        public string ShopTable { get; set; }

        /// <summary>Currency item ID (from Tome).</summary>
        public string Currency { get; set; } = "Coins";

        /// <summary>Price multiplier for all items.</summary>
        public float PriceMultiplier { get; set; } = 1f;

        /// <summary>Whether to refresh stock periodically.</summary>
        public bool RefreshStock { get; set; } = true;

        /// <summary>Stock refresh interval in seconds.</summary>
        public float RefreshInterval { get; set; } = 3600f;

        public void OnOpen(Character npc, Player player)
        {
            // TODO: Open trade UI via Veneer
            Plugin.Log.LogDebug($"Opening trade service for {npc.m_name}");
        }

        public void OnClose(Character npc, Player player)
        {
            Plugin.Log.LogDebug($"Closing trade service for {npc.m_name}");
        }
    }

    /// <summary>
    /// Gambling service for NPCs.
    /// </summary>
    public class GambleService : INPCService
    {
        public string Id => "gamble";
        public string Label { get; set; } = "$service_gamble";
        public Sprite Icon { get; set; }

        /// <summary>Type of gambling offered.</summary>
        public GambleType Type { get; set; }

        /// <summary>Cost item ID.</summary>
        public string CostItem { get; set; }

        /// <summary>Cost amount.</summary>
        public int CostAmount { get; set; }

        /// <summary>Success chance for risky gambles.</summary>
        public float SuccessChance { get; set; } = 1f;

        public void OnOpen(Character npc, Player player)
        {
            // TODO: Open gamble UI via Veneer
            Plugin.Log.LogDebug($"Opening gamble service for {npc.m_name}");
        }

        public void OnClose(Character npc, Player player)
        {
            Plugin.Log.LogDebug($"Closing gamble service for {npc.m_name}");
        }
    }

    /// <summary>
    /// Custom service for NPCs (mod-defined).
    /// </summary>
    public class CustomService : INPCService
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public Sprite Icon { get; set; }

        /// <summary>Callback when service is opened.</summary>
        public Action<Character, Player> OnOpenCallback { get; set; }

        /// <summary>Callback when service is closed.</summary>
        public Action<Character, Player> OnCloseCallback { get; set; }

        public CustomService(string id, string label, Action<Character, Player> onOpen)
        {
            Id = id;
            Label = label;
            OnOpenCallback = onOpen;
        }

        public void OnOpen(Character npc, Player player)
        {
            OnOpenCallback?.Invoke(npc, player);
        }

        public void OnClose(Character npc, Player player)
        {
            OnCloseCallback?.Invoke(npc, player);
        }
    }
}
