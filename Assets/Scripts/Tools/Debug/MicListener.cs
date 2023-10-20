using Hypernex.Game.Audio;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(AudioSource))]
    public class MicListener : MonoBehaviour
    {
        private AudioSource AudioSource;

        private void OnClip(float[] pcm, AudioClip clip) =>
            AudioSourceDriver.Set(AudioSource, pcm, clip.channels, clip.frequency);

        private void OnEnable()
        {
            AudioSource = GetComponent<AudioSource>();
            Mic.OnClipReady += OnClip;
            Mic.Instance.StartRecording();
        }

        private void OnDisable()
        {
            Mic.Instance.StopRecording();
            Mic.OnClipReady -= OnClip;
        }
    }
}