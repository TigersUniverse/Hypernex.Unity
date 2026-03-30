using System.Linq;
using Hypernex.Game;
using Hypernex.Tools;
using Unity.Mathematics;
using UnityEngine;

namespace Hypernex.UI.Components
{
    public class UIAxis : UIRender
    {
        public bool is2D;
        public RectTransform Dot;
        public RectTransform TopLeft;
        public RectTransform BottomRight;
        
        private Vector3 center;
        private Vector3 extents;
        private int activeTouch = -1;

        protected virtual void AxisPositionChanged(Vector2 pos){}
        
        public Vector2 GetPosition()
        {
            Vector3 localPos = Dot.localPosition;
            Vector2 result = new Vector2((localPos.x - center.x) / extents.x, (localPos.y - center.y) / extents.y);
            return math.clamp(result, new Vector2(-1f, -1f), new Vector2(1f, 1f));
        }

        internal void Render()
        {
            center = (TopLeft.localPosition + BottomRight.localPosition) * 0.5f;
            extents = new Vector3(math.abs(TopLeft.localPosition.x - BottomRight.localPosition.x) * 0.5f,
                math.abs(TopLeft.localPosition.y - BottomRight.localPosition.y) * 0.5f, 0);
        }

        protected void MoveDotToPosition(Vector2 v2)
        {
            v2 = math.clamp(v2, new Vector2(-1f, -1f), new Vector2(1f, 1f));
            Vector3 localPos = new Vector3(center.x + v2.x * extents.x, center.y + v2.y * extents.y, 0f);
            Dot.localPosition = localPos;
            AxisPositionChanged(GetPosition());
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
            AxisPositionChanged(GetPosition());
        }

        private void OnEnter(Vector2 localPoint)
        {
            float minX = math.min(TopLeft.localPosition.x, BottomRight.localPosition.x);
            float maxX = math.max(TopLeft.localPosition.x, BottomRight.localPosition.x);
            float minY = math.min(TopLeft.localPosition.y, BottomRight.localPosition.y);
            float maxY = math.max(TopLeft.localPosition.y, BottomRight.localPosition.y);
            if (localPoint.x < minX || localPoint.x > maxX) return;
            if (localPoint.y < minY || localPoint.y > maxY) return;
            Dot.localPosition = localPoint;
            AxisPositionChanged(GetPosition());
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
                if (!is2D)
                {
                    Ray desktopRay = localPlayer.Camera.ScreenPointToRay(Input.mousePosition);
                    if (canvasPlane.Raycast(desktopRay, out float enterDesktop))
                        OnEnter(desktopRay, enterDesktop);
                }
                else
                {
                    if (Init.Instance.IsMobile)
                    {
                        if (activeTouch >= 0)
                        {
                            Touch? currentTouch = MobileTouch.GetTouchFromFingerId(activeTouch);
                            // check if the touch still exists
                            if (currentTouch == null || currentTouch.Value.phase == TouchPhase.Ended)
                            {
                                activeTouch = -1;
                                MoveDotToPosition(Vector2.zero);
                            }
                            else
                            {
                                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                    (RectTransform)Dot.parent,
                                    currentTouch.Value.position,
                                    null,
                                    out Vector2 localPoint
                                );
                                OnEnter(localPoint);
                            }
                        }
                        else
                        {
                            Touch? currentTouch = MobileTouch.GetTouchFromBounds(Dot, TopLeft.localPosition,
                                BottomRight.localPosition, out Vector2? localPoint);
                            if (currentTouch != null && localPoint != null)
                            {
                                activeTouch = currentTouch.Value.fingerId;
                                OnEnter(localPoint.Value);
                            }
                            else
                                activeTouch = -1;
                        }
                    }
                    else
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            (RectTransform)Dot.parent,
                            Input.mousePosition,
                            null,
                            out Vector2 localPoint
                        );
                        OnEnter(localPoint);
                    }
                }
            }
            else
            {
                // 2D only supports mouse click, because it can't exist in VR
                if (is2D) return;
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