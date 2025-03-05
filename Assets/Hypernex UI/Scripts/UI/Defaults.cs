using UnityEngine;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class Defaults : MonoBehaviour
    {
        public static Defaults Instance { get; private set; }

        public Texture2D DefaultProfilePicture;
        public Texture2D DefaultProfileBanner;
        public Texture2D DefaultAvatarBanner;
        public Texture2D DefaultWorldBanner;

        public void OnEnable()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;
        }
    }
}