using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityOpus;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class Mic : MonoBehaviour
    {
        public static Mic Instance;
        
        private const int lengthSeconds = 1;

        public static string SelectedDevice { get; private set; }
        public static bool IsRecording { get; private set; }
        public static AudioClip Clip { get; private set; }
        public static Action<float[]> OnClipReady { get; set; } = data => { };
        public static SamplingFrequency Frequency { get; private set; }
        public static NumChannels NumChannels { get; private set; } = NumChannels.Mono;

        public List<string> Devices => new(devices);
            
        private List<string> devices = new();
        private int head;
        private float[] processBuffer = new float[512];
        private float[] microphoneBuffer;

        public void SetDevice(string device)
        {
            if (Devices.Contains(device) && !IsRecording)
                SelectedDevice = device;
        }

        public void StartRecording(int frequency = 24000)
        {
            if (IsRecording || string.IsNullOrEmpty(SelectedDevice))
                return;
            IsRecording = true;
            int minFrequency;
            int maxFrequency;
            Microphone.GetDeviceCaps(SelectedDevice, out minFrequency, out maxFrequency);
            if (frequency < minFrequency)
                frequency = minFrequency;
            if (frequency > maxFrequency)
                frequency = maxFrequency;
            Frequency = OpusHandler.GetClosestFrequency(frequency);
            microphoneBuffer ??= new float[lengthSeconds * (int) Frequency];
            /*int bufferSize = (int) (frequency * (length_ms / 1000f));
            c = StartCoroutine(clipRecording(frequency, bufferSize));*/
            Clip = Microphone.Start(SelectedDevice, true, lengthSeconds, (int)Frequency);
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;
            Microphone.End(SelectedDevice);
            IsRecording = false;
        }

        /*private IEnumerator clipRecording(int frequency, int bufferSize)
        {
            Clip = Microphone.Start(SelectedDevice, true, 1, frequency);
            while (IsRecording)
            {
                int pos = Microphone.GetPosition(SelectedDevice);
                if (pos > bufferSize)
                {
                    float[] data = new float[bufferSize];
                    Clip.GetData(data, pos - bufferSize);
                    OnClipReady.Invoke(data);
                    yield return null;
                }
            }
        }*/

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

        
        static int GetDataLength(int bufferLength, int head, int tail)
        {
            if (head < tail)
                return tail - head;
            return bufferLength - head + tail;
        } 
        
        private void Update()
        {
            if (IsRecording)
            {
                var position = Microphone.GetPosition(SelectedDevice);
                if (position < 0 || head == position) {
                    return;
                }

                Clip.GetData(microphoneBuffer, 0);
                while (GetDataLength(microphoneBuffer.Length, head, position) > processBuffer.Length) {
                    var remain = microphoneBuffer.Length - head;
                    if (remain < processBuffer.Length) {
                        Array.Copy(microphoneBuffer, head, processBuffer, 0, remain);
                        Array.Copy(microphoneBuffer, 0, processBuffer, remain, processBuffer.Length - remain);
                    } else {
                        Array.Copy(microphoneBuffer, head, processBuffer, 0, processBuffer.Length);
                    }

                    OnClipReady?.Invoke(processBuffer);

                    head += processBuffer.Length;
                    if (head > microphoneBuffer.Length) {
                        head -= microphoneBuffer.Length;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if(IsRecording)
                StopRecording();
        }
    }
}