using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Networking.Messages;
using UnityEngine;
using UnityOpus;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Tools
{
    public class OpusHandler : MonoBehaviour
    {
        public const int BITRATE = 96000;
        private const int AUDIO_CLIP_LENGTH = 1024 * 6;
        private const int FRAME_SIZE = 120;
        
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
            encoder ??= new Encoder(Mic.Frequency, NumChannels.Mono, OpusApplication.Audio)
            {
                Bitrate = BITRATE,
                Complexity = 10,
                Signal = OpusSignal.Music
            };
            a(data);
        }

        public void DecodeFromVoice(PlayerVoice playerVoice)
        {
            if (source.clip == null)
            {
                source.clip = AudioClip.Create(playerVoice.Auth.UserId + "_voice", AUDIO_CLIP_LENGTH,
                    playerVoice.Channels, playerVoice.SampleRate, false);
            }
            switch (playerVoice.Encoder.ToLower())
            {
                case "raw":
                    float[] d = DataConversion.ConvertByteToFloat(playerVoice.Bytes);
                    PlayDecodedToVoice(d, playerVoice.EncodeLength);
                    break;
                case "opus":
                    decoder ??= new Decoder(GetClosestFrequency(playerVoice.SampleRate), NumChannels.Mono);
                    e(playerVoice.Bytes, playerVoice.EncodeLength);
                    break;
            }
        }

        public void PlayDecodedToVoice(float[] pcm, int pcmLength) {
            if (audioClipData == null || audioClipData.Length != pcmLength) {
                // assume that pcmLength will not change.
                audioClipData = new float[pcmLength];
            }
            Array.Copy(pcm, audioClipData, pcmLength);
            source.clip.SetData(audioClipData, head);
            head += pcmLength;
            if (!source.isPlaying && head > AUDIO_CLIP_LENGTH / 2) {
                source.Play();
            }
            head %= AUDIO_CLIP_LENGTH;
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