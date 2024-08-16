using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Game.Avatar;
using Hypernex.Networking.Messages.Data;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Hypernex.Tools
{
    public static class Extensions
    {
        internal static NetworkedObject GetNetworkTransform(this Transform transform, Transform root = null, bool global = true)
        {
            NetworkedObject networkedObject = new NetworkedObject
            {
                IgnoreObjectLocation = false,
                ObjectLocation = AnimationUtility.CalculateTransformPath(transform,
                    root == null ? AnimationUtility.GetRootOfChild(transform) : root)
            };
            networkedObject.Position = global
                ? NetworkConversionTools.Vector3Tofloat3(transform.position)
                : NetworkConversionTools.Vector3Tofloat3(transform.localPosition);
            networkedObject.Rotation = global
                ? NetworkConversionTools.QuaternionTofloat4(new Quaternion(transform.eulerAngles.x,
                    transform.eulerAngles.y, transform.eulerAngles.z, 0))
                : new float4(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z,
                    0);
            networkedObject.Size = NetworkConversionTools.Vector3Tofloat3(transform.localScale);
            return networkedObject;
        }

        internal static void Apply(this NetworkedObject networkedObject, SmoothTransform transform, bool global = true)
        {
            transform.Position = NetworkConversionTools.float3ToVector3(networkedObject.Position);
            transform.Rotation = Quaternion.Euler(new Vector3(networkedObject.Rotation.x,
                networkedObject.Rotation.y, networkedObject.Rotation.z));
            transform.Scale = NetworkConversionTools.float3ToVector3(networkedObject.Size);
        }

        internal static bool TryFind<T>(this List<T> list, Predicate<T> match, out T found)
        {
            foreach (T t in list)
            {
                if (match.Invoke(t))
                {
                    found = t;
                    return true;
                }
            }
            found = default;
            return false;
        }
        
        internal static void ClearChildren(this Transform t, bool immediate = false)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                GameObject child = t.GetChild(i).gameObject;
                if(!immediate)
                {
                    Object.Destroy(child);
                    return;
                }
                Object.DestroyImmediate(child);
            }
        }

        internal static RotationOffsetDriver GetOffsetRotator(this Transform t, Transform root) => new (t, root);

        internal static RotationOffsetDriver GetRotatorFromAvatarBone(this AvatarCreator avatarCreator,
            HumanBodyBones humanBodyBones)
        {
            Transform bone = avatarCreator.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null) return null;
            return new RotationOffsetDriver(bone, avatarCreator.Avatar.transform);
        }
        
        internal static bool IsDifferentByRange(this Vector3 current, Vector3 last, float value)
        {
            if (last == current)
                return false;
            float v = Vector3.Distance(last, current);
            return v > value;
        }

        internal static bool IsDifferentByRange(this Quaternion current, Quaternion last, float value)
        {
            if (last == current)
                return false;
            float angle = Quaternion.Angle(current, last);
            return angle > value;
        }

        /// <summary>
        /// Overrides a current VideoPlayer with the described type.
        /// </summary>
        /// <param name="descriptor">The descriptor</param>
        /// <param name="videoPlayerType">The type of IVideoPlayer. Requires a one-parameter constructor (input VideoPlayerDescriptor)</param>
        public static IVideoPlayer Replace(this VideoPlayerDescriptor descriptor, Type videoPlayerType)
        {
            if (descriptor.CurrentVideoPlayer != null)
                descriptor.CurrentVideoPlayer.Dispose();
            IVideoPlayer videoPlayer = (IVideoPlayer) Activator.CreateInstance(videoPlayerType, new object[1]{descriptor});
            descriptor.CurrentVideoPlayer = videoPlayer;
            return videoPlayer;
        }

        public static void SelectVolume(this Volume[] volumes)
        {
            if(volumes == null) return;
            VolumeManager.instance.SetCustomDefaultProfiles(volumes.Where(x =>
            {
                GameObject gameObject = x.gameObject;
                if (gameObject == null) return false;
                return gameObject.activeSelf && x.enabled;
            }).Select(x => x.profile).ToList());
        }
    }
}