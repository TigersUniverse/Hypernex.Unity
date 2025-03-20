using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI.Abstraction
{
    public class WorldRender : UIRender, IRender<WorldMeta>
    {
        private static List<WorldMeta> CachedWorldMeta = new();
        
        public TMP_Text WorldName;
        public RawImage Banner;
        public TMP_Text WorldCreator;
        public TMP_Text DescriptionText;
        public Button NextIcon;
        public Button PreviousIcon;
        public TMP_Text PlayerCount;
        internal WorldMeta meta;
        
        private List<(Texture2D, byte[])> Icons = new();
        private int currentIndex;
        
        public static void GetWorldMeta(string worldId, Action<WorldMeta> callback)
        {
            if (CachedWorldMeta.Count(x => x.Id == worldId) > 0)
            {
                WorldMeta worldMeta = CachedWorldMeta.First(x => x.Id == worldId);
                callback.Invoke(worldMeta);
                return;
            }
            APIPlayer.APIObject.GetWorldMeta(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        CachedWorldMeta.Add(result.result.Meta);
                        callback.Invoke(result.result.Meta);
                    }));
                else
                    Logger.CurrentLogger.Error("Failed to get WorldMeta for " + worldId);
            }, worldId);
        }
        
        private static void GetAllInstanceHosts(Action<List<(SafeInstance, User)>> callback, List<SafeInstance> instances, List<(SafeInstance, User)> c = null)
        {
            if (instances.Count <= 0)
            {
                callback.Invoke(new List<(SafeInstance, User)>());
                return;
            }
            List<(SafeInstance, User)> temp;
            if (c == null)
                temp = new List<(SafeInstance, User)>();
            else
                temp = c;
            SafeInstance sharedInstance = instances.ElementAt(0);
            APIPlayer.APIObject.GetUser(result =>
            {
                if (result.success)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                        temp.Add((sharedInstance, result.result.UserData))));
                instances.Remove(sharedInstance);
                if(instances.Count > 0)
                    QuickInvoke.InvokeActionOnMainThread(new Action(() => GetAllInstanceHosts(callback, instances, temp)));
                else
                    QuickInvoke.InvokeActionOnMainThread(callback, temp);
            }, sharedInstance.InstanceCreatorId, isUserId: true);
        }

        public static void GetWorldInstances(Action<List<(SafeInstance, User)>> callback, WorldMeta worldMeta)
        {
            List<SafeInstance> safeInstances = new List<SafeInstance>();
            APIPlayer.APIObject.GetPublicInstancesOfWorld(instanceResults =>
            {
                if (instanceResults.success)
                {
                    foreach (SafeInstance resultSafeInstance in instanceResults.result.SafeInstances)
                    {
                        if (safeInstances.Count(x =>
                                x.GameServerId == resultSafeInstance.GameServerId &&
                                x.InstanceId == resultSafeInstance.InstanceId) <= 0)
                            safeInstances.Add(resultSafeInstance);
                    }
                }
                GetAllInstanceHosts(callback.Invoke, safeInstances);
            }, worldMeta.Id);
        }
        
        public static void GetWorldInstances(Action<List<SafeInstance>> callback, WorldMeta worldMeta)
        {
            List<SafeInstance> safeInstances = new List<SafeInstance>();
            APIPlayer.APIObject.GetPublicInstancesOfWorld(instanceResults =>
            {
                if (instanceResults.success)
                {
                    foreach (SafeInstance resultSafeInstance in instanceResults.result.SafeInstances)
                    {
                        if (safeInstances.Count(x =>
                                x.GameServerId == resultSafeInstance.GameServerId &&
                                x.InstanceId == resultSafeInstance.InstanceId) <= 0)
                            safeInstances.Add(resultSafeInstance);
                    }
                }
                QuickInvoke.InvokeActionOnMainThread(callback, safeInstances);
            }, worldMeta.Id);
        }
        
        private void RenderIcon()
        {
            (Texture2D, byte[]) d = Icons[currentIndex];
            GifRenderer gifRenderer = Banner.gameObject.GetComponent<GifRenderer>();
            if (gifRenderer != null)
                Destroy(gifRenderer);
            if (d.Item1 != null)
                Banner.texture = d.Item1;
            else if (d.Item2 != null)
            {
                gifRenderer = Banner.gameObject.AddComponent<GifRenderer>();
                gifRenderer.LoadGif(d.Item2);
            }
        }
        
        public void Render(WorldMeta worldMeta)
        {
            meta = worldMeta;
            Icons.Clear();
            currentIndex = 0;
            if(Banner != null)
            {
                if (worldMeta.IconURLs.Count > 0)
                {
                    int x = 0;
                    foreach (string iconURL in worldMeta.IconURLs)
                    {
                        DownloadTools.DownloadBytes(iconURL, bytes =>
                        {
                            if (GifRenderer.IsGif(bytes))
                            {
                                Icons.Add((null, bytes));
                                if (x > 0)
                                    return;
                                RenderIcon();
                            }
                            else
                            {
                                Icons.Add((ImageTools.BytesToTexture2D(iconURL, bytes), null));
                                if (x > 0)
                                    return;
                                RenderIcon();
                            }

                            x++;
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(worldMeta.ThumbnailURL))
                {
                    DownloadTools.DownloadBytes(worldMeta.ThumbnailURL, bytes =>
                    {
                        if (GifRenderer.IsGif(bytes))
                        {
                            Icons.Add((null, bytes));
                            RenderIcon();
                        }
                        else
                        {
                            Icons.Add((ImageTools.BytesToTexture2D(worldMeta.ThumbnailURL, bytes), null));
                            RenderIcon();
                        }
                    });
                }
                else
                {
                    Icons.Add((Defaults.Instance.DefaultWorldBanner, null));
                    RenderIcon();
                }
            }
            if (WorldName != null)
                WorldName.text = worldMeta.Name;
            if(WorldCreator != null)
                APIPlayer.APIObject.GetUser(creatorCallback =>
                {
                    QuickInvoke.InvokeActionOnMainThread(new Action(() =>
                    {
                        if (creatorCallback.success)
                            WorldCreator.text = $"Created by {creatorCallback.result.UserData.GetUserDisplayName()}";
                    }));
                }, worldMeta.OwnerId);
            if(DescriptionText != null)
                DescriptionText.text = worldMeta.Description;
            if (PlayerCount != null)
                GetWorldInstances(safeInstances => PlayerCount.text = safeInstances.GetWorldPlayerCount().ToString(),
                    worldMeta);
            if(NextIcon != null)
                NextIcon.gameObject.SetActive(Banner != null && worldMeta.IconURLs.Count > 0);
            if(PreviousIcon != null)
                PreviousIcon.gameObject.SetActive(Banner != null && worldMeta.IconURLs.Count > 0);
        }
        
        private void Start()
        {
            if(Banner == null || NextIcon == null || PreviousIcon == null) return;
            NextIcon.onClick.AddListener(() =>
            {
                if (Icons.Count <= 0)
                    return;
                if (currentIndex + 1 <= Icons.Count - 1)
                {
                    currentIndex++;
                    RenderIcon();
                    return;
                }
                currentIndex = 0;
                RenderIcon();
            });
            PreviousIcon.onClick.AddListener(() =>
            {
                if (Icons.Count <= 0)
                    return;
                if (currentIndex - 1 >= 0)
                {
                    currentIndex--;
                    RenderIcon();
                    return;
                }
                currentIndex = Icons.Count - 1;
                RenderIcon();
            });
        }
    }
}