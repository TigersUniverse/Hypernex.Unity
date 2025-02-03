using System;
using FFmpeg.Unity;
using Hypernex.CCK.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Video
{
    public class FFVideoPlayer : IVideoPlayer
    {
        private VideoPlayerDescriptor desc;
        private FFPlayUnity videoPlayer;
        private FFTexturePlayer texture;
        private FFAudioPlayer audio;
        private global::BufferAudioSource buffer;
        private AudioSource audioSource;
        private Material material;

        public FFVideoPlayer(VideoPlayerDescriptor descriptor)
        {
            GameObject attachedObject = descriptor.gameObject;
            desc = descriptor;
            videoPlayer = attachedObject.GetComponent<FFPlayUnity>();
            if (videoPlayer == null)
            {
                videoPlayer = attachedObject.AddComponent<FFPlayUnity>();
                videoPlayer.videoOffset = -0.5d;
                texture = attachedObject.AddComponent<FFTexturePlayer>();
                audio = attachedObject.AddComponent<FFAudioPlayer>();
                audio.bufferSize = 2f;
                videoPlayer.texturePlayer = texture;
                videoPlayer.audioPlayer = audio;
            }
            audioSource = descriptor.AudioOutput;
            if (audioSource == null) audioSource = attachedObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = attachedObject.AddComponent<AudioSource>();
            buffer = audioSource.GetComponent<BufferAudioSource>();
            if (buffer == null) buffer = audioSource.gameObject.AddComponent<BufferAudioSource>();
            buffer.audioPlayer = videoPlayer.audioPlayer;
            buffer.audioSource = audioSource;
            videoPlayer.texturePlayer.OnDisplay += OnDisplay;
            descriptor.AudioOutput = audioSource;
            audioSource.outputAudioMixerGroup = Init.Instance.WorldGroup;
            audioSource.spatialize = true;
            descriptor.CurrentVideoPlayer = this;
        }

        private void OnDisplay(Texture2D tex)
        {
            foreach (Renderer renderer in desc.VideoOutputs)
            {
                if (renderer.material == null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    desc.ShaderEmissionProperty = "_EmissionMap";
                }
                renderer.material.mainTexture = tex;
                renderer.material.mainTextureScale = new Vector2(1f, -1f);
                if (desc.IsEmissive)
                {
                    renderer.material.SetTexture(desc.ShaderEmissionProperty, tex);
                    renderer.material.SetTextureScale(desc.ShaderEmissionProperty, new Vector2(1f, -1f));
                }
            }
        }
        
        public static bool CanBeUsed() => true;

        public static bool CanBeUsed(Uri source)
        {
            // if (source.Scheme != "file") return false;
            // TODO: Check to see if file is in compatible format
            return true;
        }

        public bool IsPlaying => videoPlayer.IsPlaying;
        public bool Muted
        {
            get => audioSource.mute;
            set => audioSource.mute = value;
        }

        public bool Looping { get; set; } = false;

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
            get => videoPlayer.PlaybackTime;
            set => videoPlayer.Seek(value);
        }

        public double Length => videoPlayer.GetLength();

        private string url;
        public string Source
        {
            get => url;
            set
            {
                if (url != value)
                {
                    url = value;
                    videoPlayer.Play(url);
                }
            }
        }

        public void Play() => videoPlayer.Resume();
        public void Pause() => videoPlayer.Pause();
        public void Stop() => videoPlayer.Pause();
        
        public void Dispose()
        {
            Object.Destroy(material);
        }
    }
}