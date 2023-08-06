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
        private AvatarCreator AvatarCreator;

        private Quaternion[] InitialThumbs = Array.Empty<Quaternion>();
        private Quaternion[] InitialIndex = Array.Empty<Quaternion>();
        private Quaternion[] InitialMiddle = Array.Empty<Quaternion>();
        private Quaternion[] InitialRing = Array.Empty<Quaternion>();
        private Quaternion[] InitialLittle = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRThumbs = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRIndex = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRMiddle = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRRing = Array.Empty<Quaternion>();
        internal static Quaternion[] InitialXRLittle = Array.Empty<Quaternion>();

        public FingerCalibration(AvatarCreator a)
        {
            AvatarCreator = a;
            for (int x = 0; x < 5; x++)
            {
                if(TryGetFingers(0, out Transform[] avatarBones))
                {
                    List<Quaternion> initialAvatarBones = new();
                    List<Quaternion> initialXRBones = new();
                    for (int y = 0; y < avatarBones.Length; y++)
                    {
                        int hl = avatarBones.Length / 2;
                        Transform t = avatarBones[y];
                        HandGetter h = hl > y + 1
                            ? LocalPlayer.Instance.LeftHandGetter
                            : LocalPlayer.Instance.RightHandGetter;
                        initialAvatarBones.Add(t.localRotation);
                        initialXRBones.Add(h.orientations[GetIndexFromFingerTransforms(x, y)]);
                    }
                    switch (x)
                    {
                        case 0:
                            InitialThumbs = initialAvatarBones.ToArray();
                            InitialXRThumbs = initialXRBones.ToArray();
                            break;
                        case 1:
                            InitialIndex = initialAvatarBones.ToArray();
                            InitialXRIndex = initialXRBones.ToArray();
                            break;
                        case 2:
                            InitialMiddle = initialAvatarBones.ToArray();
                            InitialXRMiddle = initialXRBones.ToArray();
                            break;
                        case 3:
                            InitialRing = initialAvatarBones.ToArray();
                            InitialXRRing = initialXRBones.ToArray();
                            break;
                        case 4:
                            InitialLittle = initialAvatarBones.ToArray();
                            InitialXRLittle = initialXRBones.ToArray();
                            break;
                    }
                }
            }
        }

        internal void LateUpdate()
        {
            for (int i = 0; i < 5; i++)
            {
                if(TryGetFingers(i, out Transform[] fingers))
                    ApplyFingerTracking(i, fingers);
            }
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
                            return 7;
                        case 1 or 3:
                            return 8;
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
                            return 10;
                        case 1 or 4:
                            return 11;
                        case 2 or 5:
                            return 12;
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
                            return 14;
                        case 1 or 4:
                            return 15;
                        case 2 or 5:
                            return 16;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
                case 3:
                {
                    // Middle
                    switch (i)
                    {
                        case 0 or 3:
                            return 18;
                        case 1 or 4:
                            return 19;
                        case 2 or 5:
                            return 20;
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
                Quaternion difference = Quaternion.Inverse(initialXRRotation) * xrRotation;
                avatarBone.localRotation = initialAvatarBoneRotation * difference;
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
                HandGetter h = ts.Length / 2 > i + 1
                    ? LocalPlayer.Instance.LeftHandGetter
                    : LocalPlayer.Instance.RightHandGetter;
                /*Quaternion r = Quaternion.Euler(h.orientations[x].eulerAngles);
                t.localRotation = r;*/
                TryCalibrate(finger, i, h.orientations[x], t);
                i++;
            }
        }
    }
}