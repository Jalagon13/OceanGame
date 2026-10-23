using Unity.Netcode;
using UnityEngine;

namespace SketchLand
{
    public class NetworkPlayerBinding : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            Player.Instance.BindCharacter(GetComponent<ServerCharacter>());
        }
    }
}
