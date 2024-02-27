﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EmoteLaugh.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EmoteLaugh.Core
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class ModBase : BaseUnityPlugin
    {
        private const string modGUID = "thekagamiest.EmoteSounds";

        private const string modName = "EmoteSounds";

        private const string modVersion = "1.0.0";

        private const string assetName = "stupidsounds.bundle";

        public static float AudioVolume { get; private set; }

        public static ConfigEntry<bool> AllowDebug { get; private set; }

        public static Dictionary<int, AudioClip> EmoteSounds { get; private set; }
        public static List<int> InterruptableAudio { get; private set; }

        private static ModBase Instance;

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ManualLogSource logger;

        void Awake()
        {
            // Standard mod stuff
            if (Instance == null)
            {
                Instance = this;
            }

            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            logger.LogInfo("Plugin initialized");

            // Load asset bundle from the same directory as the .dll file itself
            string assetPath = Path.Combine(Path.GetDirectoryName(((BaseUnityPlugin)this).Info.Location), assetName);

            AssetBundle assetLoader = AssetBundle.LoadFromFile(assetPath);

            // Load sounds, warn users if the sounds could not be loaded
            AudioClip LaughAudio = assetLoader.LoadAsset<AudioClip>("laugh");
            AudioClip VineBoomAudio = assetLoader.LoadAsset<AudioClip>("boom");
            AudioClip RizzAudio = assetLoader.LoadAsset<AudioClip>("rizz");

            if (LaughAudio == null)
            {
                logger.LogWarning("Could not load laugh sound");
            }

            if (VineBoomAudio == null)
            {
                logger.LogWarning("Could not load vine boom sound");
            }

            if (RizzAudio == null)
            {
                logger.LogWarning("Could not load rizz sound");
            }

            // Create new dictionary (emoteID = Audio) and fill it up
            EmoteSounds = new Dictionary<int, AudioClip>
            {
                { 2, LaughAudio },          // Point emote
                { 3, VineBoomAudio },       // One middle finger emote
                { 1003, VineBoomAudio },    // Two middle fingers emote
                { 7, RizzAudio }            // Twerk emote
            };

            // So far only point emote has preventable audio.
            // This is done to prevent cat laugh spam. Also has purpose for funny timing
            InterruptableAudio = new List<int>() { 2 };

            // Generate config and convert audio volume to usable value for Unity
            AllowDebug = Config.Bind("Debug", "Allow debug logs", false, "Allow debug logs to be printed.");
            ConfigEntry<int> volumeEntry = Config.Bind("General", "Audio volume", 
                    75, 
                    new ConfigDescription("How loud you want the sounds to be?", 
                        new AcceptableValueRange<int>(0, 100), 
                        Array.Empty<object>()
                    )
                );

            AudioVolume = (float)(volumeEntry.Value / 100.0);

            // Patch classes
            harmony.PatchAll(typeof(ModBase));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
        }
    }
}