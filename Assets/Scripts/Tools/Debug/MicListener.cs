using System.Linq;
using Hypernex.Game.Audio;
using UnityEngine;

namespace Hypernex.Tools.Debug
{
    [RequireComponent(typeof(AudioSource))]
    public class MicListener : MonoBehaviour
    {
        private AudioSource AudioSource;
        public string driverName = "";

        private void OnClip(float[] pcm, AudioClip clip, bool isEmpty)
        {
            if (string.IsNullOrEmpty(driverName))
                AudioSourceDriver.Set(AudioSource, pcm, clip.channels, clip.frequency);
            else
            {
                var driver = AudioSourceDriver.GetAudioCodecByName(driverName);
                if (driver == null)
                    return;
                var msgs = driver.Encode(pcm, clip, default);
                foreach (var msg in msgs)
                {
                    driver.Decode(msg, AudioSource);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MicOn();
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                MicOff();
            }
        }

        private void MicOn()
        {
            AudioSource = GetComponent<AudioSource>();
            if (Mic.IsRecording)
                return;
            Mic.OnClipReady += OnClip;
            Mic.Instance.StartRecording();
            // UnityEngine.Debug.Log($"Channels: {Mic.NumChannels} SampleRate: {Mic.Frequency} Output: {AudioSettings.outputSampleRate}");
        }

        private void MicOff()
        {
            if (!Mic.IsRecording)
                return;
            Mic.Instance.StopRecording();
            Mic.OnClipReady -= OnClip;
        }
    }
}