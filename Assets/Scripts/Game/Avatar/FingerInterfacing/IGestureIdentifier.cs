namespace Hypernex.Game.Avatar.FingerInterfacing
{
    public interface IGestureIdentifier
    {
        string Name { get; }
        int Unknown { get; }
        int Fist { get; }
        int OpenHand { get; }
        int Point { get; }
        int Peace { get; }
        int OkHand { get; }
        int RockAndRoll { get; }
        int Gun { get; }
        int ThumbsUp { get; }
    }
}