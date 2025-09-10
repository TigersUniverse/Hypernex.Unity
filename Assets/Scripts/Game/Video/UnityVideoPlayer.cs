using System;
using System.IO;
using System.Linq;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Internals;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Video
{
    public class UnityVideoPlayer : IVideoPlayer
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static readonly string[] supportedFormats =
        {
            ".asf",
            ".avi",
            ".dv",
            ".m4v",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".ogv",
            ".vp8",
            ".webm",
            ".wmv"
        };
        
        private static readonly string[] supportedCodecs = {
            "h263",
            "h264", "avc1",
            "vp80",
            "mp1v", "mp2v", "mp4v"
        };
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private static readonly string[] supportedFormats =
        {
            ".dv",
            ".m4v",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".ogv",
            ".vp8",
            ".webm",
            ".wmv"
        };

        private static readonly string[] supportedCodecs = {
            "h264", "avc1",
            "vp80",
            "mp1v", "mp2v", "mp4v",
            "divx", "xvid"
        };
#elif UNITY_ANDROID
        private static readonly string[] supportedFormats =
        {
            ".3gp",
            ".mp4",
            ".mkv",
            ".ts",
            ".webm"
        };

        private static readonly string[] supportedCodecs = {
            "h263",
            "h264", "avc1",
            "h265", "hevc", "hev1",
            "mpeg4", "mp4v",
            "vp80", "vp90",
            "av01"
        };
#else
        private static readonly string[] supportedFormats =
        {
            ".ogv",
            ".vp8",
            ".webm"
        };

        private static readonly string[] supportedCodecs = {
            "vp80", "vp90",
            "av01",
            "h264", "avc1"
        };
#endif
        
        private VideoPlayer videoPlayer;
        private AudioSource audioSource;
        private RenderTexture renderTexture;

        public UnityVideoPlayer(VideoPlayerDescriptor descriptor)
        {
            GameObject attachedObject = descriptor.gameObject;
            videoPlayer = attachedObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = attachedObject.AddComponent<VideoPlayer>();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
            audioSource = descriptor.AudioOutput;
            if (audioSource == null) audioSource = attachedObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = attachedObject.AddComponent<AudioSource>();
            descriptor.AudioOutput = audioSource;
            audioSource.outputAudioMixerGroup = Init.Instance.WorldGroup;
            audioSource.spatialize = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
            // Create the RenderTexture
            renderTexture = new RenderTexture(1920, 1080, 16);
            renderTexture.useDynamicScale = false;
            renderTexture.Create();
            // Apply RenderTexture to Video and Material
            videoPlayer.targetTexture = renderTexture;
            foreach (Renderer renderer in descriptor.VideoOutputs)
            {
                if (renderer.material == null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    descriptor.ShaderEmissionProperty = "_EmissionMap";
                }
                renderer.material.mainTexture = renderTexture;
                if(descriptor.IsEmissive)
                    renderer.material.SetTexture(descriptor.ShaderEmissionProperty, renderTexture);
            }
            videoPlayer.SetTargetAudioSource(0, descriptor.AudioOutput);
            descriptor.CurrentVideoPlayer = this;
        }
        
        public static bool CanBeUsed() => true;

        public static bool CanBeUsed(Uri source)
        {
            if (source.Scheme != "file") return false;
            string fileExtension = Path.GetExtension(source.AbsoluteUri);
            // regardless of if it's encoded with something that's supported, the file extension has to be supported
            if (!supportedFormats.Contains(fileExtension.ToLowerInvariant())) return false;
            // Check if we can get the codec
            if (VideoPlayerManager.CanGetCodecs())
            {
                // If we can, then get the codecs
                string[] codecs = VideoPlayerManager.GetCodecs(source);
                bool supportedCodecPresent = false;
                foreach (string codec in codecs)
                {
                    if(!supportedCodecs.Contains(codec)) continue;
                    supportedCodecPresent = true;
                }
                // We'll end it here, because now we know that we have a good video
                return supportedCodecPresent;
            }
            // If we make it here, we'll assume ffmpeg already re-encode the file
            return true;
        }

        public bool IsPlaying => videoPlayer.isPlaying;
        public bool Muted
        {
            get => audioSource.mute;
            set => audioSource.mute = value;
        }

        public bool Looping
        {
            get => videoPlayer.isLooping;
            set => videoPlayer.isLooping = value;
        }

        public float Pitch
        {
            get => audioSource.pitch;
            set => audioSource.pitch = value;
        }

        public float Volume
        {
            get => audioSource.volume;
            set => audioSource.volume = value;
        }

        public double Position
        {
            get => videoPlayer.time;
            set => videoPlayer.time = value;
        }

        public double Length => videoPlayer.length;

        public string Source
        {
            get => videoPlayer.url;
            set => videoPlayer.url = value;
        }

        public void Play() => videoPlayer.Play();
        public void Pause() => videoPlayer.Pause();
        public void Stop() => videoPlayer.Stop();
        public string GetFileHeader() => "file:///";

        public void Dispose()
        {
            renderTexture.DiscardContents();
            renderTexture.Release();
            Object.Destroy(renderTexture);
        }
    }
}