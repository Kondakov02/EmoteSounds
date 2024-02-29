using EmoteLaugh.Core;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    internal class EmoteController : NetworkBehaviour
    {
        private static PlayerControllerB __player;

        private static AudioSource __playerAudio;

        private static float oldAudioSourceVolume;

        private static AudioClip oldAudioClip;

        private const string emoteParamInAnimator = "emoteNumber";

        private static int previousEmoteID = 0;

        private static bool playingInterruptableAudio = false;

        public EmoteController(PlayerControllerB playerController)
        {
            __player = playerController;
            __playerAudio = playerController.movementAudio;
        }

        private void AboutToPlaySound()
        {
            if (!IsOwner) 
            {
                return; 
            }

            int currentEmoteID = __player.playerBodyAnimator.GetInteger(emoteParamInAnimator);

            if (ModBase.AllowDebug.Value)
            {
                ModBase.logger.LogDebug("Currently playing emote " + currentEmoteID);
            }

            // Check if emote is being performed and if there is a sound for it
            if (__player.performingEmote && ModBase.EmoteSounds.ContainsKey(currentEmoteID))
            {
                // Check if player hasn't pressed the same button again while emoting.
                // This is done to prevent sound spam.
                if (currentEmoteID != previousEmoteID)
                {
                    bool playLongAudio = ModBase.InterruptableAudio.Contains(currentEmoteID);

                    PlaySound(playLongAudio, currentEmoteID);
                }
            }

            previousEmoteID = currentEmoteID;

            PlaySoundSoundServerRpc(false, 0);
        }

        private void AboutToStopSound()
        {
            if (!IsOwner)
            {
                return;
            }

            StopEmoteSoundServerRpc(false);
        }

        private void PlaySound(bool playLongAudio, int emoteID)
        {
            if (!ModBase.EmoteSounds.TryGetValue(emoteID, out AudioClip audioToPlay))
            {
                return;
            }

            if (playLongAudio)
            {
                // Save old volume and audio clip (idk if movement audio has it, just in case)
                // Pitch is not saved because it is randomized with every footstep anyway
                oldAudioSourceVolume = __playerAudio.volume;
                oldAudioClip = __playerAudio.clip;

                // Set the new values and play the sound
                __playerAudio.volume = ModBase.AudioVolume;
                __playerAudio.clip = audioToPlay;
                __playerAudio.pitch = 1f;

                __playerAudio.Play();

                playingInterruptableAudio = true;
            }
            else
            {
                __playerAudio.PlayOneShot(audioToPlay, ModBase.AudioVolume);
            }

            WalkieTalkie.TransmitOneShotAudio(__playerAudio, audioToPlay, ModBase.AudioVolume);
        }

        private void StopSound()
        {
            previousEmoteID = 0;

            if (playingInterruptableAudio)
            {
                __playerAudio.Stop();
                __playerAudio.volume = oldAudioSourceVolume;
                __playerAudio.clip = oldAudioClip;
                playingInterruptableAudio = false;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void PlaySoundSoundServerRpc(bool playLongAudio, int emoteID)
        {
            PlaySoundSoundClientRpc(playLongAudio, emoteID);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void PlaySoundSoundClientRpc(bool playLongAudio, int emoteID)
        {
            if (IsOwner)
            {
                return;
            }
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void StopEmoteSoundServerRpc(bool playingLongAudio)
        {
            StopEmoteSoundClientRpc(playingLongAudio);
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void StopEmoteSoundClientRpc(bool playingLongAudio) 
        {
            if (IsOwner)
            {
                return;
            }
        }
    }
}
