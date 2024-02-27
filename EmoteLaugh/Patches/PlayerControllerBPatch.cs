using EmoteLaugh.Core;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static float oldAudioSourceVolume;

        private static AudioClip oldAudioClip;

        private const string emoteParamInAnimator = "emoteNumber";

        private static int previousEmoteID = 0;

        private static bool playingInterruptableAudio = false;

        [HarmonyPatch(nameof(PlayerControllerB.StartPerformingEmoteClientRpc))]
        [HarmonyPostfix]
        public static void PlayEmote(ref Animator ___playerBodyAnimator, ref AudioSource ___movementAudio, bool ___performingEmote)
        {
            int currentEmoteID = ___playerBodyAnimator.GetInteger(emoteParamInAnimator);

            if (ModBase.AllowDebug.Value)
            {
                ModBase.logger.LogDebug("Currently playing emote " + currentEmoteID);
            }

            // Check if emote is being performed and if there is a sound for it
            if (___performingEmote && ModBase.EmoteSounds.ContainsKey(currentEmoteID))
            {
                // Check if player hasn't pressed the same button again while emoting.
                // This is done to prevent sound spam.
                if (currentEmoteID != previousEmoteID)
                {
                    // Choose which sound to play
                    AudioClip audioToPlay = ModBase.EmoteSounds.GetValueSafe(currentEmoteID);

                    // If the audio is long or annoying, check if it can be interrupted.
                    // If it can, let's play it the normal way. The sound will be interrupted by moving.
                    // If not, just play it as uninterruptable one shot.
                    if (ModBase.InterruptableAudio.Contains(currentEmoteID))
                    {
                        // Save old volume and audio clip (idk if movement audio has it, just in case)
                        oldAudioSourceVolume = ___movementAudio.volume;
                        oldAudioClip = ___movementAudio.clip;

                        // Set the new values and play the sound
                        ___movementAudio.volume = ModBase.AudioVolume;
                        ___movementAudio.clip = audioToPlay;

                        ___movementAudio.Play();

                        playingInterruptableAudio = true;
                    }
                    else
                    {
                        ___movementAudio.PlayOneShot(audioToPlay, ModBase.AudioVolume);
                    }

                    // Transmit the sound over walkie
                    WalkieTalkie.TransmitOneShotAudio(___movementAudio, audioToPlay, ModBase.AudioVolume);
                }
            }
            previousEmoteID = currentEmoteID;
        }

        [HarmonyPatch(nameof(PlayerControllerB.StopPerformingEmoteClientRpc))]
        [HarmonyPostfix]
        public static void StopEmote(ref AudioSource ___movementAudio, bool ___performingEmote)
        {
            if (!___performingEmote)
            {
                previousEmoteID = 0;
                if (playingInterruptableAudio)
                {
                    HaltAudioPlay(ref ___movementAudio);
                }
            }
        }

        // Stop playing current audio clip, also revert audio source settings
        // to what they were before
        private static void HaltAudioPlay(ref AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.volume = oldAudioSourceVolume;
            audioSource.clip = oldAudioClip;
            playingInterruptableAudio = false;
        }
    }
}
