using EmoteLaugh.Core;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void InjectController(PlayerControllerB __instance)
        {
            ((Component)__instance).gameObject.AddComponent<EmoteController>();
        }
    }
}
