using Denizen.Entities;
using Denizen.UI;
using UnityEngine;

namespace Denizen.Core
{
    /// <summary>
    /// AI component for NPC entities.
    /// Handles interaction, dialogue, and services.
    /// </summary>
    public class NPCAI : DenizenAI
    {
        private NPCConfig _npcConfig;
        private Player _interactingPlayer;
        private float _idleChatTimer;
        private float _nextIdleChat;

        protected override void Start()
        {
            base.Start();
            _npcConfig = _denizen?.GetNPCConfig();
            _nextIdleChat = Random.Range(30f, 60f);
        }

        protected override void Update()
        {
            if (_zNetView != null && !_zNetView.IsOwner())
                return;

            base.Update();

            // Idle chatter
            if (_npcConfig?.Dialogue?.Idle != null && _npcConfig.Dialogue.Idle.Count > 0)
            {
                _idleChatTimer += Time.deltaTime;
                if (_idleChatTimer >= _nextIdleChat)
                {
                    DoIdleChat();
                    _idleChatTimer = 0f;
                    _nextIdleChat = Random.Range(30f, 90f);
                }
            }
        }

        protected override void UpdateIdle()
        {
            // NPCs don't actively seek threats
            // They only react when attacked
        }

        protected override Character FindThreat()
        {
            // NPCs are passive - don't look for threats
            return null;
        }

        /// <summary>
        /// Called when a player interacts with this NPC.
        /// </summary>
        public void OnInteract(Player player)
        {
            if (player == null) return;
            if (_npcConfig == null) return;

            _interactingPlayer = player;

            // Face the player
            Vector3 dir = (player.transform.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            // Show greeting
            if (!string.IsNullOrEmpty(_npcConfig.Dialogue?.Greeting))
            {
                ShowMessage(_npcConfig.Dialogue.Greeting);
            }

            // Trigger event
            DenizenEvents.RaiseNPCInteract(_character, player);

            // Open service menu or dialogue
            if (_npcConfig.Services?.Count > 0)
            {
                OpenServiceMenu(player);
            }
            else if (!string.IsNullOrEmpty(_npcConfig.Dialogue?.DialogueTree))
            {
                OpenDialogue(player);
            }
        }

        /// <summary>
        /// Open the service selection menu.
        /// </summary>
        private void OpenServiceMenu(Player player)
        {
            if (_npcConfig.Services == null || _npcConfig.Services.Count == 0)
                return;

            // Use the service UI system
            DenizenServiceUI.OpenServiceMenu(_character, player, _npcConfig.Services);
        }

        /// <summary>
        /// Open a specific service.
        /// </summary>
        public void OpenService(string serviceId, Player player)
        {
            if (_npcConfig?.Services == null) return;

            foreach (var service in _npcConfig.Services)
            {
                if (service.Id == serviceId)
                {
                    DenizenEvents.RaiseServiceOpened(_character, player, serviceId);
                    service.OnOpen(_character, player);
                    return;
                }
            }

            Plugin.Log.LogWarning($"Service not found: {serviceId}");
        }

        /// <summary>
        /// Open the dialogue tree.
        /// </summary>
        private void OpenDialogue(Player player)
        {
            if (!string.IsNullOrEmpty(_npcConfig.Dialogue?.DialogueTree))
            {
                DenizenDialogueUI.OpenDialogue(_character, player, _npcConfig.Dialogue.DialogueTree);
            }
            else if (_npcConfig.Dialogue != null)
            {
                // Simple dialogue (just greeting)
                DenizenDialogueUI.OpenSimpleDialogue(_character, player, _npcConfig.Dialogue);
            }
        }

        /// <summary>
        /// Close interaction.
        /// </summary>
        public void CloseInteraction()
        {
            if (_interactingPlayer != null && !string.IsNullOrEmpty(_npcConfig?.Dialogue?.Farewell))
            {
                ShowMessage(_npcConfig.Dialogue.Farewell);
            }

            // Close any open services
            if (_npcConfig?.Services != null)
            {
                foreach (var service in _npcConfig.Services)
                {
                    service.OnClose(_character, _interactingPlayer);
                }
            }

            _interactingPlayer = null;
        }

        /// <summary>
        /// Do idle chatter.
        /// </summary>
        private void DoIdleChat()
        {
            if (_npcConfig?.Dialogue?.Idle == null || _npcConfig.Dialogue.Idle.Count == 0)
                return;

            // Only if players are nearby
            var nearbyPlayer = Player.GetClosestPlayer(transform.position, 20f);
            if (nearbyPlayer == null) return;

            int index = Random.Range(0, _npcConfig.Dialogue.Idle.Count);
            ShowMessage(_npcConfig.Dialogue.Idle[index]);
        }

        /// <summary>
        /// Show a message above the NPC.
        /// </summary>
        private void ShowMessage(string messageKey)
        {
            string message = Localization.instance.Localize(messageKey);
            Chat.instance?.SendText(Talker.Type.Normal, message);
        }

        public override void OnDamaged(HitData hit, Character attacker)
        {
            base.OnDamaged(hit, attacker);

            // NPCs flee when attacked
            if (_behavior.FleeThreshold > 0)
            {
                SetState(CreatureState.Fleeing);
            }
        }
    }
}
