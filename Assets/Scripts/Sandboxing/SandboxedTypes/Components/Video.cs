using System;
using System.IO;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game.Video;
using Hypernex.Tools;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Video
    {
        private readonly bool read;
        private VideoPlayerDescriptor videoPlayerDescriptor;
        
        private IVideoPlayer GetVideoPlayer()
        {
            if (videoPlayerDescriptor == null) return null;
            if (videoPlayerDescriptor.CurrentVideoPlayer == null) return null;
            return videoPlayerDescriptor.CurrentVideoPlayer;
        }
        
        public Video(Item i)
        {
            read = i.IsReadOnly;
            videoPlayerDescriptor = i.t.GetComponent<VideoPlayerDescriptor>();
            if (videoPlayerDescriptor == null) throw new Exception("No VideoPlayerDescriptor found on Item at " + i.Path);
        }

        public bool IsPlaying() => GetVideoPlayer()?.IsPlaying ?? false;
        public bool IsMuted() => GetVideoPlayer()?.Muted ?? false;
        public bool IsLooping() => GetVideoPlayer()?.Looping ?? false;

        public void Play()
        {
            if(read)
                return;
            GetVideoPlayer()?.Play();
        }

        public void Pause()
        {
            if(read)
                return;
            GetVideoPlayer()?.Pause();
        }

        public void Stop()
        {
            if(read)
                return;
            GetVideoPlayer()?.Stop();
        }
        
        public void SetMute(bool value)
        {
            if(read)
                return;
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return;
            videoPlayer.Muted = value;
        }
        
        public void SetLoop(bool value)
        {
            if(read)
                return;
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return;
            videoPlayer.Looping = value;
        }
        
        public float GetPitch() => GetVideoPlayer()?.Pitch ?? 0.0f;
        public void SetPitch(float value)
        {
            if(read)
                return;
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return;
            videoPlayer.Pitch = value;
        }
        
        public float GetVolume() => GetVideoPlayer()?.Volume ?? 0.0f;

        public void SetVolume(float value)
        {
            if(read)
                return;
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return;
            videoPlayer.Volume = value;
        }
        
        public double GetPosition() => GetVideoPlayer()?.Position ?? 0.0;
        public void SetPosition(float value)
        {
            if(read)
                return;
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return;
            videoPlayer.Position = value;
        }

        public double GetLength()
        {
            IVideoPlayer videoPlayer = GetVideoPlayer();
            if(videoPlayer == null)
                return 0.0;
            return videoPlayer.Length;
        }

        public void LoadFromStream(Streaming.StreamDownload streamDownload)
        {
            if(read || videoPlayerDescriptor == null)
                return;
            if (streamDownload.isStream)
            {
                IVideoPlayer videoPlayer =
                    videoPlayerDescriptor.Replace(
                        VideoPlayerManager.GetVideoPlayerType(new Uri(streamDownload.pathToFile)));
                if (videoPlayer == null)
                    return;
                videoPlayer.Source = streamDownload.pathToFile;
            }
            else
            {
                if (!File.Exists(streamDownload.pathToFile))
                    return;
                IVideoPlayer videoPlayer =
                    videoPlayerDescriptor.Replace(VideoPlayerManager.GetVideoPlayerType(new Uri(streamDownload.pathToFile)));
                if (videoPlayer == null)
                    return;
                string filePath = videoPlayer.GetFileHeader() + streamDownload.pathToFile;
                videoPlayer.Source = filePath;
            }
        }
    }
}