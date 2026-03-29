using System.Collections.Generic;
using System.Linq;
using HypernexSharp.APIObjects;

namespace Hypernex.Tools
{
    public static class PlatformExtensions
    {
        public static AvatarMeta[] FilterBySupportedPlatform(this AvatarMeta[] avatarMetas)
        {
            List<AvatarMeta> newAvatarMetas = new List<AvatarMeta>();
            BuildPlatform b = AssetBundleTools.Platform;
            foreach (AvatarMeta avatarMeta in avatarMetas)
            {
                if(avatarMeta.Builds.Count(x => x.BuildPlatform == b) <= 0) continue;
                newAvatarMetas.Add(avatarMeta);
            }
            return newAvatarMetas.ToArray();
        }
        
        public static void FilterBySupportedPlatform(this List<AvatarMeta> avatarMetas)
        {
            BuildPlatform b = AssetBundleTools.Platform;
            avatarMetas.RemoveAll(x => x.Builds.Count(y => y.BuildPlatform == b) <= 0);
        }
        
        public static bool IsSupportedOnActivePlatform(this AvatarMeta avatarMeta)
        {
            BuildPlatform b = AssetBundleTools.Platform;
            return avatarMeta.Builds.Count(x => x.BuildPlatform == b) > 0;
        }
        
        public static WorldMeta[] FilterBySupportedPlatform(this WorldMeta[] worldMetas)
        {
            List<WorldMeta> newWorldMetas = new List<WorldMeta>();
            BuildPlatform b = AssetBundleTools.Platform;
            foreach (WorldMeta worldMeta in worldMetas)
            {
                if(worldMeta.Builds.Count(x => x.BuildPlatform == b) <= 0) continue;
                newWorldMetas.Add(worldMeta);
            }
            return newWorldMetas.ToArray();
        }
        
        public static void FilterBySupportedPlatform(this List<WorldMeta> worldMetas)
        {
            BuildPlatform b = AssetBundleTools.Platform;
            worldMetas.RemoveAll(x => x.Builds.Count(y => y.BuildPlatform == b) <= 0);
        }

        public static bool IsSupportedOnActivePlatform(this WorldMeta worldMeta)
        {
            BuildPlatform b = AssetBundleTools.Platform;
            return worldMeta.Builds.Count(x => x.BuildPlatform == b) > 0;
        }
    }
}