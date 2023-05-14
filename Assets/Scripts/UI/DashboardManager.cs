using Hypernex.Game;
using UnityEngine;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class DashboardManager : MonoBehaviour
    {
        public bool IsVisible { get; private set; }

        public GameObject Dashboard;

        public void Start() => IsVisible = Dashboard.activeSelf;

        public void PositionDashboard(LocalPlayer localPlayer)
        {
            Transform reference = localPlayer.CameraOffset.transform.GetChild(0).GetChild(0);
            Dashboard.transform.position = reference.position + reference.forward * 3f;
            Dashboard.transform.rotation =
                Quaternion.LookRotation((Dashboard.transform.position - reference.position).normalized);
        }

        public void ToggleDashboard(LocalPlayer localPlayer)
        {
            IsVisible = !IsVisible;
            if (IsVisible)
                PositionDashboard(localPlayer);
            Dashboard.SetActive(IsVisible);
        }
    }
}