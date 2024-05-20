using VRCFaceTracking.Core.Params.Data;

namespace Hypernex.ExtendedTracking.CustomFaceExpressions
{
    public class CombinedEyeLid : ICustomFaceExpression
    {
        public string Name => "CombinedEyeLid";

        public float GetWeight(UnifiedTrackingData data) => (data.Eye.Left.Openness + data.Eye.Right.Openness) / 2;
    }
}