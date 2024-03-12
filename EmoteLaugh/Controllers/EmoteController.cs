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
        }

        private void LateUpdate()
        {
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
                        playingSound = true;

                        bool playLongAudio = ModBase.InterruptableAudio.Contains(currentEmoteID);

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

            bool muffled = __player.isInHangarShipRoom && __player.playersManager.hangarDoorsClosed;
            RoundManager.Instance.PlayAudibleNoise(__player.transform.position, 30f, 1f, 0, muffled, 0);

            if (!ModBase.EmoteSounds.TryGetValue(emoteID, out AudioClip audioToPlay))
            {
                ModBase.logger.LogInfo("Could not get audio clip");
                return;
            }

            float globalVolume = (float)(ModBase.GlobalAudioVolume.Value * 0.01);
            float audioVolume = globalVolume;

            if (__player.IsOwner)
            {
                audioVolume = (float)(ModBase.LocalAudioVolume.Value * 0.01);
            }

            if (playLongAudio)
            {
                /* Save old volume and audio clip (idk if movement audio has it, just in case)
                 * Pitch is not saved because it is randomized with every footstep anyway
                 */
                oldAudioSourceVolume = __playerAudio.volume;
                oldAudioClip = __playerAudio.clip;

                // Set the new values and play the sound
                __playerAudio.volume = audioVolume;
                __playerAudio.clip = audioToPlay;
                __playerAudio.pitch = 1f;

                __playerAudio.Play();

                playingInterruptableAudio = true;
            }
            else
            {
                __playerAudio.PlayOneShot(audioToPlay, audioVolume);
            }

            WalkieTalkie.TransmitOneShotAudio(__playerAudio, audioToPlay, globalVolume);
        }

        public void StopSound(bool calledFromRpc)
        {
            if (calledFromRpc && __player.IsOwner)
            {
                return;
            }

            if (playingInterruptableAudio)
            {
                __playerAudio.Stop();
                __playerAudio.volume = oldAudioSourceVolume;
                __playerAudio.clip = oldAudioClip;
                playingInterruptableAudio = false;
            }
        }
    }
}
