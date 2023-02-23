using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class DynamicScroll : MonoBehaviour
{
    public ScrollDirection Direction;
    public float Spacing = 5f;

    private ScrollRect scrollRect;
    private readonly List<RectTransform> Items = new();

    private Func<RectTransform, int> sort;

    private void Refresh()
    {
        float sizes = 0;
        foreach (RectTransform item in Items)
        {
            switch (Direction)
            {
                case ScrollDirection.Vertical:
                {
                    foreach (RectTransform o in Items)
                        sizes += o.rect.height;
                    sizes += Spacing;
                    item.rect.Set(item.rect.x, sizes, item.rect.width, item.rect.height);
                    break;
                }
                case ScrollDirection.Horizontal:
                {
                    foreach (RectTransform o in Items)
                        sizes += o.rect.width;
                    sizes += Spacing;
                    item.rect.Set(sizes, item.rect.y, item.rect.width, item.rect.height);
                    break;
                }
            }
        }
    }
    
    public void AddItem(RectTransform item)
    {
        if (item.transform.parent != scrollRect.content.transform)
            item.transform.parent = scrollRect.content.transform;
        Items.Add(item);
        Refresh();
    }

    public void RemoveItem(RectTransform item)
    {
        if (Items.Contains(item))
        {
            Items.Remove(item);
            Destroy(item);
        }
        else
            return;
        Refresh();
    }

    private void OnEnable() => scrollRect = gameObject.GetComponent<ScrollRect>();

    private int lastCount;
    private void Update()
    {
        if(lastCount != Items.Count)
            Refresh();
        lastCount = Items.Count;
    }
}

public enum ScrollDirection
{
    Vertical,
    Horizontal
}