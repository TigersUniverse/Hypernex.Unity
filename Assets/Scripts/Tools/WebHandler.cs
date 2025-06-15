using System;
using Hypernex.Configuration;
using Hypernex.Game;
using Hypernex.Player;
using Hypernex.UI.Abstraction;
using Hypernex.UI.Components;
using HypernexSharp.APIObjects;

namespace Hypernex.Tools
{
    public static class WebHandler
    {
        public static void HandleLaunchArgs(string[] args, CreateInstanceWindow createInstanceTemplate)
        {
            bool didWorld = false;
            bool didAvatar = false;
            foreach (string arg in args)
            {
                if(arg.Contains("hypernex://"))
                {
                    string s = arg.Split("hypernex://")[1];
                    s = s.TrimEnd('/');
                    string ss = s.Split('_')[0];
                    switch (ss)
                    {
                        case "world" when didWorld:
                            continue;
                        case "world":
                            didWorld = true;
                            // Go to Create Instance
                            WorldRender.GetWorldMeta(s, meta =>
                            {
                                if (meta == null)
                                    return;
                                createInstanceTemplate.Apply(meta);
                            });
                            break;
                        case "avatar" when didAvatar:
                            continue;
                        case "avatar":
                            didAvatar = true;
                            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null &&
                                GameInstance.FocusedInstance.World.LockAvatarSwitching)
                                return;
                            APIPlayer.APIObject.GetAvatarMeta(r =>
                            {
                                if(!r.success)
                                    return;
                                // No perms
                                if (r.result.Meta.Publicity == AvatarPublicity.OwnerOnly &&
                                    r.result.Meta.OwnerId != APIPlayer.APIUser.Id)
                                    return;
                                QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                                {
                                    ConfigManager.SelectedConfigUser.CurrentAvatar = r.result.Meta.Id;
                                    if(LocalPlayer.Instance != null)
                                    {
                                        LocalPlayer.Instance.LoadAvatar();
                                        OverlayNotification.AddMessageToQueue(new MessageMeta(MessageUrgency.Info, MessageButtons.None)
                                        {
                                            Header = "Equipping Avatar",
                                            Description = $"Equipping Avatar {r.result.Meta.Name}, Please Wait."
                                        });
                                    }
                                    ConfigManager.SaveConfigToFile();
                                }));
                            }, s);
                            break;
                    }
                }
            }
        }
    }
}