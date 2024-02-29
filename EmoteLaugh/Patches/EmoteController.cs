using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    internal class EmoteController : NetworkBehaviour
    {
        private static float oldAudioSourceVolume;

        private static AudioClip oldAudioClip;

        private const string emoteParamInAnimator = "emoteNumber";

        private static int previousEmoteID = 0;

        private static bool playingInterruptableAudio = false;

        [ServerRpc (RequireOwnership = false)]
        private void PlayEmoteSoundServerRpc(bool playLongAudio, byte emoteID)
        {
            PlayEmoteSoundClientRpc(playLongAudio, emoteID);
        }

        [ClientRpc]
        private void PlayEmoteSoundClientRpc(bool playLongAudio, byte emoteID)
        {

        }

        [ServerRpc (RequireOwnership = false)]
        private void StopEmoteSoundServerRpc(bool playingLongAudio)
        {
            StopEmoteSoundClientRpc(playingLongAudio);
        }

        [ClientRpc]
        private void StopEmoteSoundClientRpc(bool playingLongAudio) 
        {
            
        }
    }
}
