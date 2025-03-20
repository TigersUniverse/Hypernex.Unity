using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hypernex.CCK.Unity;
using Hypernex.Game.Audio;
using Hypernex.Game.Avatar;
using Hypernex.Networking.Messages.Data;
using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
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

        internal static int GetWorldPlayerCount(this List<SafeInstance> instances)
        {
            int i = 0;
            foreach (SafeInstance safeInstance in instances)
                i += safeInstance.ConnectedUsers.Count;
            return i;
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
                    continue;
                }
                Object.DestroyImmediate(child);
            }
        }

        internal static void AddChild(this Transform parent, Transform t) => t.SetParent(parent);

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

        internal static AudioSource PrepareNetVoice(this GameObject gameObject)
        {
            AudioSource audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            BufferAudioSource bufferAudioSource = gameObject.GetComponent<BufferAudioSource>();
            if (bufferAudioSource == null) gameObject.AddComponent<BufferAudioSource>();
            audioSource.spatialize = true;
            audioSource.spatializePostEffects = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0;
            audioSource.maxDistance = 10;
            audioSource.outputAudioMixerGroup = Init.Instance.VoiceGroup;
            audioSource.dopplerLevel = 0;
            return audioSource;
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

        internal static string CastIf24Hour(this string longDateTime, bool is24Hour)
        {
            char[] chars = longDateTime.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c == 'h' || c == 'H')
                    chars[i] = is24Hour ? 'H' : 'h';
                if (c == 't')
                    chars[i] = is24Hour ? ' ' : 't';
            }
            string format = new string(chars);
            if (!is24Hour && !format.Contains("tt"))
                format += " tt";
            return format;
        }

        private static List<VolumeProfile> lastVolumeProfiles = new();

        private static bool IsVolumeProfileCollectionEqual(this VolumeProfile[] b)
        {
            if (lastVolumeProfiles.Count != b.Length) return false;
            for (int i = 0; i < lastVolumeProfiles.Count; i++)
            {
                VolumeProfile itemA = lastVolumeProfiles.ElementAt(i);
                VolumeProfile itemB = b[i];
                if (itemA != itemB) return false;
            }
            return true;
        }

        public static void SelectVolume(this Volume[] volumes, bool force = false)
        {
            if(volumes == null) return;
            VolumeProfile[] volumeProfilesArray = volumes.Where(x =>
            {
                GameObject gameObject = x.gameObject;
                return gameObject.activeSelf && x.enabled;
            }).Select(x => x.profile).ToArray();
            if(volumeProfilesArray.IsVolumeProfileCollectionEqual() && !force) return;
            lastVolumeProfiles.Clear();
            lastVolumeProfiles.AddRange(volumeProfilesArray);
            VolumeManager.instance.SetCustomDefaultProfiles(lastVolumeProfiles);
        }

        public static string GetUserDisplayName(this User user)
        {
            if (!string.IsNullOrEmpty(user.Bio.DisplayName))
                return user.Bio.DisplayName + " <size=15>@" + user.Username + "</size>";
            return "@" + user.Username;
        }

        public static void RenderNetImage(this RawImage rawImage, string url, Texture2D fallback = null)
        {
            if (!string.IsNullOrEmpty(url))
                DownloadTools.DownloadBytes(url,
                    bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            if(ComponentTools.HasComponent<GifRenderer>(rawImage.gameObject))
                                Object.Destroy(rawImage.GetComponent<GifRenderer>());
                            GifRenderer gifRenderer = rawImage.gameObject.AddComponent<GifRenderer>();
                            gifRenderer.LoadGif(bytes);
                        }
                        else
                            rawImage.texture = ImageTools.BytesToTexture2D(url, bytes);
                    });
            else
                rawImage.texture = fallback;
        }

        internal class UserEqualityComparer : IEqualityComparer<User>
        {
            public static UserEqualityComparer Instance { get; } = new();

            public bool Equals(User x, User y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(User obj) => obj.GetHashCode();
        }
    }
}