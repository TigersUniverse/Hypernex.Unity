using UnityEngine;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class MobileManager : MonoBehaviour
    {
        public Init Init;
        public DashboardManager Dashboard;

        public GameObject Controls;
        public GameObject[] MainControls;

        private void Update()
        {
            Controls.SetActive(!Dashboard.IsVisible && Init.MobileControls);
            foreach (GameObject control in MainControls)
            {
                control.SetActive(Init.IsMobile);
            }
        }
    }
}