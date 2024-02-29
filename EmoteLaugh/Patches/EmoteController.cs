using EmoteLaugh.Core;
using GameNetcodeStuff;
using System;
using System.Reflection;
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

        private static MethodInfo CheckConditionsForEmote;

        private void Start()
        {
            __player = ((Component)this).GetComponent<PlayerControllerB>();
            __playerAudio = __player.movementAudio;

            // "Check conditions" method is private, so get it through reflection
            CheckConditionsForEmote = __player.GetType().GetMethod("CheckConditionsForEmote", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (__player.performingEmote)
            {
                AboutToPlaySound();

                if (CheckConditionsForEmote == null)
                {
                    return;
                }

                bool canPerformEmote = (bool)CheckConditionsForEmote.Invoke(__player, Array.Empty<object>());

                if (!canPerformEmote)
                {
                    AboutToStopSound();
                }
            }
        }

        private void AboutToPlaySound()
        {
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
                    // Safety net for when the "check conditions" method is not found in PlayerController class.
                    // If it is not here, then EmoteController can't stop the sound and restore AudioSource values.
                    // Instead, play the long sound as uninterruptable.
                    bool playLongAudio = ModBase.InterruptableAudio.Contains(currentEmoteID) && CheckConditionsForEmote != null;

                    // Play locally
                    PlaySound(playLongAudio, currentEmoteID);

                    // Send signal to everyone else
                    PlaySoundSoundServerRpc(playLongAudio, currentEmoteID);
                }
            }

            previousEmoteID = currentEmoteID;
        }

        private void AboutToStopSound()
        {
            // Stop locally
            StopSound();

            // Send signal to everyone else
            StopEmoteSoundServerRpc();
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

            PlaySound(playLongAudio, emoteID);
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void StopEmoteSoundServerRpc()
        {
            StopEmoteSoundClientRpc();
        }

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void StopEmoteSoundClientRpc() 
        {
            if (IsOwner)
            {
                return;
            }

            StopSound();
        }
    }
}
