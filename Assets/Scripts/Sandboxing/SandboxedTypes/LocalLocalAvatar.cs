using Hypernex.Game;
using Hypernex.Networking.Messages.Data;
using Hypernex.Tools;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    // I know this class name sounds stupid, but it is the LocalAvatar for Local scripts
    public static class LocalLocalAvatar
    {
        public static Item GetAvatarObject(HumanBodyBones humanBodyBones)
        {
            if (LocalPlayer.Instance == null)
                return null;
            Transform bone = LocalPlayer.Instance.GetBoneFromHumanoid(humanBodyBones);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public static Item GetAvatarObjectByPath(string path)
        {
            if (LocalPlayer.Instance == null || LocalPlayer.Instance.avatar == null)
                return null;
            Transform bone = LocalPlayer.Instance.avatar.transform.Find(path);
            if (bone == null)
                return null;
            return new Item(bone);
        }

        public static void TeleportTo(float3 position)
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.transform.position = NetworkConversionTools.float3ToVector3(position);
        }

        public static void Rotate(float4 rotation)
        {
            if (LocalPlayer.Instance == null)
                return;
            LocalPlayer.Instance.transform.rotation = NetworkConversionTools.float4ToQuaternion(rotation);
        }
    }
}