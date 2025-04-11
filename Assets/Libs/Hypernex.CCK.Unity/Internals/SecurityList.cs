using System.Collections.Generic;

namespace Hypernex.CCK.Unity.Internals
{
    public class SecurityList<T>
    {
        private readonly List<T> l;
        private readonly bool allowRemove;

        public SecurityList(bool allowRemove = false) : this(new List<T>(), allowRemove){}

        public SecurityList(List<T> l, bool allowRemove = false)
        {
            this.l = l;
            this.allowRemove = allowRemove;
        }

        public void Allow(T t)
        {
            if(l.Contains(t)) return;
            l.Add(t);
        }

        public void Remove(T t)
        {
            if(!allowRemove) return;
            if (!l.Contains(t)) return;
            l.Remove(t);
        }

        public List<T> ToList() => new List<T>(l);
        public T[] ToArray() => l.ToArray();
    }
}