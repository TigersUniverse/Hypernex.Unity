using UnityEngine;
using UnityEngine.EventSystems;

namespace Hypernex.Tools
{
    // most of this is copied from Unity's implementation, but with the cursor lock checks removed.
    public class FirstPersonInputModule : StandaloneInputModule
    {
        protected MouseState m_MouseState = new MouseState();
        protected Vector2 mouseDelta;

        private void OnGUI()
        {
            if(Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 delta = Vector2.zero;
            Rect rect = new Rect(Screen.width / 2f + delta.x, Screen.height / 2f + delta.y, 32f, 32f);
            GUI.DrawTexture(rect, eventSystem.IsPointerOverGameObject() ? CursorTools.newMouse : CursorTools.newCircle, ScaleMode.StretchToFill);
        }

        public override void UpdateModule()
        {
            base.UpdateModule();
            mouseDelta = Input.mousePositionDelta;
        }

        protected override MouseState GetMousePointerEventData(int id)
        {
            // Populate the left button...
            PointerEventData leftData;
            var created = GetPointerData(kMouseLeftId, out leftData, true);

            leftData.Reset();

            if (created)
                leftData.position = input.mousePosition;

            Vector2 pos = input.mousePosition;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                leftData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
                leftData.delta = Input.mousePositionDelta;
            }
            else
            {
                leftData.delta = pos - leftData.position;
                leftData.position = pos;
            }
            leftData.scrollDelta = input.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

            // copy the apropriate data into right and middle slots
            PointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true);
            rightData.Reset();

            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;

            PointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true);
            middleData.Reset();

            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            m_MouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

            return m_MouseState;
        }
        protected override void ProcessMove(PointerEventData pointerEvent)
        {
            var targetGO = pointerEvent.pointerCurrentRaycast.gameObject;
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }
    }
}