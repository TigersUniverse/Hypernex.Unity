using Hypernex.ExtendedTracking;
using UnityEngine;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class FaceTrackingDebugger : MonoBehaviour
    {
        private UnifiedTrackingData data;
        
        private void Start() => FaceTrackingManager.OnTrackingUpdated += d => data = d;

        private void OnGUI()
        {
            if(data == null) return;
            for (int i = 0; i < (int) UnifiedExpressions.Max; i++)
            {
                float val = data.Shapes[i].Weight;
                GUILayout.Label((UnifiedExpressions) i + ": " + val);
            }
        }
    }
}