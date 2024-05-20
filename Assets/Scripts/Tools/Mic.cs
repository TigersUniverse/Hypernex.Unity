using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Game.Audio;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class Mic : MonoBehaviour
    {
        public static Mic Instance;

        public List<string> Devices => new(devices);
        private List<string> devices = new();

        public static string SelectedDevice { get; private set; }
        public static bool IsRecording { get; private set; }
        public static AudioClip Clip { get; private set; }
        public static Action<float[], AudioClip, bool> OnClipReady { get; set; } = (data, clip, isEmpty) => { };
        public static int Frequency { get; private set; }
        public static int NumChannels => Clip.channels;
        public static int FrameSizeMs { get; set; } = 40;
        public static int SampleBufferSize => Mathf.RoundToInt(FrameSizeMs / 1000f * Frequency * NumChannels);

        private int lastPosition;

        public void SetDevice(string device)
        {
            if (Devices.Contains(device) && !IsRecording)
                SelectedDevice = device;
        }

        public void StartRecording()
        {
            if (IsRecording || string.IsNullOrEmpty(SelectedDevice))
                return;
            IsRecording = true;
            int minFrequency;
            int maxFrequency;
            Microphone.GetDeviceCaps(SelectedDevice, out minFrequency, out maxFrequency);
            // UnityEngine.Debug.Log($"Min {minFrequency} Max {maxFrequency}");
            Frequency = Mathf.Clamp(48000, minFrequency, maxFrequency);
            Frequency = 48000;
            // Frequency = 24000;
            Clip = Microphone.Start(SelectedDevice, true, 10, Frequency);
        }

        private void Update()
        {
            if (!IsRecording)
                return;
            int position = Microphone.GetPosition(SelectedDevice);
            if (position < 0)
                return;
            int offset = SampleBufferSize;
            if (position >= offset)
            {
                int targetPos = (position - offset) / SampleBufferSize * SampleBufferSize;
                int lastTargetPos = (lastPosition - offset) / SampleBufferSize * SampleBufferSize;
                if (targetPos != lastTargetPos)
                {
                    float[] data = new float[SampleBufferSize];
                    Clip.GetData(data, targetPos);
                    // UnityEngine.Debug.Log($"len {data.Length} pos {targetPos}");
                    OnClipReady.Invoke(data, Clip, false);
                    lastPosition = position;
                }
            }
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;
            Microphone.End(SelectedDevice);
            IsRecording = false;
            OnClipReady.Invoke(Array.Empty<float>(), Clip, true);
            OpusAudioCodec.MicrophoneOff();
            ConcentusAudioCodec.MicrophoneOff();
        }

        public void RefreshDevices() => devices = Microphone.devices.ToList();

        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Warn("Cannot have more than one Mic Instance!");
                Destroy(this);
            }
            Instance = this;
            RefreshDevices();
            if(devices.Count > 0)
                SelectedDevice = devices[0];
        }

        private void OnDestroy()
        {
            if(IsRecording)
                StopRecording();
        }
    }
}