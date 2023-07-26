using Hypernex.Game;
using UnityEngine;

namespace Hypernex.UI
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class DashboardManager : MonoBehaviour
    {
        private const float SCALE = 0.001293102f;
        
        public bool IsVisible { get; private set; }
        public DontDestroyMe DontDestroyMe { get; private set; }

        public GameObject Dashboard;

        public Vector3 OpenedPosition { get; private set; }
        public Quaternion OpenedRotation { get; private set; }
        public Vector3 OpenedScale { get; private set; }
        public Bounds OpenedBounds { get; private set; }

        public void Start()
        {
            IsVisible = Dashboard.activeSelf;
            DontDestroyMe = GetComponent<DontDestroyMe>();
        }

        public void PositionDashboard(LocalPlayer localPlayer)
        {
            Transform reference = localPlayer.Camera.transform.GetChild(1);
            if(LocalPlayer.IsVR)
            {
                float s = localPlayer.transform.localScale.y;
                Dashboard.transform.localScale = new Vector3(SCALE * s, SCALE * s, SCALE * s);
                Dashboard.transform.position = reference.position + reference.forward * (0.73f * s);
            }
            else
            {
                Dashboard.transform.localScale = new Vector3(SCALE, SCALE, SCALE);
                Dashboard.transform.position = reference.position + reference.forward * 0.73f;
            }
            Dashboard.transform.rotation =
                Quaternion.LookRotation((Dashboard.transform.position - reference.position).normalized);
        }

        public void ToggleDashboard(LocalPlayer localPlayer)
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                PositionDashboard(localPlayer);
                OpenedPosition = localPlayer.transform.position;
                OpenedRotation = localPlayer.transform.rotation;
                OpenedScale = localPlayer.transform.localScale;
                OpenedBounds = localPlayer.CharacterController.bounds;
            }
            Dashboard.SetActive(IsVisible);
            localPlayer.LockCamera = IsVisible;
            localPlayer.LockMovement = IsVisible;
        }
    }
}