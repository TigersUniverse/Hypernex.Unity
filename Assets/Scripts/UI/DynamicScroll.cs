using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class DynamicScroll : MonoBehaviour
    {
        public ScrollDirection Direction;
        public float Spacing = 5f;

        private ScrollRect scrollRect;
        public List<RectTransform> Items = new();

        private void Scale()
        {
            float sizeUntilScale = 0f;
            float size = 0f;
            switch (Direction)
            {
                case ScrollDirection.Vertical:
                    scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, 0);
                    sizeUntilScale += scrollRect.content.rect.size.y;
                    foreach (RectTransform rectTransform in Items)
                    {
                        size += rectTransform.sizeDelta.y + Spacing;
                        if (size >= scrollRect.content.sizeDelta.y && sizeUntilScale == 0f)
                            sizeUntilScale = size;
                    }
                    if (size < scrollRect.content.sizeDelta.y)
                        break;
                    scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, size - sizeUntilScale);
                    break;
                case ScrollDirection.Horizontal:
                    scrollRect.content.sizeDelta = new Vector2(0, scrollRect.content.sizeDelta.y);
                    sizeUntilScale += scrollRect.content.rect.size.x;
                    foreach (RectTransform rectTransform in Items)
                    {
                        size += rectTransform.sizeDelta.x + Spacing;
                        if (size >= scrollRect.content.sizeDelta.x && sizeUntilScale == 0f)
                            sizeUntilScale = size;
                    }
                    if (size < scrollRect.content.sizeDelta.x)
                        break;
                    scrollRect.content.sizeDelta = new Vector2(size - sizeUntilScale, scrollRect.content.sizeDelta.y);
                    break;
            }
        }

        public void Refresh()
        {
            if (Items.Count <= 0)
                return;
            float sizes = 0;
            switch (Direction)
            {
                case ScrollDirection.Vertical:
                {
                    int i = 0;
                    sizes -= Items[0].rect.height / 2;
                    foreach (RectTransform item in Items)
                    {
                        if(i > 0)
                        {
                            sizes += item.rect.height;
                            sizes += Spacing;
                        }
                        item.anchoredPosition3D = new Vector3(0, sizes, 0);
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
                        {
                            sizes += item.rect.width;
                            sizes += Spacing;
                        }
                        item.anchoredPosition3D = new Vector3(sizes, item.rect.height/2 - (20 + (float) Math.PI * 2), 0);
                        item.rotation = new Quaternion(0, 0, 0, 0);
                        i++;
                    }
                    break;
                }
            }
            Scale();
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
            foreach (RectTransform rectTransform in new List<RectTransform>(Items))
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