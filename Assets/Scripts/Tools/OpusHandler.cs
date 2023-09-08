using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hypernex.Networking.Messages;
using UnityEngine;
using UnityOpus;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    public class OpusHandler : MonoBehaviour
    {
        public const int BITRATE = 96000;
        private const int AUDIO_CLIP_LENGTH = 512;
        private const int FRAME_SIZE = 120;
        private const int AUDIO_QUEUE_LENGTH = 5;

        public Action<byte[], int> OnEncoded = (bytes, i) => { };
        public Action<float[], int> OnDecoded = (floats, i) => { };

        private Encoder encoder;
        private Decoder decoder;
        private Queue<float> pcmQueue = new();
        private readonly float[] pcmBuffer = new float[Decoder.maximumPacketDuration * (int)NumChannels.Mono];
        private readonly float[] frameBuffer = new float[FRAME_SIZE];
        private readonly byte[] outputBuffer = new byte[FRAME_SIZE * 4];
        private AudioSource source;
        private float[] audioClipData;
        private int head;
        private int headAudio;
        private int listIndex;
        private int audioIndex;
        private float[][] audioClipDataList;
        private Queue<float> pcmQueueOut = new();
        private Mutex listMutex = new();

        public static SamplingFrequency GetClosestFrequency(int frequency)
        {
            if (frequency <= 8000)
                return SamplingFrequency.Frequency_8000;
            if (frequency <= 12000)
                return SamplingFrequency.Frequency_12000;
            if (frequency <= 16000)
                return SamplingFrequency.Frequency_16000;
            if (frequency <= 24000)
                return SamplingFrequency.Frequency_24000;
            return SamplingFrequency.Frequency_48000;
        }

        internal void OnMicStart()
        {
            encoder?.Dispose();
            encoder = null;
        }

        public void EncodeMicrophone(float[] data)
        {
            encoder ??= new Encoder(Mic.Frequency, Mic.NumChannels, OpusApplication.Audio)
            {
                Bitrate = BITRATE,
                Complexity = 10,
                Signal = OpusSignal.Voice
            };
            a(data);
        }

        public void DecodeFromVoice(PlayerVoice playerVoice)
        {
            if (source.clip == null)
            {
                source.clip = AudioClip.Create(playerVoice.Auth.UserId + "_voice", AUDIO_CLIP_LENGTH,
                    playerVoice.Channels, playerVoice.SampleRate, true, ReaderCallback, PositionCallback);
                source.loop = true;
                source.Play();
            }
            switch (playerVoice.Encoder.ToLower())
            {
                case "raw":
                    float[] d = DataConversion.ConvertByteToFloat(playerVoice.Bytes);
                    PlayDecodedToVoice(d, playerVoice.EncodeLength);
                    break;
                case "opus":
                    decoder ??= new Decoder(GetClosestFrequency(playerVoice.SampleRate), playerVoice.Channels == 1 ? NumChannels.Mono : NumChannels.Stereo);
                    e(playerVoice.Bytes, playerVoice.EncodeLength);
                    break;
            }
        }

        private void PositionCallback(int position)
        {
            if (listMutex.WaitOne(1))
            {
                headAudio = position;
                listMutex.ReleaseMutex();
            }
        }

        private void ReaderCallback(float[] data)
        {
            Array.Clear(data, 0, data.Length);
            if (audioClipDataList == null)
            {
                return;
            }
            if (listMutex.WaitOne(1))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (pcmQueueOut.Count > 0)
                        data[i] = pcmQueueOut.Dequeue();
                    // else
                        // Debug.LogError("No more data!");
                }
                audioIndex = (audioIndex + 1) % audioClipDataList.Length;
                headAudio += data.Length;
                listMutex.ReleaseMutex();
            }
        }

        /*
        private void OnGUI()
        {
            if (source == null)
                return;
            GUILayout.BeginArea(Screen.safeArea);
            GUILayout.Label($"Length: {audioClipData?.Length}");
            GUILayout.Label($"Head: {head}");
            GUILayout.Label($"HeadAudio: {headAudio}");
            GUILayout.Label($"ListIndex: {listIndex}");
            GUILayout.Label($"AudioIndex: {audioIndex}");
            GUILayout.EndArea();
        }
        */

        public void PlayDecodedToVoice(float[] pcm, int pcmLength) {
            Debug.Assert(pcmLength > 0, $"pcmLength ({pcmLength}) < 0");
            if (listMutex.WaitOne(0))
            {
                if (source != null && source.isPlaying && pcmQueueOut.Count == 0)
                {
                    // source.Stop();
                    // Debug.Log("Stop", this);
                    // head = 0;
                }
                if (audioClipDataList == null)
                {
                    audioClipDataList = new float[AUDIO_QUEUE_LENGTH][];
                    for (int i = 0; i < audioClipDataList.Length; i++)
                    {
                        audioClipDataList[i] = new float[pcmLength];
                    }
                }
                if (audioClipData == null || audioClipData.Length != pcmLength) {
                    // assume that pcmLength will not change.
                    audioClipData = new float[pcmLength];
                }
                Array.Copy(pcm, audioClipData, pcmLength);
                // source.clip.SetData(audioClipData, head);
                for (int i = 0; i < pcmLength; i++)
                {
                    pcmQueueOut.Enqueue(pcm[i]);
                }
                if (!source.isPlaying && pcmQueueOut.Count > 0) {
                    // source.PlayDelayed(0.1f);
                    source.Play();
                    // Debug.Log("Play", this);
                }
                head += pcmLength;
                listMutex.ReleaseMutex();
            }
        }

        private void OnEnable()
        {
            source = GetComponent<AudioSource>();
            // Can be null if we're only encoding (LocalPlayer)
            if(source != null)
                source.loop = true;
            /*Mic.OnClipReady += a;
            OnEncoded += e;
            OnDecoded += d;*/
        }
        
        private void a(float[] data) {
            foreach (var sample in data) {
                pcmQueue.Enqueue(sample);
            }
            while (pcmQueue.Count > FRAME_SIZE) {
                for (int i = 0; i < FRAME_SIZE; i++) {
                    frameBuffer[i] = pcmQueue.Dequeue();
                }
                var encodedLength = encoder.Encode(frameBuffer, outputBuffer);
                OnEncoded?.Invoke(outputBuffer, encodedLength);
            }
        }
        
        private void e(byte[] data, int length) {
            var pcmLength = decoder.Decode(data, length, pcmBuffer);
            OnDecoded?.Invoke(pcmBuffer, pcmLength);
        }

        private void Update()
        {
            if (listMutex.WaitOne(0))
            {
                if (source != null && source.isPlaying && pcmQueueOut.Count == 0)
                {
                    source.Stop();
                    // Debug.Log("Stop2", this);
                }
                listMutex.ReleaseMutex();
            }
            if (pcmQueue.Count > FRAME_SIZE)
            {
                for (int i = 0; i < FRAME_SIZE; i++)
                {
                    frameBuffer[i] = pcmQueue.Dequeue();
                }
                int l = encoder.Encode(frameBuffer, outputBuffer);
                OnEncoded.Invoke(outputBuffer, l);
            }
        }

        private void OnDisable()
        {
            /*Mic.OnClipReady -= a;
            OnEncoded -= e;
            OnDecoded -= d;*/
            encoder?.Dispose();
            decoder?.Dispose();
            if(source != null)
                source.Stop();
        }
    }
}