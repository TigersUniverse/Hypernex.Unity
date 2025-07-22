using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.Game;
using Hypernex.Tools;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Hypernex.UI.Abstraction.AvatarControllers
{
    public class AxisControl : UIRender, IRender<(AvatarControl, AvatarParameter, AvatarParameter)>, IParameterControl
    {
        public TMP_Text ControlText;
        public Image Icon;
        public Transform Dot;
        public Transform TopLeft;
        public Transform BottomRight;

        private AvatarParameter avatarParameter1;
        private AvatarParameter avatarParameter2;
        private Vector3 center;
        private Vector3 extents;
        private Vector2 state;
        
        public void Render((AvatarControl, AvatarParameter, AvatarParameter) t)
        {
            center = (TopLeft.localPosition + BottomRight.localPosition) * 0.5f;
            extents = new Vector3(math.abs(TopLeft.localPosition.x - BottomRight.localPosition.x) * 0.5f,
                math.abs(TopLeft.localPosition.y - BottomRight.localPosition.y) * 0.5f, 0);
            ControlText.text = t.Item1.ControlName;
            Icon.gameObject.SetActive(t.Item1.ControlSprite != null);
            Icon.sprite = t.Item1.ControlSprite;
            avatarParameter1 = t.Item2;
            avatarParameter2 = t.Item3;
            UpdateState();
        }

        public void UpdateState()
        {
            if (LocalPlayer.Instance.AvatarCreator == null) return;
            float x = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter1.ParameterName).ParameterToFloat();
            float y = LocalPlayer.Instance.AvatarCreator.GetParameter(avatarParameter2.ParameterName).ParameterToFloat();
            state = new Vector2(x, y);
            MoveDotToPosition(state);
            AxisPositionChanged();
        }

        private void AxisPositionChanged()
        {
            Vector2 v2 = GetPosition();
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter1.ParameterName, v2.x, null, true);
            LocalPlayer.Instance.AvatarCreator.SetParameter(avatarParameter2.ParameterName, v2.y, null, true);
        }

        private void MoveDotToPosition(Vector2 v2)
        {
            v2 = math.clamp(v2, new Vector2(-1f, -1f), new Vector2(1f, 1f));
            Vector3 localPos = new Vector3(center.x + v2.x * extents.x, center.y + v2.y * extents.y, 0f);
            Dot.localPosition = localPos;
        }

        private Vector2 GetPosition()
        {
            Vector3 localPos = Dot.localPosition;
            Vector2 result = new Vector2((localPos.x - center.x) / extents.x, (localPos.y - center.y) / extents.y);
            return math.clamp(result, new Vector2(-1f, -1f), new Vector2(1f, 1f));
        }

        private void OnEnter(Ray ray, float enter)
        {
            Vector3 worldHitPoint = ray.GetPoint(enter);
            Vector3 localMousePos = Dot.parent.InverseTransformPoint(worldHitPoint);
            localMousePos.z = 0;
            Vector3 topLeft = TopLeft.localPosition;
            Vector3 bottomRight = BottomRight.localPosition;
            if (localMousePos.x < math.min(topLeft.x, bottomRight.x) || localMousePos.x > math.max(topLeft.x, bottomRight.x)) return;
            if (localMousePos.y < math.min(topLeft.y, bottomRight.y) || localMousePos.y > math.max(topLeft.y, bottomRight.y)) return;
            Dot.localPosition = localMousePos;
            AxisPositionChanged();
        }

        private void Update()
        {
            if(!gameObject.activeInHierarchy) return;
            LocalPlayer localPlayer = LocalPlayer.Instance;
            bool leftHandPressed = false;
            bool rightHandPressed = false;
            if (LocalPlayer.IsVR)
            {
                foreach (IBinding binding in localPlayer.Bindings)
                {
                    if(binding.Trigger < 0.9f) continue;
                    if (binding.IsLook)
                        leftHandPressed = true;
                    else
                        rightHandPressed = true;
                }
                if(!leftHandPressed && !rightHandPressed) return;
            }
            else
            {
                IBinding lookBinding = localPlayer.Bindings.FirstOrDefault(x => !x.IsLook);
                if (lookBinding == null) return;
                if (lookBinding.Trigger < 0.9f) return;
            }
            Plane canvasPlane = new Plane(-Dot.parent.forward, Dot.parent.position);
            if (!LocalPlayer.IsVR)
            {
                Ray desktopRay = localPlayer.Camera.ScreenPointToRay(Input.mousePosition);
                if(canvasPlane.Raycast(desktopRay, out float enterDesktop))
                    OnEnter(desktopRay, enterDesktop);
            }
            else
            {
                if (leftHandPressed)
                {
                    Ray leftHandRay = new Ray(localPlayer.LeftHandReference.transform.position,
                        localPlayer.LeftHandReference.transform.forward);
                    if (canvasPlane.Raycast(leftHandRay, out float enterLeftHand))
                        OnEnter(leftHandRay, enterLeftHand);
                }
                else if (rightHandPressed)
                {
                    Ray rightHandRay = new Ray(localPlayer.RightHandReference.transform.position,
                        localPlayer.RightHandReference.transform.forward);
                    if(canvasPlane.Raycast(rightHandRay, out float enterRightHand))
                        OnEnter(rightHandRay, enterRightHand);
                }
            }
        }
    }
}