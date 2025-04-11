using System;
using System.Collections;
using System.IO;
using Hypernex.Game;
using Hypernex.Tools;
using Nexbox;
using UnityEngine;
using UnityEngine.Networking;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Audio
    {
        private readonly Item item;
        private readonly bool read;
        private AudioSource audioSource;
        
        private static AudioSource GetAudioSource(Item item)
        {
            AudioSource a = item.t.GetComponent<AudioSource>();
            if (a == null)
                return null;
            return a;
        }

        public Audio(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            audioSource = GetAudioSource(i);
            if (audioSource == null) throw new Exception("No AudioSource found on Item at " + i.Path);
        }
        
        public bool Enabled
        {
            get => audioSource == null ? false : audioSource.enabled;
            set
            {
                if(read || audioSource == null) return;
                audioSource.enabled = value;
            }
        }

        public bool IsPlaying() => audioSource.isPlaying;
        public bool IsMuted() => audioSource.mute;
        public bool IsLooping() => audioSource.loop;

        public void Play()
        {
            if(read)
                return;
            audioSource.Play();
        }

        public void Pause()
        {
            if(read)
                return;
            audioSource.Pause();
        }

        public void Resume()
        {
            if(read)
                return;
            audioSource.UnPause();
        }

        public void Stop()
        {
            if(read)
                return;
            audioSource.Stop();
        }

        public void SetAudioClip(string asset)
        {
            if(read)
                return;
            AudioClip audioClip = (AudioClip) SandboxTools.GetObjectFromWorldResource(asset,
                GameInstance.GetInstanceFromScene(item.t.gameObject.scene));
            if(audioSource == null || audioClip == null)
                return;
            audioSource.clip = audioClip;
        }

        public void SetMute(bool value)
        {
            if(read || audioSource == null)
                return;
            audioSource.mute = value;
        }
        
        public void SetLoop(bool value)
        {
            if(read || audioSource == null)
                return;
            audioSource.loop = value;
        }
        
        public float GetPitch() => audioSource.pitch;
        public void SetPitch(float value)
        {
            if(read || audioSource == null)
                return;
            audioSource.pitch = value;
        }
        
        public float GetVolume() => audioSource.volume;
        public void SetVolume(float value)
        {
            if(read || audioSource == null)
                return;
            audioSource.volume = value;
        }
        
        public float GetPosition() => audioSource.time;
        public void SetPosition(float value)
        {
            if(read || audioSource == null)
                return;
            audioSource.time = value;
        }

        public float GetLength()
        {
            if(audioSource == null || audioSource.clip == null)
                return 0.0f;
            return audioSource.clip.length;
        }

        private static IEnumerator WaitForAudio(string pathToFile, AudioSource audioSource, object onLoad)
        {
            using UnityWebRequest r = UnityWebRequestMultimedia.GetAudioClip(pathToFile, AudioType.MPEG);
            yield return r.SendWebRequest();
            if (r.result == UnityWebRequest.Result.Success)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(r);
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onLoad));
            }
        }

        public void LoadFromStream(Streaming.StreamDownload streamDownload, object onLoad)
        {
            if(read || audioSource == null)
                return;
            if (streamDownload.isStream || !File.Exists(streamDownload.pathToFile))
                return;
            CoroutineRunner.Instance.StartCoroutine(WaitForAudio("file://" + streamDownload.pathToFile, audioSource,
                onLoad));
        }
    }
}