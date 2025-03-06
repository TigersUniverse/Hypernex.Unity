using System;
using System.Collections.Generic;
using Hypernex.Player;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction
{
    public class WorldRender : UIRender, IRender<WorldMeta>
    {
        public TMP_Text WorldName;
        public RawImage Banner;
        public TMP_Text WorldCreator;
        public TMP_Text DescriptionText;
        public Button NextIcon;
        public Button PreviousIcon;
        
        private List<(Texture2D, byte[])> Icons = new();
        private int currentIndex;
        
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
        }
        
        public void Start()
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