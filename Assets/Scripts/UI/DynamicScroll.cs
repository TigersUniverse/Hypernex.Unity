using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class DynamicScroll : MonoBehaviour
    {
        public ScrollDirection Direction;
        public float Spacing = 5f;

        private ScrollRect scrollRect;
        public List<RectTransform> Items = new();

        public void Refresh()
        {
            // TODO: Scale Canvas
            if (Items.Count <= 0)
                return;
            float sizes = 0;
            switch (Direction)
            {
                case ScrollDirection.Vertical:
                {
                    int i = 0;
                    sizes += Items[0].rect.height / 2;
                    foreach (RectTransform item in Items)
                    {
                        if(i > 0)
                            sizes += item.rect.height;
                        sizes += Spacing;
                        item.anchoredPosition3D = new Vector3(item.rect.width/2, sizes, 0);
                        item.rotation = new Quaternion(0, 0, 0, 0);
                        i++;
                    }
                    break;
                }
                case ScrollDirection.Horizontal:
                {
                    int i = 0;
                    sizes += Items[0].rect.width / 2;
                    foreach (RectTransform item in Items)
                    {
                        if(i > 0)
                            sizes += item.rect.width;
                        sizes += Spacing;
                        item.anchoredPosition3D = new Vector3(sizes, item.rect.height/2 - (20 + (float) Math.PI * 2), 0);
                        item.rotation = new Quaternion(0, 0, 0, 0);
                        i++;
                    }
                    break;
                }
            }
        }
    
        public void AddItem(RectTransform item)
        {
            if (item.transform.parent != scrollRect.content.transform)
                item.transform.SetParent(scrollRect.content.transform);
            Items.Add(item);
            Refresh();
        }

        public void RemoveItem(RectTransform item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                Destroy(item.gameObject);
            }
            else
                return;
            Refresh();
        }

        public void Clear()
        {
            foreach (RectTransform rectTransform in Items)
                RemoveItem(rectTransform);
        }

        private void OnEnable() => scrollRect = gameObject.GetComponent<ScrollRect>();
    }

    public enum ScrollDirection
    {
        Vertical,
        Horizontal
    }
}