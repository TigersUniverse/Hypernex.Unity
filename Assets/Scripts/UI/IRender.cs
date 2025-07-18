﻿using UnityEngine;

namespace Hypernex.UI
{
    public interface IRender<T>
    {
        public Transform transform { get; }
        public void Render(T t);
        public T1 GetComponent<T1>();
    }
}