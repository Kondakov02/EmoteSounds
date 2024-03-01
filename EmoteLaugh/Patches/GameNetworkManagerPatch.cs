using EmoteLaugh.Core;
using HarmonyLib;
using Unity.Netcode;

namespace EmoteLaugh.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void AddToPrefabs(ref GameNetworkManager __instance)
        {
            __instance.GetComponent<NetworkManager>().AddNetworkPrefab(ModBase.Instance.networkManagerPrefab);
        }
    }
}
