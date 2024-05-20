namespace Hypernex.Game.Avatar.FingerInterfacing
{
    public interface IFingerCurler
    {
        public Hand Hand { get; }
        public float ThumbCurl { get; }
        public float IndexCurl { get; }
        public float MiddleCurl { get; }
        public float RingCurl { get; }
        public float PinkyCurl { get; }

        public bool IsCurled(float amount) => amount > FingerCalibration.CurlAmount;
    }

    public enum Hand
    {
        Left,
        Right
    }
}