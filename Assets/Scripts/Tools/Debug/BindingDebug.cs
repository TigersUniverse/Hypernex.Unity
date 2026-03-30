using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class BindingDebug : MonoBehaviour
    {
        private void OnGUI()
        {
            LocalPlayer player = LocalPlayer.Instance;
            if(player == null) return;
            foreach (IBinding playerBinding in player.Bindings)
            {
                GUILayout.Label($"Binding {playerBinding.Id}");
                GUILayout.Label($"IsLook: {playerBinding.IsLook}");
                GUILayout.Label($"Up: {playerBinding.Up}");
                GUILayout.Label($"Down: {playerBinding.Down}");
                GUILayout.Label($"Left: {playerBinding.Left}");
                GUILayout.Label($"Right: {playerBinding.Right}");
                GUILayout.Label($"Button: {playerBinding.Button}");
                GUILayout.Label($"Button2: {playerBinding.Button2}");
                GUILayout.Label($"Trigger: {playerBinding.Trigger}");
                GUILayout.Label($"Grab: {playerBinding.Grab}");
            }
        }
    }
}