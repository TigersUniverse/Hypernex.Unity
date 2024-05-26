using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hypernex.CCK;
using Hypernex.Tools;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Game.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BufferAudioSource : MonoBehaviour
    {
        public class AudioPacket
        {
            public float[] pcm = new float[0];
            public int index = 0;
            public int played = 0;
        }

        public const int SAMPLE_SIZE = 512;
        public const int CLIP_SAMPLE_SIZE = 4096;
        public const float MAX_DELAY = 0.1f;
        private float[] RingBuffer = new float[CLIP_SAMPLE_SIZE];
        private int RingBufferCount = 0;
        private float[] TempRingBuffer = new float[CLIP_SAMPLE_SIZE];
        private int PositionSamples = 0;
        private int AudioThreadPosition = 0;
        private int MainThreadPosition = 0;

        private Queue<float> queue = new Queue<float>();
        private AudioPacket[] buffer = new AudioPacket[1024];
        private bool startedQueue = false;
        private bool shouldStop = false;
        private int emptyReads = 0;
        private int maxEmptyReads = 0;
        private Mutex mutex = new Mutex();
        private AudioClip clip = null;
        public AudioSource audioSource;
        public List<AudioSource> sources = new List<AudioSource>();

        private double startTime;
        private IEnumerator routine;
        private bool playing = false;

        private float[] spectrum = new float[1024];

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = new AudioPacket();
            }
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            /*
            for (int i = 0; i < 3; i++)
            {
                var src = Instantiate(audioSource.gameObject);
                DestroyImmediate(src.GetComponent<BufferAudioSource>());
                src.transform.SetParent(transform);
                sources.Add(src.GetComponent<AudioSource>());
            }
            */
        }

        private void Update()
        {
            playing = audioSource.isPlaying;
            if (clip == null)
                return;

#if false
            if (AudioSettings.dspTime - startTime >= clip.length - Time.deltaTime)
            {
                var newClip = AudioClip.Create("Voice", CLIP_SAMPLE_SIZE, clip.channels, clip.frequency, false, ReadCallback);
                audioSource.clip = newClip;
                startTime += clip.length;
                audioSource.Play();
            }
#endif

            // /*
            audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
            for (int i = 1; i < spectrum.Length - 1; i++)
            {
                Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
            }
            // */

            if (audioSource.clip != clip)
            {
                // audioSource.loop = false;
                // Destroy(this);
            }
            else if (shouldStop && audioSource.isPlaying)
            {
                audioSource.Stop();
                Destroy(this);
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!playing)
                return;
            if (TempRingBuffer.Length != data.Length / channels)
                TempRingBuffer = new float[data.Length / channels];
            ReadCallback(TempRingBuffer);
            for (int i = 0; i < data.Length / channels; i++)
            {
                for (int j = 0; j < channels; j++)
                {
                    data[i * channels + j] *= TempRingBuffer[i];
                }
            }
        }

        private IEnumerator PlaybackLoop()
        {
            AudioClip clip0 = AudioClip.Create("Voice0", CLIP_SAMPLE_SIZE, audioSource.clip.channels, audioSource.clip.frequency, false);
            AudioClip clip1 = AudioClip.Create("Voice1", CLIP_SAMPLE_SIZE, audioSource.clip.channels, audioSource.clip.frequency, false);
            bool use0 = true;
            startTime = AudioSettings.dspTime + 0.25f;
            int div = 1;
            // Fill(clip, audioSource.clip.samples / div);
            Fill(use0 ? clip0 : clip1, audioSource.clip.samples / div);
            audioSource.clip = use0 ? clip0 : clip1;
            audioSource.PlayScheduled(startTime);
            use0 = !use0;
            while (!shouldStop)
            {
                startTime += (double)audioSource.clip.samples / audioSource.clip.frequency / div;
                Fill(use0 ? clip0 : clip1, audioSource.clip.samples / div);
                audioSource.clip = use0 ? clip0 : clip1;
                audioSource.PlayScheduled(startTime);
                use0 = !use0;
                while (AudioSettings.dspTime - startTime < (double)audioSource.clip.samples / audioSource.clip.frequency / div - Time.unscaledDeltaTime * 0)
                    yield return null;
                // Fill(clip, audioSource.clip.samples / div);
            }
            Destroy(this);
        }

        private void Fill(AudioClip clip, int size)
        {
            if (TempRingBuffer.Length != size)
                TempRingBuffer = new float[size];
            ReadCallback(TempRingBuffer);
            clip.SetData(TempRingBuffer, 0);
            clip.LoadAudioData();
            return;
            clip.SetData(TempRingBuffer, PositionSamples % clip.samples);
            clip.LoadAudioData();
            PositionSamples += size;
        }

        private bool FillInsert(int size)
        {
            if (RingBufferCount < size)
            {
                return false;
            }
            if (TempRingBuffer.Length != size)
                TempRingBuffer = new float[size];
            ReadCallbackInsert(TempRingBuffer);
            // MainThreadPosition = AudioThreadPosition + clip.samples - size;
            while (MainThreadPosition < 0)
                MainThreadPosition += clip.samples;
            clip.SetData(TempRingBuffer, MainThreadPosition % clip.samples);
            MainThreadPosition += size;
            return true;
        }

        private void ReadCallbackInsert(float[] data)
        {
            if (RingBufferCount <= 0)
            {
                // Debug.LogWarning("Out of data on the RingBuffer");
                Array.Fill(data, 0f);
                emptyReads += data.Length;
                if (emptyReads >= maxEmptyReads)
                    shouldStop = true;
                return;
            }
            emptyReads = 0;
            shouldStop = false;
            if (mutex.WaitOne(1000))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (RingBufferCount > 0)
                    {
                        if (AudioThreadPosition >= 0)
                        {
                            data[i] = RingBuffer[(AudioThreadPosition) % RingBuffer.Length];
                            RingBuffer[(AudioThreadPosition) % RingBuffer.Length] = 0f;
                        }
                        else
                        {
                            data[i] = 0f;
                        }
                        RingBufferCount--;
                        AudioThreadPosition++;
                    }
                    else
                    {
                        data[i] = 0f;
                    }
                }
                // AudioThreadPosition += data.Length;
                mutex.ReleaseMutex();
            }
        }

        private void PositionCallbackInsert(int position)
        {
            Debug.Log(position);
            AudioThreadPosition = position;
        }

        private void ReadCallback(float[] data)
        {
            if (queue == null)
                return;
            if (queue.Count <= 0)
            {
                Array.Fill(data, 0f);
                Debug.LogWarning("Buffer is empty, filling with zero.");
                emptyReads += data.Length;
                if (emptyReads >= maxEmptyReads)
                    shouldStop = true;
                return;
            }
            emptyReads = 0;
            shouldStop = false;
            if (mutex.WaitOne(1000))
            {
                int found = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (queue.Count > 0)
                    {
                        data[i] = queue.Dequeue();
                    }
                    else
                    {
                        found++;
                        data[i] = 0f;
                    }
                }
                if (found > 0)
                {
                    Debug.LogWarning($"Buffer is empty ({found}).");
                }
                mutex.ReleaseMutex();
            }
            else
            {
                // Debug.LogWarning("Mutex not aquired, buffer not readable.");
            }
        }

        public void AddQueue(float[] pcm, int channels, int frequency)
        {
            // /*
            // queue ??= new Queue<float>();
            if (mutex.WaitOne(1000))
            {
                foreach (var val in pcm)
                {
                    queue.Enqueue(val);
                }
                mutex.ReleaseMutex();
            }
            else
            {
                // Debug.LogWarning("Mutex not aquired, buffer not filled.");
            }
            // */
            bool newClip = false;
            if (clip == null || clip.channels != channels || clip.frequency != frequency)
            {
                maxEmptyReads = frequency * channels;
                newClip = true;
            }
            if (newClip && queue.Count > CLIP_SAMPLE_SIZE * 2)
            {
                // Debug.Log("new clip");
                startedQueue = false;
                shouldStop = false;
                clip = AudioClip.Create("Voice", CLIP_SAMPLE_SIZE, channels, frequency, false);
                float[] temp = new float[CLIP_SAMPLE_SIZE];
                Array.Fill(temp, 1f);
                clip.SetData(temp, 0);
                // clip = AudioClip.Create("Voice", CLIP_SAMPLE_SIZE, channels, frequency, true, ReadCallback);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Stop();
            #if true
                audioSource.Play();
            #endif
                startTime = AudioSettings.dspTime;
                PositionSamples = 0;
            #if false
                if (routine != null)
                    StopCoroutine(routine);
                routine = PlaybackLoop();
                StartCoroutine(routine);
            #endif
            }
            if (audioSource.clip != clip)
            {
                // audioSource.clip = clip;
            }
        }

        public void AddInsertQueue(float[] pcm, int channels, int frequency, int index)
        {
            // InsertQueue(pcm, channels, frequency, index);
            // return;
            // if (clip != null && index < PositionSamples)
            //     return;
            // /*
            // queue ??= new Queue<float>();
            if (mutex.WaitOne(1000))
            {
                for (int i = 0; i < pcm.Length; i++)
                {
                    RingBuffer[(index + i) % RingBuffer.Length] = pcm[i];
                    RingBufferCount++;
                }
                PositionSamples = index;
                mutex.ReleaseMutex();
            }
            // */
            if (clip == null || clip.channels != channels || clip.frequency != frequency)
            {
                maxEmptyReads = frequency * channels;
                shouldStop = true;
            }
            if (RingBufferCount > SAMPLE_SIZE && !audioSource.isPlaying && clip == null)
            {
                // Debug.Log("new clip");
                startedQueue = false;
                shouldStop = false;
                clip = AudioClip.Create("Voice", CLIP_SAMPLE_SIZE, channels, frequency, false);
                // PositionSamples = 0;
                // Debug.Log($"{clip.samples} * {channels} = {SAMPLE_SIZE * 2}");
                MainThreadPosition = index;// - SAMPLE_SIZE * 2;
                AudioThreadPosition = index;
                FillInsert(SAMPLE_SIZE);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Stop();
                audioSource.PlayDelayed(MAX_DELAY);
                startTime = AudioSettings.dspTime - MAX_DELAY;
            }
        }

        public void InsertQueue(float[] pcm, int channels, int frequency, int index)
        {
            bool newClip = false;
            if (clip == null || clip.channels != channels || clip.frequency != frequency)
            {
                maxEmptyReads = frequency * channels;
                // shouldStop = true;
                newClip = true;
            }
            if (mutex.WaitOne(1000))
            {
                if (newClip)
                {
                    Array.Fill(RingBuffer, 0f);
                    RingBufferCount = 0;
                }
                else
                {
                    for (int i = 0; i < pcm.Length; i++)
                    {
                        RingBuffer[(index + i) % RingBuffer.Length] = pcm[i];
                        RingBufferCount++;
                    }
                    PositionSamples = index;
                }
                mutex.ReleaseMutex();
            }
            if (newClip)
            {
                Debug.Log("new clip");
                startedQueue = false;
                shouldStop = false;
                // AudioSettings.GetDSPBufferSize(out int bufLen, out _);
                // Debug.Log(bufLen);
                AudioThreadPosition = 0;
                // AudioThreadPosition = bufLen;
                // AudioThreadPosition = frequency * channels;
                // AudioThreadPosition = SAMPLE_SIZE;
                clip = AudioClip.Create("Voice", CLIP_SAMPLE_SIZE, channels, frequency, true, ReadCallbackInsert);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Stop();
                audioSource.Play();
                startTime = AudioSettings.dspTime;
            }
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }
        }
    }
}