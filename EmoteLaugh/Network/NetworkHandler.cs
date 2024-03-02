using EmoteLaugh.Core;
using Unity.Netcode;

namespace EmoteLaugh.Network
{
    public class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler instance;

        void Awake()
        {
            instance = this;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayEmoteSoundServerRpc()
        {
            ModBase.logger.LogInfo("Called server RPC for playing sound");
            PlayEmoteSoundClientRpc();
        }

        [ClientRpc]
        public void PlayEmoteSoundClientRpc()
        {
            ModBase.logger.LogInfo("Called client RPC for playing sound");
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopEmoteSoundServerRpc()
        {
            ModBase.logger.LogInfo("Called server RPC for stopping sound");
            StopEmoteSoundClientRpc();
        }

        [ClientRpc]
        public void StopEmoteSoundClientRpc()
        {
            ModBase.logger.LogInfo("Called client RPC for stopping sound");
        }
    }
}
