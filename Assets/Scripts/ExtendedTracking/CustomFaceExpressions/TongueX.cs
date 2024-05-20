using Hypernex.CCK.Unity;
using VRCFaceTracking.Core.Params.Data;

namespace Hypernex.ExtendedTracking.CustomFaceExpressions
{
    public class TongueX : ICustomFaceExpression
    {
        public string Name => "TongueX";
        
        public float GetWeight(UnifiedTrackingData data)
        {
            float tongueLeft = data.Shapes[(int) FaceExpressions.TongueLeft].Weight * -1;
            float tongueRight = data.Shapes[(int) FaceExpressions.TongueRight].Weight;
            return tongueRight + tongueLeft;
        }
    }
}