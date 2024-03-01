﻿using EmoteLaugh.Core;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    internal class EmoteController : NetworkBehaviour
    {
        private PlayerControllerB __player = null;

        private AudioSource __playerAudio = null;

        private float oldAudioSourceVolume = 0f;

        private AudioClip oldAudioClip = null;

        private const string emoteParamInAnimator = "emoteNumber";

        private int previousEmoteID = 0;

        private bool playingInterruptableAudio = false;

        private int UpdateCounter = 599;

        private bool playingSound = false;

        private void Start()
        {
            __player = GetComponent<PlayerControllerB>();
            __playerAudio = __player.movementAudio;
            ModBase.logger.LogInfo("Initialized emote controller");
        }

        private void LateUpdate()
        {
            

            UpdateCounter++;

            if (UpdateCounter >= 600)
            {
                ModBase.logger.LogWarning("Called update of emote controller, performing emote is " + __player.performingEmote);
                ModBase.logger.LogInfo("IsOwner " + __player.IsOwner);
                ModBase.logger.LogInfo("IsPlayerControlled " + __player.isPlayerControlled);
                UpdateCounter = 0;
            }


            if (!(__player.IsOwner && __player.isPlayerControlled))
            {
                return;
            }

            if (__player.performingEmote)
            {
                int currentEmoteID = __player.playerBodyAnimator.GetInteger(emoteParamInAnimator);

                if (currentEmoteID != previousEmoteID)
                {
                    if (playingSound)
                    {
                        playingSound = false;
                        AboutToStopSound();
                    }
                    
                    bool emoteHasSound = ModBase.EmoteSounds.ContainsKey(currentEmoteID);
                    
                    if (emoteHasSound)
                    {
                        ModBase.logger.LogInfo("Player is performing emote, preparing to play sound");
                        playingSound = true;

                        bool playLongAudio = ModBase.InterruptableAudio.Contains(currentEmoteID);

                        ModBase.logger.LogInfo("About to play long audio - " + playLongAudio);

                        // Play locally
                        PlaySound(playLongAudio, true, currentEmoteID);

                        // Send signal to everyone else
                        PlayEmoteSoundServerRpc(playLongAudio, currentEmoteID);
                    }
                }
                previousEmoteID = currentEmoteID;
            }

            if (!__player.performingEmote && playingSound)
            {
                ModBase.logger.LogInfo("Player stopped performing emote, stopping sound");
                playingSound = false;
                previousEmoteID = 0;
                AboutToStopSound();
            }
        }

        private void AboutToStopSound()
        {
            // Stop locally
            StopSound();

            // Send signal to everyone else
            StopEmoteSoundServerRpc();
        }

        private void PlaySound(bool playLongAudio, bool lowerVolume, int emoteID)
        {
            if (!ModBase.EmoteSounds.TryGetValue(emoteID, out AudioClip audioToPlay))
            {
                ModBase.logger.LogInfo("Could not get audio clip");
                return;
            }

            float audioVolume = ModBase.AudioVolume;

            if (lowerVolume)
            {
                audioVolume *= 0.1f;
            }

            if (playLongAudio)
            {
                ModBase.logger.LogInfo("Saving old audio source values");
                // Save old volume and audio clip (idk if movement audio has it, just in case)
                // Pitch is not saved because it is randomized with every footstep anyway
                oldAudioSourceVolume = __playerAudio.volume;
                oldAudioClip = __playerAudio.clip;

                ModBase.logger.LogInfo("Setting new audio source values");
                // Set the new values and play the sound
                __playerAudio.volume = audioVolume;
                __playerAudio.clip = audioToPlay;
                __playerAudio.pitch = 1f;

                ModBase.logger.LogInfo("Playing audio interruptable");
                __playerAudio.Play();

                playingInterruptableAudio = true;
            }
            else
            {
                ModBase.logger.LogInfo("Playing audio one shot");
                __playerAudio.PlayOneShot(audioToPlay, audioVolume);
            }

            ModBase.logger.LogInfo("Transmitting audio over walkie");
            WalkieTalkie.TransmitOneShotAudio(__playerAudio, audioToPlay, ModBase.AudioVolume);
        }

        private void StopSound()
        {
            if (playingInterruptableAudio)
            {
                ModBase.logger.LogInfo("Restoring audio source values");
                __playerAudio.Stop();
                __playerAudio.volume = oldAudioSourceVolume;
                __playerAudio.clip = oldAudioClip;
                playingInterruptableAudio = false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlayEmoteSoundServerRpc(bool playLongAudio, int emoteID)
        {
            ModBase.logger.LogInfo("Called server RPC for playing sound");
            PlayEmoteSoundClientRpc(playLongAudio, emoteID);
        }

        [ClientRpc]
        private void PlayEmoteSoundClientRpc(bool playLongAudio, int emoteID)
        {
            if (__player.IsOwner)
            {
                return;
            }
            ModBase.logger.LogInfo("Called client RPC for playing sound");

            PlaySound(playLongAudio, false, emoteID);
        }

        [ServerRpc(RequireOwnership = false)]
        private void StopEmoteSoundServerRpc()
        {
            ModBase.logger.LogInfo("Called server RPC for stopping sound");
            StopEmoteSoundClientRpc();
        }

        [ClientRpc]
        private void StopEmoteSoundClientRpc() 
        {
            if (__player.IsOwner)
            {
                return;
            }

            ModBase.logger.LogInfo("Called client RPC for stopping sound");
            StopSound();
        }
    }
}
