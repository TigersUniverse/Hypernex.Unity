using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class InstanceDebug : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.Label("Number of Instances: " + GameInstance.GameInstances.Length);
            GameInstance gameInstance = GameInstance.FocusedInstance;
            if (gameInstance == null)
            {
                GUILayout.Label("Not connected!");
                return;
            }
            GUILayout.Label("Is Open: " + gameInstance.IsOpen);
            GUILayout.Label("Game Server: " + gameInstance.gameServerId);
            GUILayout.Label("Instance: " + gameInstance.instanceId);
            GUILayout.Label("Is Host: " + gameInstance.isHost);
            GUILayout.Label("Is Authed: " + gameInstance.authed);
        }
    }
}