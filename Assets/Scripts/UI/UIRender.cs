using UnityEngine;

namespace Hypernex.UI
{
    public abstract class UIRender : MonoBehaviour
    {
        protected bool HasInitialized { get; private set; }

        internal virtual void Initialize() => HasInitialized = true;

        private void Start()
        {
            if(HasInitialized) return;
            Initialize();
        }
    }
}