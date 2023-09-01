using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.Game
{
    /// <summary>
    /// Calibrates Finger Rotations for Finger Tracking
    /// </summary>
    public class FingerCalibration
    {
        private const int UNKNOWN_GESTURE = 0;
        private const int FIST_GESTURE = 1;
        private const int OPEN_HAND_GESTURE = 2;
        private const int POINT_GESTURE = 3;
        private const int PEACE_GESTURE = 4;
        private const int OK_HAND_GESTURE = 5;
        private const int GUN_GESTURE = 6;
        private const int THUMBS_UP_GESTURE = 7;

        /// <summary>
        /// How much pressure is required to activate a curl
        /// </summary>
        public static float CurlAmount { get; set; } = 0.29f;
        
        private AvatarCreator AvatarCreator;

        private Quaternion[] InitialThumbs = Array.Empty<Quaternion>();
        private Quaternion[] InitialIndex = Array.Empty<Quaternion>();
        private Quaternion[] InitialMiddle = Array.Empty<Quaternion>();
        private Quaternion[] InitialRing = Array.Empty<Quaternion>();
        private Quaternion[] InitialLittle = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRThumbs = new Quaternion[4];
        internal static Quaternion[] InitialXRIndex = new Quaternion[6];
        internal static Quaternion[] InitialXRMiddle = new Quaternion[6];
        internal static Quaternion[] InitialXRRing = new Quaternion[6];
        internal static Quaternion[] InitialXRLittle = new Quaternion[6];

        public FingerCalibration(AvatarCreator a)
        {
            AvatarCreator = a;
            for (int x = 0; x < 5; x++)
            {
                if(TryGetFingers(x, out Transform[] avatarBones))
                {
                    List<Quaternion> initialAvatarBones = new();
                    //List<Quaternion> initialXRBones = new();
                    for (int y = 0; y < avatarBones.Length; y++)
                    {
                        Transform t = avatarBones[y];
                        if(t == null) continue;
                        initialAvatarBones.Add(t.localRotation);
                        //initialXRBones.Add(h.orientations[GetIndexFromFingerTransforms(x, y)]);
                    }
                    switch (x)
                    {
                        case 0:
                            InitialThumbs = initialAvatarBones.ToArray();
                            //InitialXRThumbs = initialXRBones.ToArray();
                            break;
                        case 1:
                            InitialIndex = initialAvatarBones.ToArray();
                            //InitialXRIndex = initialXRBones.ToArray();
                            break;
                        case 2:
                            InitialMiddle = initialAvatarBones.ToArray();
                            //InitialXRMiddle = initialXRBones.ToArray();
                            break;
                        case 3:
                            InitialRing = initialAvatarBones.ToArray();
                            //InitialXRRing = initialXRBones.ToArray();
                            break;
                        case 4:
                            InitialLittle = initialAvatarBones.ToArray();
                            //InitialXRLittle = initialXRBones.ToArray();
                            break;
                    }
                }
            }
        }

        internal void Update()
        {
            // Update Finger Gestures
            AvatarCreator.SetParameter("GestureLeft",
                GetGestureNumberFromHandGetter(LocalPlayer.Instance.LeftHandGetter));
            AvatarCreator.SetParameter("GestureRight",
                GetGestureNumberFromHandGetter(LocalPlayer.Instance.RightHandGetter));
        }

        internal void LateUpdate()
        {
            for (int i = 0; i < 5; i++)
            {
                if(TryGetFingers(i, out Transform[] fingers))
                    ApplyFingerTracking(i, fingers);
            }
        }

        private bool IsCurled(float amount) => amount > CurlAmount;

        private int GetGestureNumberFromHandGetter(HandGetter handGetter)
        {
            float thumb = handGetter.Curls[0];
            float index = handGetter.Curls[1];
            float middle = handGetter.Curls[2];
            float ring = handGetter.Curls[3];
            float little = handGetter.Curls[4];
            // 5
            if (IsCurled(thumb) && IsCurled(index) && IsCurled(middle) && IsCurled(ring) && IsCurled(little))
                return FIST_GESTURE;
            // 4
            if (IsCurled(index) && IsCurled(middle) && IsCurled(ring) && IsCurled(little))
                return THUMBS_UP_GESTURE;
            if (IsCurled(thumb) && IsCurled(middle) && IsCurled(ring) && IsCurled(little))
                return POINT_GESTURE;
            // 3
            if (IsCurled(middle) && IsCurled(ring) && IsCurled(little))
                return GUN_GESTURE;
            if (IsCurled(thumb) && IsCurled(ring) && IsCurled(middle))
                return PEACE_GESTURE;
            // 2
            if (IsCurled(thumb) && IsCurled(index))
                return OK_HAND_GESTURE;
            // 0
            if (!IsCurled(thumb) && !IsCurled(index) && !IsCurled(middle) && !IsCurled(ring) && !IsCurled(little))
                return OPEN_HAND_GESTURE;
            return UNKNOWN_GESTURE;
        }

        private int GetIndexFromFingerTransforms(int finger, int i)
        {
            switch (finger)
            {
                case 0:
                {
                    // Thumb
                    switch (i)
                    {
                        case 0 or 2:
                            return 3;
                        case 1 or 3:
                            return 4;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                case 1:
                {
                    // Index
                    switch (i)
                    {
                        case 0 or 3:
                            return 7;
                        case 1 or 4:
                            return 8;
                        case 2 or 5:
                            return 9;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                case 2:
                {
                    // Middle
                    switch (i)
                    {
                        case 0 or 3:
                            return 12;
                        case 1 or 4:
                            return 13;
                        case 2 or 5:
                            return 14;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                case 3:
                {
                    // Ring
                    switch (i)
                    {
                        case 0 or 3:
                            return 17;
                        case 1 or 4:
                            return 18;
                        case 2 or 5:
                            return 19;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                case 4:
                {
                    // Little
                    switch (i)
                    {
                        case 0 or 3:
                            return 22;
                        case 1 or 4:
                            return 23;
                        case 2 or 5:
                            return 24;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        private bool TryGetFingers(int finger, out Transform[] t)
        {
            switch (finger)
            {
                case 0:
                {
                    // Apply the Proximal to 22, and the Distal to 23. Skip 24; no tip.
                    try
                    {
                        
                        List<Transform> ts = new();
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftThumbProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftThumbDistal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightThumbProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightThumbDistal));
                        t = ts.ToArray();
                        return true;
                    }
                    catch (Exception)
                    {
                        t = Array.Empty<Transform>();
                        return false;
                    }
                }
                case 1:
                {
                    // Apply the Proximal to 25, and the Intermediate to 26, and Distal to 27. Skip 28; no tip.
                    try
                    {
                        List<Transform> ts = new();
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftIndexProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftIndexIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftIndexDistal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightIndexProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightIndexIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightIndexDistal));
                        t = ts.ToArray();
                        return true;
                    }
                    catch (Exception)
                    {
                        t = Array.Empty<Transform>();
                        return false;
                    }
                }
                case 2:
                {
                    // Apply the Proximal to 29, and the Intermediate to 30, and Distal to 31. Skip 32; no tip.
                    try
                    {
                        List<Transform> ts = new();
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftMiddleProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftMiddleIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftMiddleDistal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightMiddleProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightMiddleIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightMiddleDistal));
                        t = ts.ToArray();
                        return true;
                    }
                    catch (Exception)
                    {
                        t = Array.Empty<Transform>();
                        return false;
                    }
                }
                case 3:
                {
                    // Apply the Proximal to 33, and the Intermediate to 34, and Distal to 35. Skip 36; no tip.
                    try
                    {
                        List<Transform> ts = new();
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftRingProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftRingIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftRingDistal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightRingProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightRingIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightRingDistal));
                        t = ts.ToArray();
                        return true;
                    }
                    catch (Exception)
                    {
                        t = Array.Empty<Transform>();
                        return false;
                    }
                }
                case 4:
                {
                    // Apply the Proximal to 37, and the Intermediate to 38, and Distal to 39. Skip 40; no tip.
                    try
                    {
                        List<Transform> ts = new();
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftLittleProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftLittleIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.LeftLittleDistal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightLittleProximal));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightLittleIntermediate));
                        ts.Add(AvatarCreator.GetBoneFromHumanoid(HumanBodyBones.RightLittleDistal));
                        t = ts.ToArray();
                        return true;
                    }
                    catch (Exception)
                    {
                        t = Array.Empty<Transform>();
                        return false;
                    }
                }
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        private bool TryCalibrate(int finger, int i, Quaternion xrRotation, Transform avatarBone)
        {
            try
            {
                Quaternion initialAvatarBoneRotation;
                Quaternion initialXRRotation;
                switch (finger)
                {
                    case 0:
                        initialAvatarBoneRotation = InitialThumbs[i];
                        initialXRRotation = InitialXRThumbs[i];
                        break;
                    case 1:
                        initialAvatarBoneRotation = InitialIndex[i];
                        initialXRRotation = InitialXRIndex[i];
                        break;
                    case 2:
                        initialAvatarBoneRotation = InitialMiddle[i];
                        initialXRRotation = InitialXRMiddle[i];
                        break;
                    case 3:
                        initialAvatarBoneRotation = InitialRing[i];
                        initialXRRotation = InitialXRRing[i];
                        break;
                    case 4:
                        initialAvatarBoneRotation = InitialLittle[i];
                        initialXRRotation = InitialXRLittle[i];
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
                //Quaternion difference = Quaternion.Inverse(initialXRRotation) * xrRotation;
                //avatarBone.localRotation = xrRotation; //initialAvatarBoneRotation * difference;*/
                avatarBone.localRotation = initialAvatarBoneRotation * (Quaternion.Inverse(initialXRRotation) * xrRotation);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ApplyFingerTracking(int finger, Transform[] ts)
        {
            int i = 0;
            foreach (Transform t in ts)
            {
                int x = GetIndexFromFingerTransforms(finger, i);
                HandGetter h = ts.Length / 2 > i
                    ? LocalPlayer.Instance.LeftHandGetter
                    : LocalPlayer.Instance.RightHandGetter;
                TryCalibrate(finger, i, h.joints[x].rotation, t);
                i++;
            }
        }
    }
}