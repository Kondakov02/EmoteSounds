using EmoteLaugh.Core;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace EmoteLaugh.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void spawnNetManager(StartOfRound __instance)
        {
            if (__instance.IsHost)
            {
                GameObject go = GameObject.Instantiate(ModBase.Instance.networkManagerPrefab);
                go.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
