using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Descriptors;

namespace Hypernex.Game.Avatar.VisemeProviders
{
    public interface IVisemeProvider : IDisposable
    {
        public bool Enabled { get; set; }
        
        public virtual void SetupLocal(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes){}
        public virtual void SetupNet(AvatarCreator avatarCreator, BlendshapeDescriptor[] blendshapes){}
        
        internal void ApplyAudioClipToLipSync(float[] data);
        
        public int GetVisemeIndex();
        
        /// <summary>
        /// Gets all Visemes and their values
        /// </summary>
        /// <returns>Key: Name of Viseme, Value: Weight of Viseme</returns>
        public Dictionary<string, float> GetVisemes();
    }
}