using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Configuration;
using Hypernex.Player;
using Hypernex.Tools;
using Hypernex.UIActions;
using HypernexSharp.APIObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Templates
{
    public class WorldTemplate : MonoBehaviour
    {
        private static List<WorldMeta> CachedWorldMeta = new();

        public LoginPageTopBarButton WorldPage;
        public CreateInstanceTemplate CreateInstanceTemplate;
        public TMP_Text WorldName;
        public RawImage Banner;
        public TMP_Text WorldCreator;
        public TMP_Text DescriptionText;
        public Button CreateInstanceButton;
        public DynamicScroll InstancesList;
        public Button NextIcon;
        public Button PreviousIcon;
        public Button ReturnButton;
        public Button FavoriteButton;
        public TMP_Text FavoriteButtonText;
        public Texture2D DefaultIcon;

        private List<(Texture2D, byte[])> Icons = new();
        private int currentIndex;
        private WorldMeta lastWorldMeta;
        private User lastCreator;

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
        
        private void CreateWorldListInstanceCard(SafeInstance safeInstance, WorldMeta worldMeta, User host, User creator)
        {
            GameObject instanceCard = DontDestroyMe.GetNotDestroyedObject("Templates").transform
                .Find("WorldListInstanceCardTemplate").gameObject;
            GameObject newInstanceCard = Instantiate(instanceCard);
            RectTransform c = newInstanceCard.GetComponent<RectTransform>();
            newInstanceCard.GetComponent<WorldListInstanceCardTemplate>().Render(safeInstance, worldMeta, host, creator);
            InstancesList.AddItem(c);
        }

        public static void GetWorldMeta(string worldId, Action<WorldMeta> callback)
        {
            if (CachedWorldMeta.Count(x => x.Id == worldId) > 0)
            {
                callback.Invoke(CachedWorldMeta.First(x => x.Id == worldId));
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
                    QuickInvoke.InvokeActionOnMainThread(callback, null);
            }, worldId);
        }

        public void Render(WorldMeta worldMeta, User creator, List<(SafeInstance, User)> instances,
            LoginPageTopBarButton previousPage)
        {
            InstancesList.Clear();
            Icons.Clear();
            Icons = new List<(Texture2D, byte[])>();
            currentIndex = 0;
            WorldName.text = worldMeta.Name;
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
                Icons.Add((DefaultIcon, null));
                RenderIcon();
            }
            WorldCreator.text = $"Created By {creator.Username}";
            DescriptionText.text = worldMeta.Description;
            foreach ((SafeInstance, User) instance in instances)
                CreateWorldListInstanceCard(instance.Item1, worldMeta, instance.Item2, creator);
            lastWorldMeta = worldMeta;
            lastCreator = creator;
            FavoriteButtonText.text = ConfigManager.SelectedConfigUser.SavedWorlds.Contains(worldMeta.Id)
                ? "Unfavorite"
                : "Favorite";
            FavoriteButton.onClick.RemoveAllListeners();
            FavoriteButton.onClick.AddListener(() =>
            {
                if (!ConfigManager.SelectedConfigUser.SavedWorlds.Contains(worldMeta.Id))
                {
                    ConfigManager.SelectedConfigUser.SavedWorlds.Add(worldMeta.Id);
                    ConfigManager.SaveConfigToFile();
                    FavoriteButtonText.text = "Unfavorite";
                }
                else
                {
                    ConfigManager.SelectedConfigUser.SavedWorlds.Remove(worldMeta.Id);
                    ConfigManager.SaveConfigToFile();
                    FavoriteButtonText.text = "Favorite";
                }
            });
            ReturnButton.onClick.RemoveAllListeners();
            ReturnButton.onClick.AddListener(() =>
            {
                CreateInstanceTemplate.gameObject.SetActive(false);
                previousPage.Show();
            });
            WorldPage.Show();
        }

        public void Start()
        {
            APIPlayer.OnUserRefresh += u => CachedWorldMeta.Clear();
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
            CreateInstanceButton.onClick.AddListener(() =>
            {
                CreateInstanceTemplate.Render(lastWorldMeta, lastCreator);
            });
        }
    }
}