﻿using EmoteLaugh.Core;
using GameNetcodeStuff;
using HarmonyLib;

namespace EmoteLaugh.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void InjectController(PlayerControllerB __instance)
        {
            __instance.gameObject.AddComponent<EmoteController>();
        }
    }
}
