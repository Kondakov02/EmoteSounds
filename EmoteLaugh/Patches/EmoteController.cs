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


    }
}
