using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hypernex.CCK;
using Hypernex.Tools;
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

        public const int SAMPLE_SIZE = 4096;
        private float[] RingBuffer = new float[SAMPLE_SIZE];
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
        }

        private void Update()
        {
            if (clip == null)
                return;

            /*
            float[] spectrum = new float[1024];
            audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
            for (int i = 1; i < spectrum.Length - 1; i++)
            {
                Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
            }
            */

            if (clip != null && audioSource.clip != clip)
            {
                audioSource.loop = false;
                Destroy(this);
            }
            else if (shouldStop && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void ReadCallback(float[] data)
        {
            #if false
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = RingBuffer[(AudioThreadPosition + i) % RingBuffer.Length];
            }
            shouldStop = false;
            AudioThreadPosition += data.Length;
            if (AudioThreadPosition > MainThreadPosition)
            {
                shouldStop = true;
            }
            return;
            #endif

            if (queue == null)
                return;
            if (queue.Count <= 0)
            {
                Array.Fill(data, 0f);
                // Debug.LogWarning("Buffer is empty, filling with zero.");
                emptyReads += data.Length;
                if (emptyReads >= maxEmptyReads)
                    shouldStop = true;
                return;
            }
            emptyReads = 0;
            shouldStop = false;
            if (mutex.WaitOne(1000))
            {
                #if false
                if (buffer.Length != 0)
                {
                    var buf = buffer.OrderBy(x => x.index).FirstOrDefault(x => x.pcm != null && x.played < x.pcm.Length); //buffer[lowestIndex];
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (buf != null)
                        {
                            buf.played++;
                        }
                        if (buf == null || buf.played >= buf.pcm.Length)
                        {
                            // find new buffer
                            buf = buffer.OrderBy(x => x.index).FirstOrDefault(x => x.pcm != null && x.played < x.pcm.Length);
                        }
                        if (buf != null)
                            data[i] = buf.pcm[buf.played];
                    }
                }
                #endif
                int found = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    #if true
                    if (queue.Count > 0)
                    {
                        data[i] = queue.Dequeue();
                    }
                    else
                    {
                        found++;
                        data[i] = 0f;
                    }
                    #endif
                }
                if (found > 0)
                {
                    // Debug.LogWarning($"Buffer is empty ({found}).");
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
            #if false
            Debug.Log(MainThreadPosition);
            for (int i = 0; i < pcm.Length; i++)
            {
                RingBuffer[(AudioThreadPosition + i) % RingBuffer.Length] = pcm[i];
            }
            MainThreadPosition += pcm.Length;
            if (AudioThreadPosition > MainThreadPosition)
            {
                MainThreadPosition = AudioThreadPosition;
                // audioSource.Stop();
                // audioSource.Play();
            }
            #endif
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
            if (clip == null || clip.channels != channels || clip.frequency != frequency)
            {
                maxEmptyReads = frequency * channels;
                shouldStop = true;
            }
            if (shouldStop)
            {
                // Debug.Log("new clip");
                startedQueue = false;
                shouldStop = false;
                clip = AudioClip.Create("", frequency / 2, channels, frequency, true, ReadCallback);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Stop();
                audioSource.Play();
            }
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }
        }

        public void InsertQueue(float[] pcm, int channels, int frequency, int index)
        {
            if (mutex.WaitOne(1000))
            {
                var buf = buffer[index % buffer.Length];
                if (buf.pcm.Length != pcm.Length)
                {
                    buf.pcm = new float[pcm.Length];
                }
                pcm.CopyTo(buf.pcm, 0);
                buf.index = index;
                buf.played = 0;
                mutex.ReleaseMutex();
            }
            else
            {
                // Debug.LogWarning("Mutex not aquired, buffer not filled.");
            }
            if (clip == null || clip.channels != channels || clip.frequency != frequency)
            {
                maxEmptyReads = frequency * channels;
                shouldStop = true;
            }
            if (shouldStop)
            {
                // Debug.Log("new clip");
                startedQueue = false;
                shouldStop = false;
                clip = AudioClip.Create("", frequency / 2, channels, frequency, true, ReadCallback);
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Stop();
                audioSource.Play();
            }
            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }
        }
    }
}