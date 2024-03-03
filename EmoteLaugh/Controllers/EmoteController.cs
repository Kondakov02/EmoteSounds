using EmoteLaugh.Core;
using EmoteLaugh.Network;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    internal class EmoteController : MonoBehaviour
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

        /* This is used to determine which EmoteController instance to call in ClientRpc
         * See GetEmoteController method in NetworkHandler class
         */
        private ulong GetPlayerID()
        {
            return StartOfRound.Instance.localPlayerController.GetComponent<NetworkObject>().NetworkObjectId;
        }

        private void Start()
        {
            __player = GetComponent<PlayerControllerB>();
            __playerAudio = __player.movementAudio;
            ModBase.logger.LogInfo("Initialized emote controller");
        }

        private void LateUpdate()
        {
            if (!(__player.IsOwner && __player.isPlayerControlled))
            {
                return;
            }

            //UpdateCounter++;

            if (UpdateCounter >= 600)
            {
                ModBase.logger.LogWarning("Called update of emote controller, performing emote is " + __player.performingEmote);
                ModBase.logger.LogInfo("IsOwner " + __player.IsOwner);
                ModBase.logger.LogInfo("IsPlayerControlled " + __player.isPlayerControlled);
                UpdateCounter = 0;
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
                        PlaySound(playLongAudio, currentEmoteID, false);

                        // Send signal to everyone else
                        NetworkHandler.instance.PlayEmoteSoundServerRpc(GetPlayerID(), playLongAudio, currentEmoteID);
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
            StopSound(false);

            // Send signal to everyone else
            NetworkHandler.instance.StopEmoteSoundServerRpc(GetPlayerID());
        }

        public void PlaySound(bool playLongAudio, int emoteID, bool calledFromRpc)
        {
            if (calledFromRpc && __player.IsOwner)
            {
                return;
            }

            if (!ModBase.EmoteSounds.TryGetValue(emoteID, out AudioClip audioToPlay))
            {
                ModBase.logger.LogInfo("Could not get audio clip");
                return;
            }

            float audioVolume = ModBase.AudioVolume;

            if (__player.IsOwner)
            {
                audioVolume *= 0.1f;
            }

            if (playLongAudio)
            {
                ModBase.logger.LogInfo("Saving old audio source values");
                /* Save old volume and audio clip (idk if movement audio has it, just in case)
                 * Pitch is not saved because it is randomized with every footstep anyway
                 */
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

        public void StopSound(bool calledFromRpc)
        {
            if (calledFromRpc && __player.IsOwner)
            {
                return;
            }

            if (playingInterruptableAudio)
            {
                ModBase.logger.LogInfo("Restoring audio source values");
                __playerAudio.Stop();
                __playerAudio.volume = oldAudioSourceVolume;
                __playerAudio.clip = oldAudioClip;
                playingInterruptableAudio = false;
            }
        }
    }
}
