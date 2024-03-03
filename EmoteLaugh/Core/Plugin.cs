using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EmoteLaugh.Network;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public static ConfigEntry<int> AudioVolume { get; private set; }

        public static Dictionary<int, AudioClip> EmoteSounds { get; private set; }
        public static List<int> InterruptableAudio { get; private set; }

        public GameObject networkManagerPrefab;

        public static ModBase Instance;

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

            // Load network manager and add handler to it
            networkManagerPrefab = assetLoader.LoadAsset<GameObject>("NetworkManagerEmoteSounds");
            networkManagerPrefab.AddComponent<NetworkHandler>();

            // Load sounds, warn users if the sounds could not be loaded
            AudioClip LaughAudio = assetLoader.LoadAsset<AudioClip>("laugh");
            AudioClip VineBoomAudio = assetLoader.LoadAsset<AudioClip>("vine boom");
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
                { 2, LaughAudio },          // Point finger emote
                { 3, VineBoomAudio },       // One middle finger emote
                { 1003, VineBoomAudio },    // Two middle fingers emote
                { 7, RizzAudio }            // Twerk emote
            };

            /* So far only point emote has preventable audio.
             * This is done to prevent cat laugh spam. Also has purpose for funny timing
             */
            InterruptableAudio = new List<int>() { 2 };

            // Generate config and convert audio volume to usable value for Unity
            AudioVolume = Config.Bind("General", "Audio volume", 
                    100, 
                    new ConfigDescription("How loud you want the sounds to be? Restart is NOT required.", 
                        new AcceptableValueRange<int>(0, 100), 
                        Array.Empty<object>()
                    )
                );

            try
            {
                var types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                        if (attributes.Length > 0)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }
            } 
            catch (Exception ex)
            {
                logger.LogError("Unity Netcode Patcher error\n" + ex.Message);
            }

            // Patch classes
            harmony.PatchAll();

            logger.LogInfo("Done patching");
        }
    }
}
