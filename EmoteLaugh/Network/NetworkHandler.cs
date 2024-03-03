using EmoteLaugh.Core;
using EmoteLaugh.Patches;
using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Network
{
    public class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler instance;

        void Awake()
        {
            instance = this;
        }

        /* Get EmoteController of the player who called rpc, aka who wants to play the sound.
         * There is probably a better way to play the sound across the network,
         * without going back into certain EmoteController.
         * But that would likely require a code structure change and rewrite.
         * Let this be the way for now.
         */
        private EmoteController GetEmoteController(ulong playerID)
        {
            NetworkObject networkObj = GetNetworkObject(playerID);

            if (networkObj == null)
            {
                ModBase.logger.LogError("Could not find network object for this player ID " + playerID);
                return null;
            }

            GameObject player = networkObj.gameObject;

            if (player == null)
            {
                ModBase.logger.LogError("Could not get game object for this player ID " + playerID);
                return null;
            }

            EmoteController emoteController = player.GetComponentInChildren<EmoteController>();

            if (emoteController == null)
            {
                ModBase.logger.LogError("Could not find emote controller for this player ID " + playerID);
            }

            return emoteController;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayEmoteSoundServerRpc(ulong playerID, bool playLongAudio, int emoteID)
        {
            ModBase.logger.LogInfo("Called server RPC for playing sound");
            PlayEmoteSoundClientRpc(playerID, playLongAudio, emoteID);
        }

        [ClientRpc]
        public void PlayEmoteSoundClientRpc(ulong playerID, bool playLongAudio, int emoteID)
        {
            ModBase.logger.LogInfo("Called client RPC for playing sound");

            EmoteController emoteC = GetEmoteController(playerID);

            if (emoteC != null)
            {
                emoteC.PlaySound(playLongAudio, emoteID, true);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopEmoteSoundServerRpc(ulong playerID)
        {
            ModBase.logger.LogInfo("Called server RPC for stopping sound");
            StopEmoteSoundClientRpc(playerID);
        }

        [ClientRpc]
        public void StopEmoteSoundClientRpc(ulong playerID)
        {
            ModBase.logger.LogInfo("Called client RPC for stopping sound");

            EmoteController emoteC = GetEmoteController(playerID);

            if (emoteC != null)
            {
                emoteC.StopSound(true);
            }
        }
    }
}
