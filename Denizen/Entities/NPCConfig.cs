using System.Collections.Generic;
using Denizen.Core;
using Denizen.NPC;

namespace Denizen.Entities
{
    /// <summary>
    /// Configuration for NPC entities.
    /// </summary>
    public class NPCConfig : DenizenConfig
    {
        /// <summary>Localization key for NPC title (e.g., "The Enchanter").</summary>
        public string Title { get; set; }

        /// <summary>Services this NPC provides.</summary>
        public List<INPCService> Services { get; set; } = new();

        /// <summary>Dialogue configuration.</summary>
        public DialogueConfig Dialogue { get; set; }

        public NPCConfig()
        {
            Type = DenizenType.NPC;
            Behavior = new BehaviorConfig
            {
                Aggression = Aggression.Passive,
                FleeThreshold = 0.3f
            };
        }
    }

    /// <summary>
    /// Configuration for NPC dialogue.
    /// </summary>
    public class DialogueConfig
    {
        /// <summary>Localization key for greeting.</summary>
        public string Greeting { get; set; }

        /// <summary>Localization key for farewell.</summary>
        public string Farewell { get; set; }

        /// <summary>Localization keys for idle chatter.</summary>
        public List<string> Idle { get; set; } = new();

        /// <summary>Full dialogue tree ID (if using advanced dialogue).</summary>
        public string DialogueTree { get; set; }
    }
}
