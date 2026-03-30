using Unity.Mathematics;
using UnityEngine;

namespace Hypernex.Tools
{
    public class MobileTouch
    {
        public static Touch? GetTouchFromFingerId(int fingerId)
        {
            foreach (Touch touch in Input.touches)
            {
                if(touch.fingerId != fingerId) continue;
                return touch;
            }
            return null;
        }

        public static Touch? GetBeganTouch()
        {
            foreach (Touch touch in Input.touches)
            {
                if(touch.phase != TouchPhase.Began) continue;
                return touch;
            }
            return null;
        }
        
        public static Touch? GetTouchFromBounds(RectTransform dot, Vector3 topLeft, Vector3 bottomRight, out Vector2? l)
        {
            foreach (Touch touch in Input.touches)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)dot.parent,
                    touch.position,
                    null,
                    out Vector2 localPoint
                );
                float minX = math.min(topLeft.x, bottomRight.x);
                float maxX = math.max(topLeft.x, bottomRight.x);
                float minY = math.min(topLeft.y, bottomRight.y);
                float maxY = math.max(topLeft.y, bottomRight.y);
                if (localPoint.x >= minX && localPoint.x <= maxX && localPoint.y >= minY && localPoint.y <= maxY)
                {
                    l = localPoint;
                    return touch;
                }
            }
            l = null;
            return null;
        }
    }
}