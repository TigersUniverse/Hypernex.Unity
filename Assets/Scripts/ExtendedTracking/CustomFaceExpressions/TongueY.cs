using Hypernex.CCK.Unity;
using VRCFaceTracking.Core.Params.Data;

namespace Hypernex.ExtendedTracking.CustomFaceExpressions
{
    public class TongueY : ICustomFaceExpression
    {
        public string Name => "TongueY";
        
        public float GetWeight(UnifiedTrackingData data)
        {
            float tongueUp = data.Shapes[(int) FaceExpressions.TongueUp].Weight;
            float tongueDown = data.Shapes[(int) FaceExpressions.TongueDown].Weight * -1;
            return tongueUp + tongueDown;
        }
    }
}