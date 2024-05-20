using System.Collections.Generic;
using System.Linq;
using Hypernex.Game.Avatar.FingerInterfacing;

namespace Hypernex.Game.Avatar
{
    /// <summary>
    /// Calibrates Finger Rotations for Finger Tracking
    /// </summary>
    public class FingerCalibration
    {
        public static IGestureIdentifier DefaultGestures => GestureIdentifiers.First();

        public static readonly List<IGestureIdentifier> GestureIdentifiers = new()
        {
            new HypernexGesture(),
            new CGesture(),
            new VGesture()
        };

        public static IGestureIdentifier GetGestureIdentifierFromName(string name)
        {
            IGestureIdentifier gestureIdentifier = null;
            foreach (IGestureIdentifier identifier in GestureIdentifiers)
            {
                if (identifier.Name.ToLower() == name.ToLower())
                {
                    gestureIdentifier = identifier;
                    break;
                }
            }
            return gestureIdentifier;
        }

        public static int GetGestureIndex(IGestureIdentifier identifier)
        {
            for (int i = 0; i < GestureIdentifiers.Count; i++)
            {
                IGestureIdentifier gestureIdentifier = GestureIdentifiers[i];
                if (gestureIdentifier.Name == identifier.Name)
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// How much pressure is required to activate a curl
        /// </summary>
        public static float CurlAmount { get; set; } = 0.29f;

        public const string LEFT_GESTURE_NAME = "GestureLeft";
        public const string RIGHT_GESTURE_NAME = "GestureRight";

        public const string LEFT_THUMB_CURL = "LeftThumbCurl";
        public const string LEFT_INDEX_CURL = "LeftIndexCurl";
        public const string LEFT_MIDDLE_CURL = "LeftMiddleCurl";
        public const string LEFT_RING_CURL = "LeftRingCurl";
        public const string LEFT_PINKY_CURL = "LeftPinkyCurl";
        
        public const string RIGHT_THUMB_CURL = "RightThumbCurl";
        public const string RIGHT_INDEX_CURL = "RightIndexCurl";
        public const string RIGHT_MIDDLE_CURL = "RightMiddleCurl";
        public const string RIGHT_RING_CURL = "RightRingCurl";
        public const string RIGHT_PINKY_CURL = "RightPinkyCurl";
        
        private AvatarCreator AvatarCreator;

        public FingerCalibration(AvatarCreator a) => AvatarCreator = a;

        internal void Update(LocalPlayer localPlayer, IFingerCurler leftFingers, IFingerCurler rightFingers)
        {
            // Update Finger Gestures
            AvatarCreator.SetParameter(LEFT_GESTURE_NAME,
                GetGestureNumberFromHandGetter(localPlayer.GetLeftHandCurler(), localPlayer.GestureIdentifier), null,
                true);
            AvatarCreator.SetParameter(RIGHT_GESTURE_NAME,
                GetGestureNumberFromHandGetter(localPlayer.GetRightHandCurler(), localPlayer.GestureIdentifier), null,
                true);
            // Update Finger Curls
            AvatarCreator.SetParameter(LEFT_THUMB_CURL, leftFingers.ThumbCurl, null, true, true);
            AvatarCreator.SetParameter(LEFT_INDEX_CURL, leftFingers.IndexCurl, null, true, true);
            AvatarCreator.SetParameter(LEFT_MIDDLE_CURL, leftFingers.MiddleCurl, null, true, true);
            AvatarCreator.SetParameter(LEFT_RING_CURL, leftFingers.RingCurl, null, true, true);
            AvatarCreator.SetParameter(LEFT_PINKY_CURL, leftFingers.PinkyCurl, null, true, true);
            AvatarCreator.SetParameter(RIGHT_THUMB_CURL, rightFingers.ThumbCurl, null, true, true);
            AvatarCreator.SetParameter(RIGHT_INDEX_CURL, rightFingers.IndexCurl, null, true, true);
            AvatarCreator.SetParameter(RIGHT_MIDDLE_CURL, rightFingers.MiddleCurl, null, true, true);
            AvatarCreator.SetParameter(RIGHT_RING_CURL, rightFingers.RingCurl, null, true, true);
            AvatarCreator.SetParameter(RIGHT_PINKY_CURL, rightFingers.PinkyCurl, null, true, true);
        }

        internal static int GetGestureNumberFromHandGetter(IFingerCurler fingerCurler, IGestureIdentifier gestureIdentifier = null)
        {
            if (gestureIdentifier == null) gestureIdentifier = DefaultGestures;
            float thumb = fingerCurler.ThumbCurl;
            float index = fingerCurler.IndexCurl;
            float middle = fingerCurler.MiddleCurl;
            float ring = fingerCurler.RingCurl;
            float little = fingerCurler.PinkyCurl;
            // 5
            if (fingerCurler.IsCurled(thumb) && fingerCurler.IsCurled(index) && fingerCurler.IsCurled(middle) &&
                fingerCurler.IsCurled(ring) && fingerCurler.IsCurled(little))
                return gestureIdentifier.Fist;
            // 4
            if (fingerCurler.IsCurled(index) && fingerCurler.IsCurled(middle) && fingerCurler.IsCurled(ring) &&
                fingerCurler.IsCurled(little) && !fingerCurler.IsCurled(thumb))
                return gestureIdentifier.ThumbsUp;
            if (fingerCurler.IsCurled(thumb) && fingerCurler.IsCurled(middle) && fingerCurler.IsCurled(ring) &&
                fingerCurler.IsCurled(little) && !fingerCurler.IsCurled(index))
                return gestureIdentifier.Point;
            // 3
            if (fingerCurler.IsCurled(middle) && fingerCurler.IsCurled(ring) && fingerCurler.IsCurled(little) &&
                !fingerCurler.IsCurled(index) && !fingerCurler.IsCurled(thumb))
                return gestureIdentifier.Gun;
            if (fingerCurler.IsCurled(thumb) && fingerCurler.IsCurled(ring) && fingerCurler.IsCurled(little) &&
                !fingerCurler.IsCurled(index) && !fingerCurler.IsCurled(middle))
                return gestureIdentifier.Peace;
            // 2
            if (fingerCurler.IsCurled(thumb) && fingerCurler.IsCurled(index) && !fingerCurler.IsCurled(middle) &&
                !fingerCurler.IsCurled(ring) && !fingerCurler.IsCurled(little))
                return gestureIdentifier.OkHand;
            if (fingerCurler.IsCurled(middle) && fingerCurler.IsCurled(ring) && !fingerCurler.IsCurled(thumb) &&
                !fingerCurler.IsCurled(index) && !fingerCurler.IsCurled(little))
                return gestureIdentifier.RockAndRoll;
            // 0
            if (!fingerCurler.IsCurled(thumb) && !fingerCurler.IsCurled(index) && !fingerCurler.IsCurled(middle) &&
                !fingerCurler.IsCurled(ring) && !fingerCurler.IsCurled(little))
                return gestureIdentifier.OpenHand;
            return gestureIdentifier.Unknown;
        }
    }
}