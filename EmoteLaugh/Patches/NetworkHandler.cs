using System;
using Unity.Netcode;

namespace EmoteLaugh.Patches
{
    internal class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; }

        public static event Action<String> LevelEvent;

        public override void OnNetworkSpawn()
        {
            LevelEvent = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void EventServerRPC(/*parameters here*/)
        {
            // code here
        }

        [ClientRpc]
        public void EventClientRpc(string eventName)
        {
            LevelEvent?.Invoke(eventName);
        }
    }
}
