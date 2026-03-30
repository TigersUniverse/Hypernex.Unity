using UnityEngine;
using UnityEngine.EventSystems;

namespace Hypernex.UI.Components
{
    public class ButtonHeld : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;
        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;
    }
}