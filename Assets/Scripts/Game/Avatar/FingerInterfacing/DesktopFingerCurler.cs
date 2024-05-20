using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hypernex.Game.Avatar.FingerInterfacing
{
    public static class DesktopFingerCurler
    {
        private static PropertyInfo[] GestureProperties;
        private static IGestureIdentifier LastGestureIdentifier;

        private static void Refresh(IGestureIdentifier gestureIdentifier)
        {
            GestureProperties = gestureIdentifier.GetType().GetProperties().Where(x => x.PropertyType == typeof(int))
                .ToArray();
            LastGestureIdentifier = gestureIdentifier;
        }

        internal static void Update(ref Left left, ref Right right, IGestureIdentifier gestureIdentifier)
        {
            if(GestureProperties == null || LastGestureIdentifier != gestureIdentifier) Refresh(gestureIdentifier);
            left.Update();
            right.Update();
        }
        
        public class Left : IFingerCurler
        {
            public Hand Hand => Hand.Left;
            public float ThumbCurl { get; private set; }
            public float IndexCurl { get; private set; }
            public float MiddleCurl { get; private set; }
            public float RingCurl { get; private set; }
            public float PinkyCurl { get; private set; }

            internal void Update()
            {
                if (!Input.GetKey(KeyCode.LeftShift)) return;
                for (int i = 0; i < GestureProperties!.Length; i++)
                {
                    PropertyInfo gestureProperty = GestureProperties[i];
                    KeyCode keyCode = Enum.Parse<KeyCode>("Alpha" + i);
                    if (Input.GetKeyDown(keyCode))
                    {
                        switch (gestureProperty.Name)
                        {
                            case "Fist":
                                ThumbCurl = 1f;
                                IndexCurl = 1f;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "OpenHand":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 0;
                                RingCurl = 0;
                                PinkyCurl = 0;
                                break;
                            case "Point":
                                ThumbCurl = 1f;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "Peace":
                                ThumbCurl = 1f;
                                IndexCurl = 0;
                                MiddleCurl = 0;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "OkHand":
                                ThumbCurl = 1f;
                                IndexCurl = 1f;
                                MiddleCurl = 0;
                                RingCurl = 0;
                                PinkyCurl = 0;
                                break;
                            case "RockAndRoll":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 0;
                                break;
                            case "Gun":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "ThumbsUp":
                                ThumbCurl = 0;
                                IndexCurl = 1f;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                        }
                    }
                }
            }
        }
        
        public class Right : IFingerCurler
        {
            public Hand Hand => Hand.Right;
            public float ThumbCurl { get; private set; }
            public float IndexCurl { get; private set; }
            public float MiddleCurl { get; private set; }
            public float RingCurl { get; private set; }
            public float PinkyCurl { get; private set; }
            
            internal void Update()
            {
                if (!Input.GetKey(KeyCode.RightShift)) return;
                for (int i = 0; i < GestureProperties!.Length; i++)
                {
                    PropertyInfo gestureProperty = GestureProperties[i];
                    KeyCode keyCode = Enum.Parse<KeyCode>("Alpha" + i);
                    if (Input.GetKeyDown(keyCode))
                    {
                        switch (gestureProperty.Name)
                        {
                            case "Fist":
                                ThumbCurl = 1f;
                                IndexCurl = 1f;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "OpenHand":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 0;
                                RingCurl = 0;
                                PinkyCurl = 0;
                                break;
                            case "Point":
                                ThumbCurl = 1f;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "Peace":
                                ThumbCurl = 1f;
                                IndexCurl = 0;
                                MiddleCurl = 0;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "OkHand":
                                ThumbCurl = 1f;
                                IndexCurl = 1f;
                                MiddleCurl = 0;
                                RingCurl = 0;
                                PinkyCurl = 0;
                                break;
                            case "RockAndRoll":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 0;
                                break;
                            case "Gun":
                                ThumbCurl = 0;
                                IndexCurl = 0;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                            case "ThumbsUp":
                                ThumbCurl = 0;
                                IndexCurl = 1f;
                                MiddleCurl = 1f;
                                RingCurl = 1f;
                                PinkyCurl = 1f;
                                break;
                        }
                    }
                }
            }
        }
    }
}