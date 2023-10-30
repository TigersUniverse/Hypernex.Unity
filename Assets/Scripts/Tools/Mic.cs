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

        public const int FRAME_SIZE = 2880;
        
        public List<string> Devices => new(devices);
        private List<string> devices = new();
        
        public static string SelectedDevice { get; private set; }
        public static bool IsRecording { get; private set; }
        public static AudioClip Clip { get; private set; }
        public static Action<float[], AudioClip> OnClipReady { get; set; } = (data, clip) => { };
        public static int Frequency { get; private set; }
        public static int NumChannels => Clip.channels;

        private int captureLength;
        private int lastPosition;
        private Coroutine c;
        
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
            int maxFrequency;
            Microphone.GetDeviceCaps(SelectedDevice, out _, out maxFrequency);
            Frequency = maxFrequency;
            Clip = Microphone.Start(SelectedDevice, true, 1, Frequency);
            //c = StartCoroutine(ClipListener());
        }

        private IEnumerator ClipListener()
        {
            while (IsRecording)
            {
                /*int position = Microphone.GetPosition(SelectedDevice);
                if (position < lastPosition)
                    lastPosition = 0;
                if (position - lastPosition >= captureLength)
                {
                    float[] data = new float[FRAME_SIZE];
                    Clip.GetData(data, lastPosition);
                    OnClipReady.Invoke(data, Clip);
                    lastPosition = position;
                }
                else
                    yield return null;*/
                yield return null;
            }
        }

        private void Update()
        {
            // TODO: Fix lag sound
            int position = Microphone.GetPosition(SelectedDevice);
            if (position < 0) return;
            if (position < lastPosition)
                lastPosition = 0;
            if (position - lastPosition >= FRAME_SIZE)
            {
                float[] data = new float[FRAME_SIZE];
                Clip.GetData(data, lastPosition);
                OnClipReady.Invoke(data, Clip);
                lastPosition = position;
            }
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;
            Microphone.End(SelectedDevice);
            IsRecording = false;
            //StopCoroutine(c);
            OpusAudioCodec.MicrophoneOff();
        }
        
        private void Start()
        {
            if (Instance != null)
            {
                Logger.CurrentLogger.Warn("Cannot have more than one Mic Instance!");
                Destroy(this);
            }
            Instance = this;
            devices = Microphone.devices.ToList();
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