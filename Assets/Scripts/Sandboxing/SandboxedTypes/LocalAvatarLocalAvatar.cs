using Hypernex.Game;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes
{
    // I know this class name sounds stupid, but this is the LocalAvatar for LocalAvatar scripts
    public static class LocalAvatarLocalAvatar
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
    }
}