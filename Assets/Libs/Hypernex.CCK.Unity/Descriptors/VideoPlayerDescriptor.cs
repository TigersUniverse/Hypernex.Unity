using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Internals;
using UnityEngine;

namespace Hypernex.CCK.Unity.Descriptors
{
    /// <summary>
    /// Describes how to set up the VideoPlayer at setup
    /// </summary>
    public class VideoPlayerDescriptor : MonoBehaviour
    {
        /// <summary>
        /// Whether or not to set the emissive map to the texture of the created Render Texture
        /// </summary>
        public bool IsEmissive = true;

        /// <summary>
        /// Output Renderers for the VideoPlayer
        /// </summary>
        public List<Renderer> VideoOutputs = new List<Renderer>();

        /// <summary>
        /// Output Audio for Audio linked to the VideoPlayer
        /// </summary>
        public AudioSource AudioOutput;

        /// <summary>
        /// The shader property for the emission map
        /// </summary>
        public string ShaderEmissionProperty = "_EmissionMap";
        
        [HideInInspector] public IVideoPlayer CurrentVideoPlayer;

        public bool TryGetBehaviour(out VideoPlayerBehaviour videoPlayerBehaviour)
        {
            try
            {
                if (CurrentVideoPlayer == null)
                {
                    videoPlayerBehaviour = null;
                    return false;
                }
                videoPlayerBehaviour = (VideoPlayerBehaviour) CurrentVideoPlayer;
                return true;
            }
            catch (Exception)
            {
                videoPlayerBehaviour = null;
                return false;
            }
        }
        
        public void Awake()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.Awake();
        }

        public void Start()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.Start();
        }
        
        public void OnEnable()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.OnEnable();
        }
        
        public void OnDisable()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.OnDisable();
        }
        
        public void FixedUpdate()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.FixedUpdate();
        }
        
        public void Update()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.Update();
        }
        
        public void LateUpdate()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.LateUpdate();
        }
        
        public void OnAudioFilterRead(float[] data, int channels)
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.OnAudioFilterRead(data, channels);
        }
        
        public void OnGUI()
        {
            if(!TryGetBehaviour(out VideoPlayerBehaviour b)) return;
            b.OnGUI();
        }

        private void OnDestroy() => CurrentVideoPlayer?.Dispose();
    }
}