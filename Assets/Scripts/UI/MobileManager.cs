using UnityEngine;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class MobileManager : MonoBehaviour
    {
        public Init Init;
        public DashboardManager Dashboard;

        public GameObject Controls;

        private void Start()
        {
            if(Init.IsMobile) return;
            gameObject.SetActive(false);
        }

        private void Update() => Controls.SetActive(!Dashboard.IsVisible);
    }
}