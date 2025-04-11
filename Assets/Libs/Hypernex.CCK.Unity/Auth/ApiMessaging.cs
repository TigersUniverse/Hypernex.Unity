using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hypernex.CCK.Unity.Auth;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;

namespace Hypernex.CCK.Auth
{
    public static class ApiMessaging
    {
        public static async Task<bool> IsValidAsset(this UserAuth auth, string id)
        {
            if (!auth.IsAuth) return false;
            TaskCompletionSource<bool> valid = new TaskCompletionSource<bool>();
            bool isAvatar = id.Contains("avatar_");
            if (isAvatar)
                auth.hypernexObject.GetAvatarMeta(
                    result => valid.SetResult(result.success && result.result.Meta.OwnerId == auth.user.Id), id);
            else
                auth.hypernexObject.GetWorldMeta(
                    result => valid.SetResult(result.success && result.result.Meta.OwnerId == auth.user.Id), id);
            return await valid.Task;
        }

        public static async Task<List<CDNServer>> GetCDNs(this UserAuth auth)
        {
            TaskCompletionSource<List<CDNServer>> results = new TaskCompletionSource<List<CDNServer>>();
            auth.hypernexObject.GetCDNs(r =>
                results.SetResult(r.success ? r.result.Servers : new List<CDNServer>()));
            return await results.Task;
        }

        public static async Task<CallbackResult<MetaCallback<AvatarMeta>>> GetAvatarMeta(this UserAuth auth, string id)
        {
            TaskCompletionSource<CallbackResult<MetaCallback<AvatarMeta>>> result =
                new TaskCompletionSource<CallbackResult<MetaCallback<AvatarMeta>>>();
            auth.hypernexObject.GetAvatarMeta(r => result.SetResult(r), id);
            return await result.Task;
        }
        
        public static async Task<CallbackResult<MetaCallback<WorldMeta>>> GetWorldMeta(this UserAuth auth, string id)
        {
            TaskCompletionSource<CallbackResult<MetaCallback<WorldMeta>>> result =
                new TaskCompletionSource<CallbackResult<MetaCallback<WorldMeta>>>();
            auth.hypernexObject.GetWorldMeta(r => result.SetResult(r), id);
            return await result.Task;
        }

        public static async Task<CallbackResult<UploadResult>> Upload(this UserAuth auth, FileStream fileStream, CDNServer cdnServer,
            Action<int> progress = null)
        {
            TaskCompletionSource<CallbackResult<UploadResult>> result = new TaskCompletionSource<CallbackResult<UploadResult>>();
            auth.hypernexObject.Upload(r => result.SetResult(r), auth.user, auth.token, fileStream,
                cdnServer, progress);
            return await result.Task;
        }

        public static async Task<CallbackResult<AvatarUpdateResult>> UpdateAvatar(this UserAuth auth,
            UploadResult uploadResult, AvatarMeta avatarMeta)
        {
            TaskCompletionSource<CallbackResult<AvatarUpdateResult>> result = new TaskCompletionSource<CallbackResult<AvatarUpdateResult>>();
            auth.hypernexObject.UpdateAvatar(r => result.SetResult(r), auth.user, auth.token, uploadResult.UploadData,
                avatarMeta);
            return await result.Task;
        }
        
        public static async Task<CallbackResult<EmptyResult>> UpdateAvatar(this UserAuth auth, AvatarMeta avatarMeta)
        {
            TaskCompletionSource<CallbackResult<EmptyResult>> result = new TaskCompletionSource<CallbackResult<EmptyResult>>();
            auth.hypernexObject.UpdateAvatar(r => result.SetResult(r), auth.user, auth.token, avatarMeta);
            return await result.Task;
        }
        
        public static async Task<CallbackResult<WorldUpdateResult>> UpdateWorld(this UserAuth auth,
            UploadResult uploadResult, WorldMeta worldMeta)
        {
            TaskCompletionSource<CallbackResult<WorldUpdateResult>> result = new TaskCompletionSource<CallbackResult<WorldUpdateResult>>();
            auth.hypernexObject.UpdateWorld(r => result.SetResult(r), auth.user, auth.token, uploadResult.UploadData,
                worldMeta);
            return await result.Task;
        }
        
        public static async Task<CallbackResult<EmptyResult>> UpdateWorld(this UserAuth auth, WorldMeta worldMeta)
        {
            TaskCompletionSource<CallbackResult<EmptyResult>> result = new TaskCompletionSource<CallbackResult<EmptyResult>>();
            auth.hypernexObject.UpdateWorld(r => result.SetResult(r), auth.user, auth.token, worldMeta);
            return await result.Task;
        }
    }
}