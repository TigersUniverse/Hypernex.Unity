using System;
using Hypernex.Game;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Hypernex.Sandboxing.SandboxedTypes.Components
{
    public class Graphic
    {
        private readonly Item item;
        private readonly bool read;
        private RawImage rawImage;
        private Image image;

        public Graphic(Item i)
        {
            item = i;
            read = i.IsReadOnly;
            rawImage = i.t.GetComponent<RawImage>();
            image = i.t.GetComponent<Image>();
            if(rawImage == null && image == null)
                throw new Exception("No Graphic found on Item at " + i.Path);
        }
        
        public bool Enabled
        {
            get
            {
                if (image != null) return image.enabled;
                if (rawImage == null) return false;
                return rawImage.enabled;
            }
            set
            {
                if(read) return;
                if (image != null) image.enabled = value;
                if (rawImage != null) image.enabled = value;
            }
        }
        
        public bool RawEnabled
        {
            get => rawImage == null ? false : rawImage.enabled;
            set
            {
                if(read || rawImage == null) return;
                rawImage.enabled = value;
            }
        }
        
        public void SetImageFromAsset(string asset)
        {
            if(read)
                return;
            Object a = SandboxTools.GetObjectFromWorldResource(asset,
                GameInstance.GetInstanceFromScene(item.t.gameObject.scene));
            if (a == null)
                return;
            if (image != null)
                image.sprite = (Sprite) a;
            if (rawImage != null)
                rawImage.texture = (UnityEngine.Texture) a;
        }
        
        public void SetImageFromTexture(Texture texture)
        {
            if(read)
                return;
            if (rawImage != null)
                rawImage.texture = texture.r;
        }
    }
}